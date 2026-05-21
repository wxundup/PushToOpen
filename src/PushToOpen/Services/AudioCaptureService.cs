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

    public AudioCaptureService()
    {
        try { _enumerator.RegisterEndpointNotificationCallback(this); } catch { /* enumerator notifications best-effort */ }
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
        catch { /* may be unavailable */ }

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
            }
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
        return Task.CompletedTask;
    }

    public void SetGainDb(double db) => _gainLinear = AppSettings.DbToLinear(db);

    public void SetNoiseGateDb(double db) => _gateLinear = AppSettings.DbToLinear(db);

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            CaptureError?.Invoke(this, e.Exception.Message);
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
            double mono = sampleSum / channels;
            mono *= _gainLinear;
            double abs = Math.Abs(mono);
            if (abs > peak) peak = abs;
            sumSq += mono * mono;
        }

        double rms = Math.Sqrt(sumSq / frameCount);
        if (rms < _gateLinear) rms = 0;

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

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) => _ = RefreshDevicesAsync();
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) => _ = RefreshDevicesAsync();
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) => _ = RefreshDevicesAsync();
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => _ = RefreshDevicesAsync();
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
