using PushToOpen.Models;

namespace PushToOpen.Services;

public readonly record struct AudioLevel(double Rms, double Peak, double RmsDb, double PeakDb, DateTime Timestamp);

public interface IAudioCaptureService : IAsyncDisposable
{
    IReadOnlyList<AudioDeviceInfo> Devices { get; }

    AudioDeviceInfo? CurrentDevice { get; }

    bool IsCapturing { get; }

    event EventHandler<AudioLevel>? LevelMeasured;

    event EventHandler<IReadOnlyList<AudioDeviceInfo>>? DevicesChanged;

    event EventHandler<string?>? CaptureError;

    Task RefreshDevicesAsync();

    Task StartAsync(string? deviceId);

    Task StopAsync();

    void SetGainDb(double db);

    void SetNoiseGateDb(double db);
}
