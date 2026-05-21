using System.Numerics;

namespace PushToOpen.Services;

/// <summary>
/// Spectral subtraction noise suppression with overlap-add reconstruction.
/// Hann analysis window (50% hop → COLA satisfied). Per-bin noise floor tracked
/// via exponential moving average on low-energy frames. Oversubtraction with
/// a spectral floor avoids musical noise.
///
/// Not Krisp/RNNoise (no neural net). Real DSP — audibly removes steady-state
/// hiss, fan noise, room tone. Mono-only.
/// </summary>
public sealed class NoiseSuppressor
{
    private const int FftSize = 512;
    private const int Hop = FftSize / 2;
    private const double FloorDb = -55.0;

    private readonly double[] _window;
    private readonly double[] _inputBuffer = new double[FftSize];
    private readonly double[] _outputAccum = new double[FftSize];
    private readonly Queue<float> _outputQueue = new(Hop * 4);
    private readonly double[] _noiseMag = new double[FftSize / 2 + 1];
    private readonly double[] _gainSmoothed = new double[FftSize / 2 + 1];
    private readonly Complex[] _fft = new Complex[FftSize];

    private int _inputFill;
    private int _frameCount;
    private bool _noiseLearned;
    private double _strength = 0.7;

    public NoiseSuppressor()
    {
        _window = new double[FftSize];
        // Periodic Hann (divisor N, not N-1) so 50% hop satisfies COLA exactly.
        for (int i = 0; i < FftSize; i++)
            _window[i] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / FftSize);

        // Pre-queue Hop zeros to compensate for STFT latency (avoids
        // emitting nothing until the first full frame arrives).
        for (int i = 0; i < Hop; i++) _outputQueue.Enqueue(0);
    }

    public void SetStrength(double s) => _strength = Math.Clamp(s, 0, 1);

    public void Reset()
    {
        Array.Clear(_inputBuffer);
        Array.Clear(_outputAccum);
        Array.Clear(_noiseMag);
        Array.Clear(_gainSmoothed);
        _outputQueue.Clear();
        for (int i = 0; i < Hop; i++) _outputQueue.Enqueue(0);
        _inputFill = 0;
        _frameCount = 0;
        _noiseLearned = false;
    }

    /// <summary>
    /// Process a buffer of mono samples in-place. Internal buffering means
    /// each sample emitted is delayed by Hop frames (~5ms at 48kHz).
    /// </summary>
    public void Process(Span<float> samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            _inputBuffer[_inputFill++] = samples[i];

            if (_inputFill == FftSize)
            {
                ProcessFrame();
                // First Hop samples of accumulator are final → enqueue.
                for (int k = 0; k < Hop; k++) _outputQueue.Enqueue((float)_outputAccum[k]);
                // Shift accumulator left by Hop, clear tail.
                Buffer.BlockCopy(_outputAccum, sizeof(double) * Hop,
                                 _outputAccum, 0, sizeof(double) * Hop);
                Array.Clear(_outputAccum, Hop, FftSize - Hop);
                // Shift input buffer left by Hop, leave room for next half-frame.
                Buffer.BlockCopy(_inputBuffer, sizeof(double) * Hop,
                                 _inputBuffer, 0, sizeof(double) * Hop);
                _inputFill = Hop;
            }

            samples[i] = _outputQueue.Count > 0 ? _outputQueue.Dequeue() : 0f;
        }
    }

    private void ProcessFrame()
    {
        for (int i = 0; i < FftSize; i++)
            _fft[i] = new Complex(_inputBuffer[i] * _window[i], 0);

        Fft(_fft, forward: true);

        int nb = FftSize / 2 + 1;

        Span<double> mag = stackalloc double[513];
        double energy = 0;
        for (int k = 0; k < nb; k++)
        {
            double m = _fft[k].Magnitude;
            mag[k] = m;
            energy += m * m;
        }
        double rms = Math.Sqrt(energy / nb) / FftSize;
        double db = rms <= 1e-9 ? -120 : 20.0 * Math.Log10(rms);

        bool learn = !_noiseLearned || db < FloorDb;
        if (learn)
        {
            double alpha = _noiseLearned ? 0.05 : 0.5;
            for (int k = 0; k < nb; k++)
                _noiseMag[k] = (1 - alpha) * _noiseMag[k] + alpha * mag[k];
            _frameCount++;
            if (_frameCount > 8) _noiseLearned = true;
        }

        double over = 1.0 + _strength * 1.5;
        double floorGain = 0.05 + (1.0 - _strength) * 0.15;
        for (int k = 0; k < nb; k++)
        {
            double m2 = mag[k] * mag[k];
            double n2 = _noiseMag[k] * _noiseMag[k];
            double gain = m2 <= 1e-12 ? floorGain : (m2 - over * n2) / m2;
            if (double.IsNaN(gain) || gain < floorGain) gain = floorGain;
            else if (gain > 1) gain = 1;
            _gainSmoothed[k] = 0.7 * _gainSmoothed[k] + 0.3 * gain;
            _fft[k] *= _gainSmoothed[k];
        }

        for (int k = nb; k < FftSize; k++)
            _fft[k] = Complex.Conjugate(_fft[FftSize - k]);

        Fft(_fft, forward: false);

        // Overlap-add (analysis-only Hann + 50% hop = COLA, no synthesis window).
        for (int i = 0; i < FftSize; i++)
            _outputAccum[i] += _fft[i].Real;
    }

    /// <summary>Iterative Cooley-Tukey radix-2 FFT, in place. N must be power of two.</summary>
    private static void Fft(Complex[] data, bool forward)
    {
        int n = data.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (data[i], data[j]) = (data[j], data[i]);
        }
        double sign = forward ? -1.0 : 1.0;
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = sign * 2.0 * Math.PI / len;
            var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
            for (int i = 0; i < n; i += len)
            {
                var w = Complex.One;
                for (int k = 0; k < len / 2; k++)
                {
                    var u = data[i + k];
                    var v = data[i + k + len / 2] * w;
                    data[i + k] = u + v;
                    data[i + k + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
        if (!forward)
        {
            for (int i = 0; i < n; i++) data[i] /= n;
        }
    }
}
