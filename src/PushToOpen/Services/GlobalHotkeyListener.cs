using System.Runtime.InteropServices;
using PushToOpen.Models;

namespace PushToOpen.Services;

/// <summary>
/// Low-level keyboard + mouse hook listening for a single configured key.
/// Fires Triggered on key-down (no auto-repeat) when binding matches.
/// </summary>
public sealed class GlobalHotkeyListener : IGlobalHotkeyListener
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    private readonly object _gate = new();
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private LowLevelProc? _keyboardProc;
    private LowLevelProc? _mouseProc;
    private KeyBindingInfo? _binding;
    private bool _keyHeld;
    private bool _disposed;

    public event EventHandler? Triggered;

    public void SetBinding(KeyBindingInfo? key)
    {
        lock (_gate) _binding = key?.Clone();
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed) return;
            if (_keyboardHook != IntPtr.Zero) return;
            _keyboardProc = KeyboardHookProc;
            _mouseProc = MouseHookProc;
            var module = GetModuleHandle(null);
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
            _mouseHook    = SetWindowsHookEx(WH_MOUSE_LL,    _mouseProc,    module, 0);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
            if (_mouseHook    != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook);    _mouseHook = IntPtr.Zero; }
            _keyboardProc = null;
            _mouseProc = null;
        }
    }

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var binding = _binding;
            if (binding is not null && binding.VirtualKey != 0)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if ((ushort)kb.vkCode == binding.VirtualKey)
                    {
                        if (!_keyHeld)
                        {
                            _keyHeld = true;
                            try { Triggered?.Invoke(this, EventArgs.Empty); } catch { }
                        }
                    }
                }
                else if (msg == 0x0101 /*WM_KEYUP*/ || msg == 0x0105 /*WM_SYSKEYUP*/)
                {
                    if ((ushort)kb.vkCode == binding.VirtualKey) _keyHeld = false;
                }
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var binding = _binding;
            if (binding is not null && binding.VirtualKey != 0 && binding.VirtualKey <= 0x06)
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
                if (vk.HasValue && vk.Value == binding.VirtualKey)
                {
                    if (!_keyHeld)
                    {
                        _keyHeld = true;
                        try { Triggered?.Invoke(this, EventArgs.Empty); } catch { }
                    }
                }
                else if (msg == 0x0202 /*WM_LBUTTONUP*/ || msg == 0x0205 /*WM_RBUTTONUP*/ ||
                         msg == 0x0208 /*WM_MBUTTONUP*/ || msg == 0x020C /*WM_XBUTTONUP*/)
                {
                    _keyHeld = false;
                }
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

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }
        Stop();
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
