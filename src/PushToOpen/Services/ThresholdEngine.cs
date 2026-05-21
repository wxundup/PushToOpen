using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class ThresholdEngine : IThresholdEngine
{
    private readonly object _gate = new();
    private double _thresholdLinear = AppSettings.DbToLinear(-38.0);
    private int _attackMs = 25;
    private int _releaseMs = 220;
    private int _debounceMs = 40;
    private bool _enabled = true;
    private bool _muted;
    private bool _appGate = true;
    private DateTime _firstAboveAt;
    private DateTime _firstBelowAt;
    private DateTime _lastTransitionAt;
    private GateState _state = GateState.Closed;

    public ThresholdEngine()
    {
        // seed so debounce check doesn't skip on the very first gate event
        _lastTransitionAt = DateTime.UtcNow - TimeSpan.FromSeconds(10);
    }

    public GateState State => _state;

    public event EventHandler? GateOpened;
    public event EventHandler? GateClosed;
    public event EventHandler<GateState>? StateChanged;

    public void SetEnabled(bool enabled)
    {
        lock (_gate) _enabled = enabled;
        if (!enabled) ForceClose();
    }

    public void SetMuted(bool muted)
    {
        lock (_gate) _muted = muted;
        if (muted) ForceClose();
    }

    public void SetAppGate(bool allowed)
    {
        lock (_gate) _appGate = allowed;
        if (!allowed) ForceClose();
    }

    public void SetThresholdDb(double db) { lock (_gate) _thresholdLinear = AppSettings.DbToLinear(db); }
    public void SetAttackMs(int ms)  { lock (_gate) _attackMs  = Math.Max(0, ms); }
    public void SetReleaseMs(int ms) { lock (_gate) _releaseMs = Math.Max(0, ms); }
    public void SetDebounceMs(int ms){ lock (_gate) _debounceMs= Math.Max(0, ms); }

    public void Feed(AudioLevel level)
    {
        bool open = false, close = false;
        lock (_gate)
        {
            if (!_enabled || _muted || !_appGate)
            {
                if (_state == GateState.Open) { _state = GateState.Closed; close = true; }
            }
            else
            {
                bool above = level.Rms >= _thresholdLinear;
                var now = level.Timestamp;
                if (above)
                {
                    if (_firstAboveAt == default) _firstAboveAt = now;
                    _firstBelowAt = default;
                    if (_state == GateState.Closed
                        && (now - _firstAboveAt).TotalMilliseconds >= _attackMs
                        && (now - _lastTransitionAt).TotalMilliseconds >= _debounceMs)
                    {
                        _state = GateState.Open;
                        _lastTransitionAt = now;
                        open = true;
                    }
                }
                else
                {
                    if (_firstBelowAt == default) _firstBelowAt = now;
                    _firstAboveAt = default;
                    if (_state == GateState.Open
                        && (now - _firstBelowAt).TotalMilliseconds >= _releaseMs
                        && (now - _lastTransitionAt).TotalMilliseconds >= _debounceMs)
                    {
                        _state = GateState.Closed;
                        _lastTransitionAt = now;
                        close = true;
                    }
                }
            }
        }

        if (open)  { GateOpened?.Invoke(this, EventArgs.Empty); StateChanged?.Invoke(this, GateState.Open);  }
        if (close) { GateClosed?.Invoke(this, EventArgs.Empty); StateChanged?.Invoke(this, GateState.Closed);}
    }

    public void ForceClose()
    {
        bool fire = false;
        lock (_gate)
        {
            if (_state == GateState.Open) { _state = GateState.Closed; fire = true; }
            _firstAboveAt = default;
            _firstBelowAt = default;
        }
        if (fire)
        {
            GateClosed?.Invoke(this, EventArgs.Empty);
            StateChanged?.Invoke(this, GateState.Closed);
        }
    }
}
