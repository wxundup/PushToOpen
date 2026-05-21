namespace PushToOpen.Services;

public sealed class TrayService : ITrayService
{
    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<bool>? MuteToggled;
    public event EventHandler<bool>? EnabledToggled;
    public event EventHandler<double>? ThresholdPresetSelected;

    public void Initialize() { /* TaskbarIcon hosted in App.xaml; events bridged via App.RaiseTray* helpers */ }

    public void UpdateState(bool enabled, bool muted, double thresholdDb)
    {
        State = (enabled, muted, thresholdDb);
        StateChanged?.Invoke(this, State);
    }

    internal (bool enabled, bool muted, double thresholdDb) State { get; private set; } = (true, false, -38.0);
    internal event EventHandler<(bool enabled, bool muted, double thresholdDb)>? StateChanged;

    internal void RaiseShow() => ShowRequested?.Invoke(this, EventArgs.Empty);
    internal void RaiseExit() => ExitRequested?.Invoke(this, EventArgs.Empty);
    internal void RaiseMute(bool muted) => MuteToggled?.Invoke(this, muted);
    internal void RaiseEnabled(bool enabled) => EnabledToggled?.Invoke(this, enabled);
    internal void RaisePreset(double db) => ThresholdPresetSelected?.Invoke(this, db);

    public void Dispose() { }
}
