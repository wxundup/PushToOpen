using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IAudioCaptureService _audio;
    private readonly IThresholdEngine _engine;
    private readonly IInputSimulator _input;
    private bool _suppressSettingsPush;

    public HomeViewModel(
        ISettingsService settings,
        IAudioCaptureService audio,
        IThresholdEngine engine,
        IInputSimulator input)
    {
        _settings = settings;
        _audio = audio;
        _engine = engine;
        _input = input;

        _audio.LevelMeasured += OnLevel;
        _engine.StateChanged += OnGateState;
        _settings.SettingsChanged += OnSettingsChanged;

        PullFromSettings(_settings.Current);
    }

    [ObservableProperty] private double rmsDb = -120;
    [ObservableProperty] private double peakDb = -120;
    [ObservableProperty] private double rmsNormalized;
    [ObservableProperty] private double peakNormalized;
    [ObservableProperty] private double thresholdDb = -38;
    [ObservableProperty] private double thresholdNormalized;
    [ObservableProperty] private bool enabled = true;
    [ObservableProperty] private bool muted;
    [ObservableProperty] private bool isGateOpen;
    [ObservableProperty] private string deviceName = "—";

    partial void OnThresholdDbChanged(double value)
    {
        ThresholdNormalized = DbToNormalized(value);
        if (_suppressSettingsPush) return;
        _settings.Mutate(s => s.ThresholdDb = value);
    }

    partial void OnEnabledChanged(bool value)
    {
        if (_suppressSettingsPush) return;
        _settings.Mutate(s => s.Enabled = value);
    }

    partial void OnMutedChanged(bool value)
    {
        if (_suppressSettingsPush) return;
        _settings.Mutate(s => s.Muted = value);
    }

    private void OnSettingsChanged(object? sender, AppSettings s) =>
        DispatcherHelper.Post(() => PullFromSettings(s));

    private void PullFromSettings(AppSettings s)
    {
        _suppressSettingsPush = true;
        ThresholdDb = s.ThresholdDb;
        Enabled = s.Enabled;
        Muted = s.Muted;
        DeviceName = _audio.CurrentDevice?.Name ?? "—";
        _suppressSettingsPush = false;
    }

    private void OnLevel(object? sender, AudioLevel level) => DispatcherHelper.Post(() =>
    {
        RmsDb = level.RmsDb;
        PeakDb = level.PeakDb;
        RmsNormalized = DbToNormalized(level.RmsDb);
        PeakNormalized = DbToNormalized(level.PeakDb);
        DeviceName = _audio.CurrentDevice?.Name ?? "—";
    });

    private void OnGateState(object? sender, GateState state) => DispatcherHelper.Post(() =>
    {
        IsGateOpen = state == GateState.Open;
    });

    private static double DbToNormalized(double db)
    {
        const double floor = -60.0;
        const double ceil = 0.0;
        if (double.IsNaN(db) || db <= floor) return 0;
        if (db >= ceil) return 1;
        return (db - floor) / (ceil - floor);
    }

    [RelayCommand]
    private void ApplyPreset(string preset)
    {
        ThresholdDb = preset switch
        {
            "whisper" => -50,
            "soft"    => -42,
            "normal"  => -34,
            "loud"    => -24,
            _ => ThresholdDb,
        };
    }

    public void Dispose()
    {
        _audio.LevelMeasured -= OnLevel;
        _engine.StateChanged -= OnGateState;
        _settings.SettingsChanged -= OnSettingsChanged;
    }
}
