using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class OverlayViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IAudioCaptureService _audio;
    private readonly IThresholdEngine _engine;
    private bool _suppress;

    public OverlayViewModel(ISettingsService settings, IAudioCaptureService audio, IThresholdEngine engine)
    {
        _settings = settings;
        _audio = audio;
        _engine = engine;
        _audio.LevelMeasured += OnLevel;
        _engine.StateChanged += OnGate;
        _settings.SettingsChanged += OnSettingsChanged;
        Pull(_settings.Current);
    }

    [ObservableProperty] private bool showOverlay;
    [ObservableProperty] private bool alwaysOnTop = true;
    [ObservableProperty] private bool isLocked;
    [ObservableProperty] private bool isMuted;
    [ObservableProperty] private double rmsNormalized;
    [ObservableProperty] private double rmsDb = double.NegativeInfinity;
    [ObservableProperty] private double thresholdNormalized;
    [ObservableProperty] private bool isGateOpen;

    partial void OnShowOverlayChanged(bool value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.ShowOverlay = value);
    }

    partial void OnAlwaysOnTopChanged(bool value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.OverlayAlwaysOnTop = value);
    }

    partial void OnIsLockedChanged(bool value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.OverlayLocked = value);
    }

    [RelayCommand]
    private void ToggleLock() => IsLocked = !IsLocked;

    private void OnLevel(object? sender, AudioLevel level) => DispatcherHelper.Post(() =>
    {
        RmsNormalized = Norm(level.RmsDb);
        RmsDb = level.RmsDb;
    });

    private void OnGate(object? sender, GateState s) => DispatcherHelper.Post(() => IsGateOpen = s == GateState.Open);

    private void OnSettingsChanged(object? sender, AppSettings s) => DispatcherHelper.Post(() => Pull(s));

    private void Pull(AppSettings s)
    {
        _suppress = true;
        ShowOverlay = s.ShowOverlay;
        AlwaysOnTop = s.OverlayAlwaysOnTop;
        IsLocked = s.OverlayLocked;
        IsMuted = s.Muted;
        ThresholdNormalized = Norm(s.ThresholdDb);
        _suppress = false;
    }

    private static double Norm(double db)
    {
        const double floor = -60.0;
        if (db <= floor) return 0;
        if (db >= 0) return 1;
        return (db - floor) / (0 - floor);
    }

    public void Dispose()
    {
        _audio.LevelMeasured -= OnLevel;
        _engine.StateChanged -= OnGate;
        _settings.SettingsChanged -= OnSettingsChanged;
    }
}
