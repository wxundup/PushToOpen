namespace PushToOpen.Services;

public interface ITrayService : IDisposable
{
    event EventHandler? ShowRequested;
    event EventHandler? ExitRequested;
    event EventHandler<bool>? MuteToggled;
    event EventHandler<bool>? EnabledToggled;
    event EventHandler<double>? ThresholdPresetSelected;

    void Initialize();
    void UpdateState(bool enabled, bool muted, double thresholdDb);
}
