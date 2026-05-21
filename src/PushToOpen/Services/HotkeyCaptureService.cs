using System.Runtime.InteropServices;
using PushToOpen.Models;
using PushToOpen.Utilities;

namespace PushToOpen.Services;

public sealed class HotkeyCaptureService : IHotkeyCaptureService
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private LowLevelProc? _keyboardProc;
    private LowLevelProc? _mouseProc;
    private bool _disposed;

    public bool IsCapturing => _keyboardHook != IntPtr.Zero;

    public event EventHandler<KeyBindingInfo>? Captured;
    public event EventHandler? Cancelled;

    public void Start()
    {
        if (IsCapturing) return;
        _keyboardProc = KeyboardHookProc;
        _mouseProc = MouseHookProc;
        var module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook    = SetWindowsHookEx(WH_MOUSE_LL,    _mouseProc,    module, 0);
    }

    public void Cancel()
    {
        bool wasCapturing = IsCapturing;
        UnhookAll();
        if (wasCapturing) Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            ushort vk = (ushort)kb.vkCode;
            if (vk == 0x1B) // Escape cancels
            {
                UnhookAll();
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var binding = KeyCodeMap.FromVirtualKey(vk);
                UnhookAll();
                Captured?.Invoke(this, binding);
            }
            return (IntPtr)1;
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            ushort? vk = msg switch
            {
                WM_LBUTTONDOWN => (ushort)0x01,
                WM_RBUTTONDOWN => (ushort)0x02,
                WM_MBUTTONDOWN => (ushort)0x04,
                WM_XBUTTONDOWN => ResolveXButton(lParam),
                _ => null,
            };
            if (vk.HasValue)
            {
                var binding = new KeyBindingInfo
                {
                    VirtualKey = vk.Value,
                    ScanCode = 0,
                    IsExtended = false,
                    DisplayName = vk.Value switch
                    {
                        0x01 => "Mouse L", 0x02 => "Mouse R", 0x04 => "Mouse M",
                        0x05 => "Mouse X1", 0x06 => "Mouse X2",
                        _ => $"Mouse {vk.Value:X2}"
                    }
                };
                UnhookAll();
                Captured?.Invoke(this, binding);
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private static ushort ResolveXButton(IntPtr lParam)
    {
        var ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        int xButton = (int)((ms.mouseData >> 16) & 0xFFFF);
        return xButton == 1 ? (ushort)0x05 : (ushort)0x06;
    }

    private void UnhookAll()
    {
        if (_keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
        if (_mouseHook    != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook);    _mouseHook = IntPtr.Zero; }
        _keyboardProc = null;
        _mouseProc = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnhookAll();
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }
}
