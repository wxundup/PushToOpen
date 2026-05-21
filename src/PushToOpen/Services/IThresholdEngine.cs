namespace PushToOpen.Services;

public enum GateState { Closed, Open }

public interface IThresholdEngine
{
    GateState State { get; }

    event EventHandler? GateOpened;
    event EventHandler? GateClosed;
    event EventHandler<GateState>? StateChanged;

    void SetEnabled(bool enabled);
    void SetMuted(bool muted);
    /// <summary>External gate — false forces the engine inactive (used by window-restriction).</summary>
    void SetAppGate(bool allowed);
    void SetThresholdDb(double db);
    void SetAttackMs(int ms);
    void SetReleaseMs(int ms);
    void SetDebounceMs(int ms);
    void Feed(AudioLevel level);
    void ForceClose();
}
