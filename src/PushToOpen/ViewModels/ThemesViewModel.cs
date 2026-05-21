using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PushToOpen.Models;
using PushToOpen.Services;
using PushToOpen.Utilities;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PushToOpen.ViewModels;

public sealed partial class ThemesViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themes;
    private bool _suppress;

    public ThemesViewModel(ISettingsService settings, IThemeService themes)
    {
        _settings = settings;
        _themes = themes;
        Themes = new ObservableCollection<ThemeDefinition>(_themes.Themes);
        _settings.SettingsChanged += OnSettings;
        Pull(_settings.Current);
    }

    public ObservableCollection<ThemeDefinition> Themes { get; }

    [ObservableProperty] private ThemeDefinition? selectedTheme;
    [ObservableProperty] private string? backgroundImagePath;
    [ObservableProperty] private ImageSource? backgroundPreview;
    [ObservableProperty] private double backgroundOpacity = 0.18;
    [ObservableProperty] private string layoutMode = "sidebar";
    [ObservableProperty] private bool isCompactLayout;

    partial void OnSelectedThemeChanged(ThemeDefinition? value)
    {
        if (_suppress || value is null) return;
        _themes.Apply(value.Name);
        _settings.Mutate(s => s.ThemeName = value.Name);
    }

    partial void OnBackgroundOpacityChanged(double value)
    {
        if (_suppress) return;
        _settings.Mutate(s => s.BackgroundImageOpacity = value);
    }

    partial void OnIsCompactLayoutChanged(bool value)
    {
        if (_suppress) return;
        LayoutMode = value ? "compact" : "sidebar";
        _settings.Mutate(s => s.LayoutMode = LayoutMode);
    }

    [RelayCommand]
    private async Task PickBackground()
    {
        try
        {
            var picker = new FileOpenPicker();
            // WinUI 3 file picker needs the window handle to attach to.
            var hwnd = WindowNative.GetWindowHandle(App.Current.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            BackgroundImagePath = file.Path;
            _settings.Mutate(s => s.BackgroundImagePath = file.Path);
            LoadPreview(file.Path);
        }
        catch { /* picker can throw if window handle weird — silently ignore */ }
    }

    [RelayCommand]
    private void ClearBackground()
    {
        BackgroundImagePath = null;
        BackgroundPreview = null;
        _settings.Mutate(s => s.BackgroundImagePath = null);
    }

    private void LoadPreview(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.UriSource = new Uri(path);
            BackgroundPreview = bmp;
        }
        catch { BackgroundPreview = null; }
    }

    private void OnSettings(object? sender, AppSettings s) => DispatcherHelper.Post(() => Pull(s));

    private void Pull(AppSettings s)
    {
        _suppress = true;
        SelectedTheme = _themes.Find(s.ThemeName) ?? _themes.Themes[0];
        BackgroundImagePath = s.BackgroundImagePath;
        BackgroundOpacity = s.BackgroundImageOpacity;
        LayoutMode = s.LayoutMode ?? "sidebar";
        IsCompactLayout = string.Equals(LayoutMode, "compact", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(BackgroundImagePath) && File.Exists(BackgroundImagePath))
            LoadPreview(BackgroundImagePath);
        else
            BackgroundPreview = null;
        _suppress = false;
    }

    public void Dispose() => _settings.SettingsChanged -= OnSettings;
}
