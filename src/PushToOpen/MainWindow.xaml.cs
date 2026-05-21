using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PushToOpen.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace PushToOpen;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settings;
    private bool _suppressClose;

    public MainWindow()
    {
        InitializeComponent();
        _settings = App.Services.GetRequiredService<ISettingsService>();
        ShowFromTrayCommand = new RelayCommand(ShowFromTray);
        ExitCommand = new RelayCommand(ExitApplication);
        PresetCommand = new RelayCommand<string>(ApplyPreset);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(Shell.TitleBarRegion);

        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Title = "PushToOpen";
        appWindow.Resize(new SizeInt32(1080, 720));
        appWindow.Closing += OnAppWindowClosing;
        try { appWindow.SetIcon("Assets/VMic.ico"); } catch { }

        SetCornerPreference(hwnd, DWMWCP_ROUND);
    }

    public RelayCommand ShowFromTrayCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand<string> PresetCommand { get; }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_suppressClose) return;
        if (_settings.Current.MinimizeToTray)
        {
            args.Cancel = true;
            HideToTray();
            return;
        }
        // Real close — tear everything down so process terminates.
        _suppressClose = true;
        try { TrayIcon?.Dispose(); } catch { }
        try { App.Current.HideOverlay(); } catch { }
        try { App.Services.GetService<IInputSimulator>()?.Release(); } catch { }
        try { App.Services.GetService<IInputSimulator>()?.Dispose(); } catch { }
        Environment.Exit(0);
    }

    public void HideToTray()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, SW_HIDE);
    }

    private void ShowFromTray()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, SW_RESTORE);
        ShowWindow(hwnd, SW_SHOW);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        try { appWindow.Show(); } catch { }
        try { appWindow.MoveInZOrderAtTop(); } catch { }
        SetForegroundWindow(hwnd);
    }

    private void ExitApplication()
    {
        _suppressClose = true;
        try { TrayIcon?.Dispose(); } catch { }
        try
        {
            var settings = App.Services.GetService<ISettingsService>();
            if (settings is not null && App.Current._overlaySettingsHandler is not null)
                settings.SettingsChanged -= App.Current._overlaySettingsHandler;
        }
        catch { }
        try { App.Current.HideOverlay(); } catch { }
        try { App.Services.GetService<IInputSimulator>()?.Release(); } catch { }
        try { App.Services.GetService<IInputSimulator>()?.Dispose(); } catch { }
        // Force-terminate — App.Exit() is unreliable while secondary windows exist
        // and Click handlers from H.NotifyIcon flyouts don't always run on UI thread.
        Environment.Exit(0);
    }

    private void ApplyPreset(string? tag)
    {
        if (!string.IsNullOrEmpty(tag)
            && double.TryParse(tag, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var db))
        {
            _settings.Mutate(s => s.ThresholdDb = db);
        }
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const uint DWMWCP_ROUND = 2;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attribute, ref uint value, uint size);

    private static void SetCornerPreference(IntPtr hwnd, uint pref)
    {
        try { DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(uint)); } catch { }
    }
}
