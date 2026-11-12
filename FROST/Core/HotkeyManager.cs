using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace FROST.Core;

public sealed class HotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int SnowToggleId = 0xF702;

    private readonly HwndSource _source;
    private readonly Action _toggleSnow;
    private bool _snowRegistered;

    public HotkeyManager(IntPtr windowHandle, Action toggleSnow)
    {
        _toggleSnow = toggleSnow;
        _source = HwndSource.FromHwnd(windowHandle);
        _source.AddHook(WindowProcedure);
        _snowRegistered = RegisterHotKey(windowHandle, SnowToggleId, ModControl | ModAlt, 0x4E); // Ctrl+Alt+N
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == SnowToggleId)
        {
            _toggleSnow();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source.RemoveHook(WindowProcedure);
        if (_snowRegistered) UnregisterHotKey(_source.Handle, SnowToggleId);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}