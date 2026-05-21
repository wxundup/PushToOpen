using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class AudioViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IAudioCaptureService _audio;
    private bool _suppress;

    public AudioViewModel(ISettingsService settings, IAudioCaptureService audio)
    {
        _settings = settings;
        _audio = audio;
        Devices = new ObservableCollection<AudioDeviceInfo>();

        _audio.DevicesChanged += OnDevicesChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        PullSettings(_settings.Current);
        OnDevicesChanged(this, _audio.Devices);
    }

    public ObservableCollection<AudioDeviceInfo> Devices { get; }

    [ObservableProperty] private AudioDeviceInfo? selectedDevice;
    [ObservableProperty] private double noiseGateDb = -55;
    [ObservableProperty] private double gainDb = 0;
    [ObservableProperty] private int pollIntervalMs = 10;
    [ObservableProperty] private string? errorMessage;

    partial void OnSelectedDeviceChanged(AudioDeviceInfo? value)
    {
        if (_suppress || value is null) return;
        _settings.Mutate(s => s.InputDeviceId = value.Id);
        _ = _audio.StartAsync(value.Id);
    }

    partial void OnNoiseGateDbChanged(double value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.NoiseGateDb = value);
    }

    partial void OnGainDbChanged(double value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.GainDb = value);
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        try { await _audio.RefreshDevicesAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private void OnDevicesChanged(object? sender, IReadOnlyList<AudioDeviceInfo> list) => DispatcherHelper.Post(() =>
    {
        _suppress = true;
        Devices.Clear();
        foreach (var d in list) Devices.Add(d);
        var targetId = _settings.Current.InputDeviceId;
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == targetId)
                      ?? Devices.FirstOrDefault(d => d.IsDefault)
                      ?? Devices.FirstOrDefault();
        _suppress = false;
    });

    private void OnSettingsChanged(object? sender, AppSettings s) => DispatcherHelper.Post(() => PullSettings(s));

    private void PullSettings(AppSettings s)
    {
        _suppress = true;
        NoiseGateDb = s.NoiseGateDb;
        GainDb = s.GainDb;
        PollIntervalMs = s.PollIntervalMs;
        _suppress = false;
    }

    public void Dispose()
    {
        _audio.DevicesChanged -= OnDevicesChanged;
        _settings.SettingsChanged -= OnSettingsChanged;
    }
}
