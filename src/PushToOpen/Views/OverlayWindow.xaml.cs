using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PushToOpen.Services;
using PushToOpen.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace PushToOpen.Views;

public sealed partial class OverlayWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly AppWindow _appWindow;
    private readonly IntPtr _hwnd;

    public OverlayWindow()
    {
        InitializeComponent();
        _settings = App.Services.GetRequiredService<ISettingsService>();

        _hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(id);
        _appWindow.IsShownInSwitchers = false;
        _appWindow.Title = "PushToOpen overlay";
        _appWindow.Resize(new SizeInt32(280, 120));

        if (_appWindow.Presenter is OverlappedPresenter op)
        {
            op.IsResizable = false;
            op.IsMaximizable = false;
            op.IsMinimizable = false;
            op.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            op.IsAlwaysOnTop = _settings.Current.OverlayAlwaysOnTop;
        }

        if (!double.IsNaN(_settings.Current.OverlayX) && !double.IsNaN(_settings.Current.OverlayY))
        {
            _appWindow.Move(new PointInt32((int)_settings.Current.OverlayX, (int)_settings.Current.OverlayY));
        }

        ExtendsContentIntoTitleBar = true;
        SetCornerPreference(_hwnd, DWMWCP_ROUND);
        EnableDragMove();

        _appWindow.Changed += OnAppWindowChanged;
        OverlayHost.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OverlayViewModel.AlwaysOnTop)
                && _appWindow.Presenter is OverlappedPresenter op2)
            {
                op2.IsAlwaysOnTop = OverlayHost.ViewModel.AlwaysOnTop;
            }
        };
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPositionChange) return;
        _settings.Mutate(s =>
        {
            s.OverlayX = sender.Position.X;
            s.OverlayY = sender.Position.Y;
        });
    }

    private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const uint DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attribute, ref uint value, uint size);

    private static void SetCornerPreference(IntPtr hwnd, uint pref)
    {
        try { DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(uint)); } catch { }
    }

    private const int HTCAPTION = 2;
    private const uint WM_NCLBUTTONDOWN = 0x00A1;

    private void EnableDragMove()
    {
        OverlayHost.Root.PointerPressed += (s, e) =>
        {
            try
            {
                var props = e.GetCurrentPoint(OverlayHost.Root).Properties;
                if (props.IsLeftButtonPressed)
                {
                    ReleaseCapture();
                    SendMessage(_hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            }
            catch { }
        };
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
