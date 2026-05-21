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

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(Shell.TitleBarRegion);

        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Title = "PushToOpen";
        appWindow.Resize(new SizeInt32(1080, 720));
        appWindow.Closing += OnAppWindowClosing;

        SetCornerPreference(hwnd, DWMWCP_ROUND);
    }

    public RelayCommand ShowFromTrayCommand { get; }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_suppressClose) return;
        if (_settings.Current.MinimizeToTray)
        {
            args.Cancel = true;
            HideToTray();
        }
    }

    public void HideToTray()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, SW_HIDE);
    }

    private void ShowFromTray()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hwnd, SW_SHOW);
        SetForegroundWindow(hwnd);
    }

    private void OnTrayShow(object sender, RoutedEventArgs e) => ShowFromTray();

    private void OnTrayExit(object sender, RoutedEventArgs e)
    {
        _suppressClose = true;
        TrayIcon?.Dispose();
        App.Services.GetService<IInputSimulator>()?.Release();
        App.Current.Exit();
    }

    private void OnTrayPreset(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tag
            && double.TryParse(tag, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var db))
        {
            _settings.Mutate(s => s.ThresholdDb = db);
        }
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
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
