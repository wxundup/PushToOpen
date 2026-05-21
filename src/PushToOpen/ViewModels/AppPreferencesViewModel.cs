using CommunityToolkit.Mvvm.ComponentModel;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class AppPreferencesViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IStartupService _startup;
    private bool _suppress;

    public AppPreferencesViewModel(ISettingsService settings, IStartupService startup)
    {
        _settings = settings;
        _startup = startup;
        _settings.SettingsChanged += OnSettings;
        Pull(_settings.Current);
    }

    [ObservableProperty] private bool minimizeToTray = true;
    [ObservableProperty] private bool startMinimized;
    [ObservableProperty] private bool launchOnStartup;

    partial void OnMinimizeToTrayChanged(bool value){ if (!_suppress) _settings.Mutate(s => s.MinimizeToTray = value); }
    partial void OnStartMinimizedChanged(bool value){ if (!_suppress) _settings.Mutate(s => s.StartMinimized = value); }
    partial void OnLaunchOnStartupChanged(bool value)
    {
        if (_suppress) return;
        _startup.SetEnabled(value);
        _settings.Mutate(s => s.LaunchOnStartup = value);
    }

    private void OnSettings(object? sender, AppSettings s) => DispatcherHelper.Post(() => Pull(s));

    private void Pull(AppSettings s)
    {
        _suppress = true;
        MinimizeToTray = s.MinimizeToTray;
        StartMinimized = s.StartMinimized;
        LaunchOnStartup = _startup.IsEnabled();
        _suppress = false;
    }

    public void Dispose() => _settings.SettingsChanged -= OnSettings;
}
