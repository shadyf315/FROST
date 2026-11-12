using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FROST.Core;

/// <summary>
/// Click-through overlay for falling snow and small snow caps on visible application windows.
/// </summary>
public sealed class SnowOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int GaRoot = 2;
    private const uint DwmaExtendedFrameBounds = 9;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr ShellWindow = GetShellWindow();
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private readonly Canvas _canvas = new();
    private readonly List<Snowflake> _flakes = [];
    private readonly Dictionary<IntPtr, SnowCap> _snowCaps = [];
    private readonly Random _random = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastFrame;
    private TimeSpan _nextWindowScan;
    private double _speed = 1;
    private double _scaleX = 1;
    private double _scaleY = 1;
    private int _virtualScreenLeft;
    private int _virtualScreenTop;
    private bool _rendering;

    public SnowOverlayWindow(int density, double speed)
    {
        _virtualScreenLeft = GetSystemMetrics(SmXVirtualScreen);
        _virtualScreenTop = GetSystemMetrics(SmYVirtualScreen);

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        IsHitTestVisible = false;
        Content = _canvas;

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        Configure(density, speed);
        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) =>
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            _scaleX = Math.Max(0.5, dpi.DpiScaleX);
            _scaleY = Math.Max(0.5, dpi.DpiScaleY);
            StartRendering();
            RefreshWindowCaps();
        };
        Closed += (_, _) => StopRendering();
    }

    public void Configure(int density, double speed)
    {
        _speed = Math.Clamp(speed, 0.2, 3.0);
        var target = Math.Clamp(density, 10, 220);

        while (_flakes.Count < target) AddFlake(randomizeHeight: true);
        while (_flakes.Count > target)
        {
            var last = _flakes[^1];
            _canvas.Children.Remove(last.Shape);
            _flakes.RemoveAt(_flakes.Count - 1);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GwlExStyle);
        SetWindowLong(hwnd, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0,
            SwpNoActivate | SwpShowWindow | 0x0001 | 0x0002);
    }

    private void StartRendering()
    {
        if (_rendering) return;
        _rendering = true;
        _lastFrame = _clock.Elapsed;
        _nextWindowScan = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopRendering()
    {
        if (!_rendering) return;
        CompositionTarget.Rendering -= OnRendering;
        _rendering = false;
    }

    private void AddFlake(bool randomizeHeight)
    {
        var radius = 1.0 + _random.NextDouble() * 2.5;
        var shape = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = Brushes.White,
            Opacity = 0.35 + _random.NextDouble() * 0.6,
            IsHitTestVisible = false,
            RenderTransform = new TranslateTransform()
        };

        var flake = new Snowflake
        {
            Shape = shape,
            X = _random.NextDouble() * Math.Max(1, ActualWidth > 0 ? ActualWidth : Width),
            Y = randomizeHeight
                ? _random.NextDouble() * Math.Max(1, ActualHeight > 0 ? ActualHeight : Height)
                : -radius * 3,
            Radius = radius,
            Fall = 22 + _random.NextDouble() * 62,
            Drift = (_random.NextDouble() - 0.5) * 17,
            Phase = _random.NextDouble() * Math.PI * 2
        };

        _flakes.Add(flake);
        _canvas.Children.Add(shape);
        Position(flake);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var delta = Math.Clamp((now - _lastFrame).TotalSeconds, 0, 0.05);
        _lastFrame = now;

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        foreach (var flake in _flakes)
        {
            flake.Y += flake.Fall * _speed * delta;
            flake.Phase += delta * 0.7;
            flake.X += (flake.Drift + Math.Sin(flake.Phase) * 7) * _speed * delta;

            if (flake.Y > height + flake.Radius * 4)
            {
                flake.Y = -flake.Radius * 4;
                flake.X = _random.NextDouble() * width;
            }

            if (flake.X < -10) flake.X = width + 8;
            if (flake.X > width + 10) flake.X = -8;
            Position(flake);
        }

        if (now >= _nextWindowScan)
        {
            _nextWindowScan = now + TimeSpan.FromMilliseconds(320);
            RefreshWindowCaps();
        }
    }

    private void RefreshWindowCaps()
    {
        var visibleHandles = new HashSet<IntPtr>();

        EnumWindows((handle, _) =>
        {
            if (handle == ShellWindow || !IsWindowVisible(handle) || IsIconic(handle))
                return true;

            var extendedStyle = GetWindowLong(handle, GwlExStyle);
            if ((extendedStyle & WsExToolWindow) != 0)
                return true;

            if (GetWindowTextLength(handle) <= 0)
                return true;

            if (!TryGetVisibleBounds(handle, out var bounds))
                return true;

            var widthPx = bounds.Right - bounds.Left;
            var heightPx = bounds.Bottom - bounds.Top;
            if (widthPx < 220 || heightPx < 110)
                return true;

            // Do not draw a cap on a background window hidden by another window.
            var hitPoint = new NativePoint
            {
                X = bounds.Left + widthPx / 2,
                Y = bounds.Top + Math.Min(8, Math.Max(2, heightPx / 10))
            };
            var hitWindow = WindowFromPoint(hitPoint);
            if (hitWindow != IntPtr.Zero)
            {
                var hitRoot = GetAncestor(hitWindow, GaRoot);
                if (hitRoot != IntPtr.Zero && hitRoot != handle)
                    return true;
            }

            var x = (bounds.Left - _virtualScreenLeft) / _scaleX;
            var y = (bounds.Top - _virtualScreenTop) / _scaleY;
            var widthDip = widthPx / _scaleX;

            if (x + widthDip < 0 || x > ActualWidth || y > ActualHeight || y + 20 < 0)
                return true;

            visibleHandles.Add(handle);
            AddOrMoveSnowCap(handle, x, y, widthDip);
            return true;
        }, IntPtr.Zero);

        foreach (var handle in _snowCaps.Keys.ToArray())
        {
            if (visibleHandles.Contains(handle))
                continue;

            _canvas.Children.Remove(_snowCaps[handle].Shape);
            _snowCaps.Remove(handle);
        }
    }

    private bool TryGetVisibleBounds(IntPtr handle, out NativeRect bounds)
    {
        if (DwmGetWindowAttribute(
                handle,
                DwmaExtendedFrameBounds,
                out bounds,
                Marshal.SizeOf<NativeRect>()) == 0)
        {
            return true;
        }

        return GetWindowRect(handle, out bounds);
    }

    private void AddOrMoveSnowCap(IntPtr handle, double x, double y, double width)
    {
        const double capHeight = 15;

        if (!_snowCaps.TryGetValue(handle, out var cap) ||
            Math.Abs(cap.Width - width) > 2)
        {
            if (cap is not null)
                _canvas.Children.Remove(cap.Shape);

            var shape = new System.Windows.Shapes.Path
            {
                Data = CreateSnowDriftGeometry(width, handle),
                Fill = new LinearGradientBrush(
                    Color.FromArgb(248, 250, 253, 255),
                    Color.FromArgb(190, 204, 220, 226),
                    new Point(0, 0),
                    new Point(0, 1)),
                Stroke = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
                StrokeThickness = 0.55,
                Opacity = 0.94,
                IsHitTestVisible = false,
                Width = width,
                Height = capHeight,
                Stretch = Stretch.Fill
            };

            cap = new SnowCap(shape, width);
            _snowCaps[handle] = cap;
            _canvas.Children.Add(shape);
        }

        Canvas.SetLeft(cap.Shape, x);
        Canvas.SetTop(cap.Shape, y - 5);
    }

    private static Geometry CreateSnowDriftGeometry(double width, IntPtr handle)
    {
        const double height = 15;
        var rawHandle = handle.ToInt64();
        var seed = unchecked((int)(rawHandle ^ (rawHandle >> 32)));
        var random = new Random(seed);
        var segmentCount = Math.Clamp((int)(width / 34), 8, 48);
        var segmentWidth = width / segmentCount;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, height), true, true);
            context.LineTo(new Point(0, 7.5 + random.NextDouble() * 1.5), true, false);

            for (var i = 0; i < segmentCount; i++)
            {
                var start = i * segmentWidth;
                var end = (i + 1) * segmentWidth;

                if (random.NextDouble() > 0.45)
                {
                    var moundX = start + segmentWidth * (0.25 + random.NextDouble() * 0.5);
                    var moundY = 2.2 + random.NextDouble() * 3.4;
                    context.LineTo(new Point(moundX - segmentWidth * 0.16, 5.3), true, false);
                    context.LineTo(new Point(moundX, moundY), true, false);
                    context.LineTo(new Point(moundX + segmentWidth * 0.18, 5.1), true, false);
                }

                context.LineTo(
                    new Point(end, 6.0 + random.NextDouble() * 2.3),
                    true,
                    false);
            }

            context.LineTo(new Point(width, height), true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static void Position(Snowflake flake)
    {
        if (flake.Shape.RenderTransform is TranslateTransform transform)
        {
            transform.X = flake.X - flake.Radius;
            transform.Y = flake.Y - flake.Radius;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRendering();
        _snowCaps.Clear();
        base.OnClosed(e);
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed record SnowCap(System.Windows.Shapes.Path Shape, double Width);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect bounds);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, uint attribute, out NativeRect value, int size);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private sealed class Snowflake
    {
        public required Ellipse Shape { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; init; }
        public double Fall { get; init; }
        public double Drift { get; init; }
        public double Phase { get; set; }
    }
}