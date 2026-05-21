using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PushToOpen.Services;

/// <summary>
/// Polls the foreground window every 250ms and reports the owning process name.
/// Polling avoids the cross-thread cost of SetWinEventHook and is plenty fast
/// for an "is the target app focused" check.
/// </summary>
public sealed class ForegroundWatcher : IForegroundWatcher
{
    private readonly object _gate = new();
    private System.Threading.Timer? _timer;
    private string? _current;
    private bool _disposed;

    public string? ForegroundProcessName
    {
        get { lock (_gate) return _current; }
    }

    public event EventHandler<string?>? ForegroundChanged;

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _timer ??= new System.Threading.Timer(Tick, null, 0, 250);
        }
    }

    public void Stop()
    {
        System.Threading.Timer? t;
        lock (_gate) { t = _timer; _timer = null; }
        try { t?.Dispose(); } catch { }
    }

    private void Tick(object? _)
    {
        string? name = null;
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid != 0)
                {
                    try
                    {
                        using var p = Process.GetProcessById((int)pid);
                        name = (p.ProcessName + ".exe").ToLowerInvariant();
                    }
                    catch { }
                }
            }
        }
        catch { }

        bool changed = false;
        lock (_gate)
        {
            if (!string.Equals(_current, name, StringComparison.OrdinalIgnoreCase))
            {
                _current = name;
                changed = true;
            }
        }
        if (changed)
        {
            try { ForegroundChanged?.Invoke(this, name); } catch { }
        }
    }

    public void Dispose()
    {
        lock (_gate) { if (_disposed) return; _disposed = true; }
        Stop();
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
