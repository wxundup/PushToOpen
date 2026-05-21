using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;

namespace PushToOpen.ViewModels;

public sealed partial class WindowViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IWindowEnumerator _enumerator;
    private readonly IForegroundWatcher _foreground;
    private bool _suppress;

    public WindowViewModel(
        ISettingsService settings,
        IWindowEnumerator enumerator,
        IForegroundWatcher foreground)
    {
        _settings = settings;
        _enumerator = enumerator;
        _foreground = foreground;
        Windows = new ObservableCollection<WindowInfo>();
        _settings.SettingsChanged += OnSettings;
        _foreground.ForegroundChanged += OnForeground;
        Refresh();
        Pull(_settings.Current);
        CurrentForeground = _foreground.ForegroundProcessName ?? "—";
    }

    public ObservableCollection<WindowInfo> Windows { get; }

    [ObservableProperty] private bool restrictionEnabled;
    [ObservableProperty] private WindowInfo? selectedWindow;
    [ObservableProperty] private string targetDisplayName = "(none)";
    [ObservableProperty] private string currentForeground = "—";
    [ObservableProperty] private bool currentlyAllowed;

    partial void OnRestrictionEnabledChanged(bool value)
    {
        if (_suppress) return;
        if (!value)
        {
            _settings.Mutate(s =>
            {
                s.RestrictToProcessName = null;
                s.RestrictToProcessDisplayName = null;
            });
        }
        else if (SelectedWindow is not null)
        {
            CommitSelection(SelectedWindow);
        }
        RecomputeAllowed();
    }

    partial void OnSelectedWindowChanged(WindowInfo? value)
    {
        if (_suppress || value is null) return;
        if (RestrictionEnabled) CommitSelection(value);
        RecomputeAllowed();
    }

    private void CommitSelection(WindowInfo w)
    {
        _settings.Mutate(s =>
        {
            s.RestrictToProcessName = w.ProcessName.ToLowerInvariant();
            s.RestrictToProcessDisplayName = string.IsNullOrWhiteSpace(w.Title) ? w.ProcessName : w.Title;
        });
        TargetDisplayName = s_displayFor(w);
    }

    private static string s_displayFor(WindowInfo w)
        => string.IsNullOrWhiteSpace(w.Title) ? w.ProcessName : w.Title;

    [RelayCommand]
    private void Refresh()
    {
        var list = _enumerator.EnumerateTopLevelWindows();
        _suppress = true;
        Windows.Clear();
        foreach (var w in list) Windows.Add(w);

        // Re-select current target if still running.
        var target = _settings.Current.RestrictToProcessName;
        if (!string.IsNullOrEmpty(target))
        {
            SelectedWindow = Windows.FirstOrDefault(w =>
                string.Equals(w.ProcessName, target, StringComparison.OrdinalIgnoreCase));
        }
        _suppress = false;
        RecomputeAllowed();
    }

    [RelayCommand]
    private void ClearTarget()
    {
        RestrictionEnabled = false;
        SelectedWindow = null;
        _settings.Mutate(s =>
        {
            s.RestrictToProcessName = null;
            s.RestrictToProcessDisplayName = null;
        });
        TargetDisplayName = "(none)";
        RecomputeAllowed();
    }

    private void OnSettings(object? sender, AppSettings s) => DispatcherHelper.Post(() => Pull(s));

    private void OnForeground(object? sender, string? proc) => DispatcherHelper.Post(() =>
    {
        CurrentForeground = proc ?? "—";
        RecomputeAllowed();
    });

    private void Pull(AppSettings s)
    {
        _suppress = true;
        var target = s.RestrictToProcessName;
        RestrictionEnabled = !string.IsNullOrEmpty(target);
        TargetDisplayName = !string.IsNullOrEmpty(s.RestrictToProcessDisplayName)
            ? s.RestrictToProcessDisplayName!
            : (target ?? "(none)");
        if (!string.IsNullOrEmpty(target))
        {
            SelectedWindow = Windows.FirstOrDefault(w =>
                string.Equals(w.ProcessName, target, StringComparison.OrdinalIgnoreCase));
        }
        _suppress = false;
        RecomputeAllowed();
    }

    private void RecomputeAllowed()
    {
        var t = _settings.Current.RestrictToProcessName;
        if (string.IsNullOrEmpty(t)) { CurrentlyAllowed = true; return; }
        var fg = _foreground.ForegroundProcessName;
        CurrentlyAllowed = fg is not null && string.Equals(fg, t, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _settings.SettingsChanged -= OnSettings;
        _foreground.ForegroundChanged -= OnForeground;
    }
}
