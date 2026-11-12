using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FROST.Services;

public sealed record WindowInfo(IntPtr Handle, string Title, string ProcessName);

public static class WindowService
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const uint DwmaCloaked = 14;
    private const uint GwOwner = 4;
    private static readonly IntPtr HwndShell = GetShellWindow();

    public static IReadOnlyList<WindowInfo> GetOpenWindows()
    {
        var windows = new List<WindowInfo>();
        var ownProcessId = Environment.ProcessId;

        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || handle == HwndShell)
                return true;

            if (GetWindow(handle, GwOwner) != IntPtr.Zero)
                return true;

            var style = GetWindowLong(handle, GwlExStyle);
            if ((style & WsExToolWindow) != 0)
                return true;

            if (IsCloaked(handle))
                return true;

            var length = GetWindowTextLength(handle);
            if (length <= 0)
                return true;

            var title = new StringBuilder(length + 1);
            GetWindowText(handle, title, title.Capacity);
            var text = title.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                return true;

            GetWindowThreadProcessId(handle, out var processId);
            if (processId == 0 || processId == ownProcessId)
                return true;

            var processName = "Application";
            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch
            {
                // A window can disappear while the list is being collected.
            }

            windows.Add(new WindowInfo(handle, text, processName));
            return true;
        }, IntPtr.Zero);

        return windows
            .OrderBy(window => window.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool Activate(WindowInfo window)
    {
        if (window.Handle == IntPtr.Zero || !IsWindow(window.Handle))
            return false;

        ShowWindow(window.Handle, 9); // SW_RESTORE
        return SetForegroundWindow(window.Handle);
    }

    private static bool IsCloaked(IntPtr handle)
    {
        try
        {
            return DwmGetWindowAttribute(handle, DwmaCloaked, out int cloaked, sizeof(int)) == 0
                   && cloaked != 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr handle, uint command);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr handle, int index);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr handle, uint attribute, out int value, int size);
}
