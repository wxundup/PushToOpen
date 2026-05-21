using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
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
    private bool _dragging;
    private PointInt32 _dragStartWindow;
    private POINT _dragStartCursor;

    public OverlayWindow()
    {
        InitializeComponent();
        _settings = App.Services.GetRequiredService<ISettingsService>();

        _hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(id);
        _appWindow.IsShownInSwitchers = false;
        _appWindow.Title = "PushToOpen overlay";
        _appWindow.Resize(new SizeInt32(300, 130));

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
        WireDrag();

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

    private void WireDrag()
    {
        var root = OverlayHost.Root as FrameworkElement;
        if (root is null) return;

        root.PointerPressed += OnDragStart;
        root.PointerMoved += OnDragMove;
        root.PointerReleased += OnDragEnd;
        root.PointerCaptureLost += OnDragCancel;
    }

    private void OnDragStart(object sender, PointerRoutedEventArgs e)
    {
        if (OverlayHost.ViewModel.IsLocked) return;
        if (sender is not UIElement el) return;
        var point = e.GetCurrentPoint(el);
        if (!point.Properties.IsLeftButtonPressed) return;

        if (!GetCursorPos(out _dragStartCursor)) return;
        _dragStartWindow = _appWindow.Position;
        _dragging = true;
        el.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnDragMove(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        if (!GetCursorPos(out var now)) return;
        int dx = now.X - _dragStartCursor.X;
        int dy = now.Y - _dragStartCursor.Y;
        _appWindow.Move(new PointInt32(_dragStartWindow.X + dx, _dragStartWindow.Y + dy));
    }

    private void OnDragEnd(object sender, PointerRoutedEventArgs e) => EndDrag(sender, e.Pointer);
    private void OnDragCancel(object sender, PointerRoutedEventArgs e) => EndDrag(sender, e.Pointer);

    private void EndDrag(object sender, Pointer? pointer)
    {
        if (!_dragging) return;
        _dragging = false;
        if (sender is UIElement el && pointer is not null)
        {
            try { el.ReleasePointerCapture(pointer); } catch { }
        }
    }

    private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const uint DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attribute, ref uint value, uint size);

    private static void SetCornerPreference(IntPtr hwnd, uint pref)
    {
        try { DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(uint)); } catch { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
