using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class WindowEnumerator : IWindowEnumerator
{
    public IReadOnlyList<WindowInfo> EnumerateTopLevelWindows()
    {
        var byProcess = new Dictionary<string, WindowInfo>(StringComparer.OrdinalIgnoreCase);

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            if (IsCloaked(hwnd)) return true;

            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return true;

            var sb = new StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            // Skip tool windows / non-app windows.
            uint exStyle = (uint)GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0) return true;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return true;

            string? processName = null;
            string? exePath = null;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName + ".exe";
                try { exePath = proc.MainModule?.FileName; } catch { }
            }
            catch { return true; }

            // Filter the obvious system shells so the list is useful.
            if (processName is null) return true;
            if (processName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase)
                && title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase))
                return true;
            if (processName.Equals("PushToOpen.exe", StringComparison.OrdinalIgnoreCase))
                return true;

            // Deduplicate by process — one entry per app, use longest title found.
            if (byProcess.TryGetValue(processName, out var existing))
            {
                if (title.Length > existing.Title.Length)
                {
                    byProcess[processName] = new WindowInfo
                    {
                        Title = title,
                        ProcessName = processName,
                        ExePath = exePath ?? existing.ExePath,
                    };
                }
            }
            else
            {
                byProcess[processName] = new WindowInfo
                {
                    Title = title,
                    ProcessName = processName,
                    ExePath = exePath,
                };
            }
            return true;
        }, IntPtr.Zero);

        return byProcess.Values
            .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCloaked(IntPtr hwnd)
    {
        try
        {
            int cloaked = 0;
            int hr = DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
            return hr == 0 && cloaked != 0;
        }
        catch { return false; }
    }

    private const uint GWL_EXSTYLE = unchecked((uint)-20);
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_APPWINDOW = 0x00040000;
    private const uint DWMWA_CLOAKED = 14;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, uint nIndex);
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, uint attr, out int pvAttribute, int cbAttribute);
}
