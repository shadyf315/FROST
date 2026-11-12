using System.Windows;

namespace FROST.Core;

/// <summary>
/// Owns the desktop snowfall overlay. All methods are called on the WPF UI thread.
/// </summary>
public sealed class SnowEngine : IDisposable
{
    private SnowOverlayWindow? _overlay;

    public bool IsEnabled => _overlay is not null;
    public int Density { get; private set; } = 85;
    public double Speed { get; private set; } = 1.0;

    public void Enable()
    {
        if (_overlay is not null) return;

        _overlay = new SnowOverlayWindow(Density, Speed);
        _overlay.Closed += (_, _) => _overlay = null;
        _overlay.Show();
    }

    public void Disable()
    {
        var overlay = _overlay;
        _overlay = null;
        overlay?.Close();
    }

    public void Toggle()
    {
        if (IsEnabled) Disable();
        else Enable();
    }

    public void Configure(int density, double speed)
    {
        Density = Math.Clamp(density, 10, 220);
        Speed = Math.Clamp(speed, 0.2, 3.0);
        _overlay?.Configure(Density, Speed);
    }

    public void Dispose() => Disable();
}
