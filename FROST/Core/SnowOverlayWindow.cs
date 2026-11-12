using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FROST.Core;

/// <summary>
/// Transparent, click-through overlay covering the complete virtual desktop.
/// It never takes focus and does not intercept mouse input.
/// </summary>
public sealed class SnowOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private readonly Canvas _canvas = new();
    private readonly List<Snowflake> _flakes = [];
    private readonly Random _random = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastFrame;
    private double _speed = 1;
    private bool _rendering;

    public SnowOverlayWindow(int density, double speed)
    {
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
        Loaded += (_, _) => StartRendering();
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
        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoActivate | SwpShowWindow | 0x0001 | 0x0002);
    }

    private void StartRendering()
    {
        if (_rendering) return;
        _rendering = true;
        _lastFrame = _clock.Elapsed;
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
    }

    private static void Position(Snowflake flake)
    {
        if (flake.Shape.RenderTransform is TranslateTransform transform)
            transform.X = flake.X - flake.Radius;
        if (flake.Shape.RenderTransform is TranslateTransform transformY)
            transformY.Y = flake.Y - flake.Radius;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRendering();
        base.OnClosed(e);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

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
