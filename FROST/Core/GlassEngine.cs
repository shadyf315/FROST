using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FROST.Core;

/// <summary>
/// Applies supported Windows 11 DWM styling to FROST-owned windows.
/// This does not attempt to modify third-party application windows.
/// </summary>
public static class GlassEngine
{
    private const uint DwmwaUseImmersiveDarkMode = 20;
    private const uint DwmwaWindowCornerPreference = 33;
    private const uint DwmwaSystemBackdropType = 38;
    private const int DwmwcpRound = 2;
    private const int DwmsbtMainWindow = 2;

    public static void ApplyTo(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            var darkMode = 1;
            var roundedCorners = DwmwcpRound;

            TrySetAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode);
            TrySetAttribute(handle, DwmwaWindowCornerPreference, ref roundedCorners);

            // Mica system backdrop is available on Windows 11 22H2 and newer.
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
            {
                var backdrop = DwmsbtMainWindow;
                TrySetAttribute(handle, DwmwaSystemBackdropType, ref backdrop);
            }
        };
    }

    private static void TrySetAttribute(IntPtr handle, uint attribute, ref int value)
    {
        try
        {
            _ = DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // DWM is unavailable; the application keeps its regular WPF appearance.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows versions may not expose newer DWM attributes.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        uint attribute,
        ref int value,
        int valueSize);
}
