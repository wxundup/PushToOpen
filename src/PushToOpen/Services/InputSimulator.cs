using System.Runtime.InteropServices;
using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class InputSimulator : IInputSimulator
{
    private readonly object _gate = new();
    private KeyBindingInfo _key = KeyBindingInfo.DefaultPushToTalk;
    private bool _down;
    private bool _disposed;

    public bool IsDown { get { lock (_gate) return _down; } }

    public void Bind(KeyBindingInfo key)
    {
        lock (_gate)
        {
            if (_down) ReleaseInternal();
            _key = key.Clone();
        }
    }

    public void Press()
    {
        lock (_gate)
        {
            if (_disposed || _down) return;
            if (SendKey(true)) _down = true;
        }
    }

    public void Release()
    {
        lock (_gate)
        {
            if (!_down) return;
            ReleaseInternal();
        }
    }

    private void ReleaseInternal()
    {
        SendKey(false);
        _down = false;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            if (_down) ReleaseInternal();
        }
    }

    private bool SendKey(bool down)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = _key.ScanCode == 0 ? _key.VirtualKey : (ushort)0,
                    wScan = _key.ScanCode,
                    dwFlags = (uint)((_key.ScanCode != 0 ? KEYEVENTF_SCANCODE : 0)
                                    | (_key.IsExtended ? KEYEVENTF_EXTENDEDKEY : 0)
                                    | (down ? 0 : KEYEVENTF_KEYUP)),
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
        uint sent = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        return sent == 1;
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }
}
