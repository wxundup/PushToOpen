using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class PushToTalkCoordinator : IAsyncDisposable
{
    private readonly IAudioCaptureService _audio;
    private readonly IThresholdEngine _engine;
    private readonly IInputSimulator _input;
    private readonly ISettingsService _settings;
    private bool _wired;
    private static bool _exitHandlersRegistered;

    public PushToTalkCoordinator(
        IAudioCaptureService audio,
        IThresholdEngine engine,
        IInputSimulator input,
        ISettingsService settings)
    {
        _audio = audio;
        _engine = engine;
        _input = input;
        _settings = settings;
    }

    public async Task StartAsync()
    {
        if (!_wired)
        {
            _audio.LevelMeasured += OnLevel;
            _engine.GateOpened += OnGateOpened;
            _engine.GateClosed += OnGateClosed;
            _settings.SettingsChanged += OnSettingsChanged;
            _wired = true;
        }

        if (!_exitHandlersRegistered)
        {
            _exitHandlersRegistered = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => SafeRelease();
            AppDomain.CurrentDomain.UnhandledException += (_, _) => SafeRelease();
        }

        ApplySettings(_settings.Current);
        await _audio.RefreshDevicesAsync().ConfigureAwait(false);
        await _audio.StartAsync(_settings.Current.InputDeviceId).ConfigureAwait(false);
    }

    private void OnSettingsChanged(object? sender, AppSettings s) => ApplySettings(s);

    private void ApplySettings(AppSettings s)
    {
        _engine.SetThresholdDb(s.ThresholdDb);
        _engine.SetAttackMs(s.AttackMs);
        _engine.SetReleaseMs(s.ReleaseMs);
        _engine.SetDebounceMs(s.DebounceMs);
        _engine.SetEnabled(s.Enabled);
        _engine.SetMuted(s.Muted);
        _audio.SetGainDb(s.GainDb);
        _audio.SetNoiseGateDb(s.NoiseGateDb);
        _input.Bind(s.Hotkey);
    }

    private void OnLevel(object? sender, AudioLevel level) => _engine.Feed(level);
    private void OnGateOpened(object? sender, EventArgs e) => _input.Press();
    private void OnGateClosed(object? sender, EventArgs e) => _input.Release();

    private void SafeRelease()
    {
        try { _input.Release(); } catch { }
        try { _input.Dispose(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_wired)
        {
            _audio.LevelMeasured -= OnLevel;
            _engine.GateOpened -= OnGateOpened;
            _engine.GateClosed -= OnGateClosed;
            _settings.SettingsChanged -= OnSettingsChanged;
        }
        SafeRelease();
        await _audio.StopAsync().ConfigureAwait(false);
    }
}
