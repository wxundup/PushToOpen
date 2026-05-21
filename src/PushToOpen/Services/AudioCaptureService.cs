using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class AudioCaptureService : IAudioCaptureService, IMMNotificationClient
{
    private readonly object _gate = new();
    private readonly MMDeviceEnumerator _enumerator = new();
    private WasapiCapture? _capture;
    private MMDevice? _device;
    private double _gainLinear = 1.0;
    private double _gateLinear = AppSettings.DbToLinear(-55.0);
    private double _peakSmoothed;
    private double _rmsSmoothed;
    private bool _disposed;

    // Monitor (hear-yourself)
    private WasapiOut? _monitorOut;
    private BufferedWaveProvider? _monitorBuffer;
    private bool _monitorEnabled;
    private double _monitorGainLinear = AppSettings.DbToLinear(-6.0);

    // Noise suppression
    private NoiseSuppressor? _suppressor;
    private bool _suppressionEnabled;

    public AudioCaptureService()
    {
        try { _enumerator.RegisterEndpointNotificationCallback(this); } catch { }
        Devices = Array.Empty<AudioDeviceInfo>();
    }

    public IReadOnlyList<AudioDeviceInfo> Devices { get; private set; }
    public AudioDeviceInfo? CurrentDevice { get; private set; }
    public bool IsCapturing { get; private set; }

    public event EventHandler<AudioLevel>? LevelMeasured;
    public event EventHandler<IReadOnlyList<AudioDeviceInfo>>? DevicesChanged;
    public event EventHandler<string?>? CaptureError;

    public Task RefreshDevicesAsync()
    {
        var list = new List<AudioDeviceInfo>();
        string? defaultId = null;
        try
        {
            using var def = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            defaultId = def.ID;
        }
        catch { }

        foreach (var dev in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            list.Add(new AudioDeviceInfo(dev.ID, dev.FriendlyName, dev.ID == defaultId));
            dev.Dispose();
        }

        Devices = list;
        DevicesChanged?.Invoke(this, list);
        return Task.CompletedTask;
    }

    public async Task StartAsync(string? deviceId)
    {
        await StopAsync().ConfigureAwait(false);
        if (Devices.Count == 0) await RefreshDevicesAsync().ConfigureAwait(false);

        MMDevice? target = null;
        try
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                try { target = _enumerator.GetDevice(deviceId); }
                catch { target = null; }
            }
            target ??= _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

            lock (_gate)
            {
                _device = target;
                CurrentDevice = new AudioDeviceInfo(target.ID, target.FriendlyName, true);
                _capture = new WasapiCapture(target, useEventSync: true, audioBufferMillisecondsLength: 20);
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();
                IsCapturing = true;
                _suppressor?.Reset();
            }

            if (_monitorEnabled) TryStartMonitor();
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            await StopAsync().ConfigureAwait(false);
        }
    }

    public Task StopAsync()
    {
        WasapiCapture? toDispose;
        MMDevice? device;
        lock (_gate)
        {
            toDispose = _capture;
            device = _device;
            _capture = null;
            _device = null;
            IsCapturing = false;
        }
        if (toDispose is not null)
        {
            toDispose.DataAvailable -= OnDataAvailable;
            toDispose.RecordingStopped -= OnRecordingStopped;
            try { toDispose.StopRecording(); } catch { }
            try { toDispose.Dispose(); } catch { }
        }
        device?.Dispose();
        StopMonitor();
        return Task.CompletedTask;
    }

    public void SetGainDb(double db) { lock (_gate) _gainLinear = AppSettings.DbToLinear(db); }
    public void SetNoiseGateDb(double db) { lock (_gate) _gateLinear = AppSettings.DbToLinear(db); }
    public void SetMonitorGainDb(double db) { lock (_gate) _monitorGainLinear = AppSettings.DbToLinear(db); }

    public void SetMonitorEnabled(bool enabled)
    {
        lock (_gate) _monitorEnabled = enabled;
        if (enabled) TryStartMonitor();
        else StopMonitor();
    }

    public void SetNoiseSuppression(bool enabled, double strength)
    {
        lock (_gate)
        {
            _suppressionEnabled = enabled;
            if (enabled)
            {
                _suppressor ??= new NoiseSuppressor();
                _suppressor.SetStrength(strength);
            }
        }
    }

    private void TryStartMonitor()
    {
        WaveFormat? fmt;
        lock (_gate) fmt = _capture?.WaveFormat;
        if (fmt is null) return;
        try
        {
            // Mono float monitor format keeps DSP path simple; resample if needed.
            var monitorFmt = WaveFormat.CreateIeeeFloatWaveFormat(fmt.SampleRate, 1);
            var buffer = new BufferedWaveProvider(monitorFmt)
            {
                BufferDuration = TimeSpan.FromMilliseconds(400),
                DiscardOnBufferOverflow = true,
            };
            var output = new WasapiOut(AudioClientShareMode.Shared, 30);
            output.Init(buffer);
            output.Play();
            lock (_gate)
            {
                _monitorBuffer = buffer;
                _monitorOut = output;
            }
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, "Monitor failed: " + ex.Message);
            StopMonitor();
        }
    }

    private void StopMonitor()
    {
        WasapiOut? out_;
        lock (_gate)
        {
            out_ = _monitorOut;
            _monitorOut = null;
            _monitorBuffer = null;
        }
        if (out_ is not null)
        {
            try { out_.Stop(); } catch { }
            try { out_.Dispose(); } catch { }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is null) return;
        CaptureError?.Invoke(this, e.Exception.Message);
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500).ConfigureAwait(false);
            bool shouldRestart;
            lock (_gate) { shouldRestart = !_disposed && _capture is null; }
            if (shouldRestart) await StartAsync(null).ConfigureAwait(false);
        });
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var capture = _capture;
        if (capture is null || e.BytesRecorded == 0) return;

        var fmt = capture.WaveFormat;
        var bps = fmt.BitsPerSample;
        var channels = fmt.Channels;
        var isFloat = fmt.Encoding == WaveFormatEncoding.IeeeFloat;
        var bytesPerSample = bps / 8;
        var frameSize = bytesPerSample * channels;
        if (frameSize == 0) return;
        var frameCount = e.BytesRecorded / frameSize;
        if (frameCount == 0) return;

        double gainLinear, gateLinear, monitorGain;
        bool monitorOn, suppressOn;
        NoiseSuppressor? suppressor;
        BufferedWaveProvider? monitorBuf;
        lock (_gate)
        {
            gainLinear = _gainLinear;
            gateLinear = _gateLinear;
            monitorGain = _monitorGainLinear;
            monitorOn = _monitorEnabled;
            suppressOn = _suppressionEnabled;
            suppressor = _suppressor;
            monitorBuf = _monitorBuffer;
        }

        // Downmix → mono float array (gain applied).
        var mono = new float[frameCount];
        double sumSq = 0;
        double peak = 0;
        var data = e.Buffer;

        for (int i = 0; i < frameCount; i++)
        {
            double sampleSum = 0;
            for (int c = 0; c < channels; c++)
            {
                int idx = (i * channels + c) * bytesPerSample;
                double s = isFloat
                    ? BitConverter.ToSingle(data, idx)
                    : bps switch
                    {
                        16 => BitConverter.ToInt16(data, idx) / 32768.0,
                        24 => Read24(data, idx) / 8388608.0,
                        32 => BitConverter.ToInt32(data, idx) / 2147483648.0,
                        _ => 0.0,
                    };
                sampleSum += s;
            }
            double m = (sampleSum / channels) * gainLinear;
            mono[i] = (float)m;
        }

        // Optional noise suppression (in-place).
        if (suppressOn && suppressor is not null)
        {
            suppressor.Process(mono);
        }

        // Compute RMS/peak from the *post-suppression* signal (so meter reflects
        // what downstream apps will see).
        for (int i = 0; i < frameCount; i++)
        {
            double s = mono[i];
            double abs = Math.Abs(s);
            if (abs > peak) peak = abs;
            sumSq += s * s;
        }

        double rms = Math.Sqrt(sumSq / frameCount);
        if (rms < gateLinear) rms = 0;

        const double rmsAttack = 0.35;
        const double rmsRelease = 0.12;
        _rmsSmoothed = rms > _rmsSmoothed
            ? _rmsSmoothed + (rms - _rmsSmoothed) * rmsAttack
            : _rmsSmoothed + (rms - _rmsSmoothed) * rmsRelease;

        const double peakAttack = 0.6;
        const double peakRelease = 0.06;
        _peakSmoothed = peak > _peakSmoothed
            ? _peakSmoothed + (peak - _peakSmoothed) * peakAttack
            : _peakSmoothed + (peak - _peakSmoothed) * peakRelease;

        var level = new AudioLevel(
            Rms: _rmsSmoothed,
            Peak: _peakSmoothed,
            RmsDb: AppSettings.LinearToDb(_rmsSmoothed),
            PeakDb: AppSettings.LinearToDb(_peakSmoothed),
            Timestamp: DateTime.UtcNow);
        LevelMeasured?.Invoke(this, level);

        // Monitor: scale by monitor gain, write float samples to output buffer.
        if (monitorOn && monitorBuf is not null)
        {
            var bytes = new byte[frameCount * 4];
            for (int i = 0; i < frameCount; i++)
            {
                float v = (float)Math.Clamp(mono[i] * monitorGain, -1.0, 1.0);
                BitConverter.GetBytes(v).CopyTo(bytes, i * 4);
            }
            try { monitorBuf.AddSamples(bytes, 0, bytes.Length); } catch { }
        }
    }

    private static int Read24(byte[] buf, int idx)
    {
        int v = buf[idx] | (buf[idx + 1] << 8) | (buf[idx + 2] << 16);
        if ((v & 0x800000) != 0) v |= unchecked((int)0xFF000000);
        return v;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
        await StopAsync().ConfigureAwait(false);
        try { _enumerator.Dispose(); } catch { }
    }

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) => _ = Task.Run(RefreshDevicesAsync);
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) => _ = Task.Run(RefreshDevicesAsync);
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) => _ = Task.Run(RefreshDevicesAsync);
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => _ = Task.Run(RefreshDevicesAsync);
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
