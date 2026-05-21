using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class HotkeyViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IHotkeyCaptureService _capture;
    private bool _suppress;

    public HotkeyViewModel(ISettingsService settings, IHotkeyCaptureService capture)
    {
        _settings = settings;
        _capture = capture;
        _capture.Captured += OnCaptured;
        _capture.Cancelled += OnCancelled;
        _settings.SettingsChanged += OnSettingsChanged;
        Pull(_settings.Current);
    }

    [ObservableProperty] private string keyDisplay = "V";
    [ObservableProperty] private int attackMs = 25;
    [ObservableProperty] private int releaseMs = 220;
    [ObservableProperty] private int debounceMs = 40;
    [ObservableProperty] private bool isCapturing;

    [RelayCommand]
    private void StartCapture()
    {
        IsCapturing = true;
        _capture.Start();
    }

    [RelayCommand]
    private void CancelCapture()
    {
        _capture.Cancel();
        IsCapturing = false;
    }

    private void OnCaptured(object? sender, KeyBindingInfo key) => DispatcherHelper.Post(() =>
    {
        IsCapturing = false;
        KeyDisplay = key.DisplayName;
        _settings.Mutate(s => s.Hotkey = key);
    });

    private void OnCancelled(object? sender, EventArgs e) => DispatcherHelper.Post(() => IsCapturing = false);

    partial void OnAttackMsChanged(int value)  { if (!_suppress) _settings.Mutate(s => s.AttackMs = value); }
    partial void OnReleaseMsChanged(int value) { if (!_suppress) _settings.Mutate(s => s.ReleaseMs = value); }
    partial void OnDebounceMsChanged(int value){ if (!_suppress) _settings.Mutate(s => s.DebounceMs = value); }

    private void OnSettingsChanged(object? sender, AppSettings s) => DispatcherHelper.Post(() => Pull(s));

    private void Pull(AppSettings s)
    {
        _suppress = true;
        KeyDisplay = s.Hotkey.DisplayName;
        AttackMs = s.AttackMs;
        ReleaseMs = s.ReleaseMs;
        DebounceMs = s.DebounceMs;
        _suppress = false;
    }

    public void Dispose()
    {
        _capture.Captured -= OnCaptured;
        _capture.Cancelled -= OnCancelled;
        _settings.SettingsChanged -= OnSettingsChanged;
    }
}
