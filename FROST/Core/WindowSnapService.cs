using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace FROST.Core;

/// <summary>
/// Adds macOS Sequoia-style edge tiling to ordinary captioned Windows windows.
/// Drag a window by its title bar to an edge or corner to preview and apply a layout.
/// </summary>
public sealed class WindowSnapService : IDisposable
{
    private const int VkLeftButton = 0x01;
    private const int GwlStyle = -16;
    private const int WsCaption = 0x00C00000;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const int EdgeThreshold = 18;
    private const int RestoreMoveThreshold = 5;
    private const int WindowGap = 8;

    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromMilliseconds(16),
        Priority = DispatcherPriority.Background
    };
    private readonly SnapPreviewWindow _preview = new();
    private readonly Dictionary<IntPtr, NativeRect> _restoreBounds = [];

    private bool _leftWasDown;
    private bool _tracking;
    private bool _hasMoved;
    private IntPtr _targetWindow;
    private NativePoint _dragStart;

    public WindowSnapService()
    {
        _timer.Tick += OnTimerTick;
    }

    public void Start() => _timer.Start();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var leftDown = (GetAsyncKeyState(VkLeftButton) & 0x8000) != 0;

        if (!leftDown)
        {
            FinishDrag();
            _leftWasDown = false;
            return;
        }

        if (!_leftWasDown)
        {
            _leftWasDown = true;
            TryBeginDrag();
            return;
        }

        if (_tracking)
            UpdateDrag();
    }

    private void TryBeginDrag()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || !IsWindowVisible(hwnd))
            return;

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId)
            return;

        var style = GetWindowLong(hwnd, GwlStyle);
        if ((style & WsCaption) != WsCaption)
            return;

        if (!GetWindowRect(hwnd, out var bounds) || !GetCursorPos(out var cursor))
            return;

        if (cursor.X < bounds.Left || cursor.X > bounds.Right ||
            cursor.Y < bounds.Top || cursor.Y > bounds.Top + 48)
            return;

        _targetWindow = hwnd;
        _dragStart = cursor;
        _tracking = true;
        _hasMoved = false;
        _preview.HidePreview();
    }

    private void UpdateDrag()
    {
        if (!IsWindow(_targetWindow) || GetForegroundWindow() != _targetWindow ||
            !GetCursorPos(out var cursor))
        {
            CancelDrag();
            return;
        }

        if (!_hasMoved)
        {
            var dx = cursor.X - _dragStart.X;
            var dy = cursor.Y - _dragStart.Y;
            if (dx * dx + dy * dy < RestoreMoveThreshold * RestoreMoveThreshold)
                return;

            _hasMoved = true;
            RestorePreviousSize(cursor);
        }

        var monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            _preview.HidePreview();
            return;
        }

        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            _preview.HidePreview();
            return;
        }

        var target = CalculateTarget(cursor, monitorInfo.WorkArea);
        if (target is NativeRect rect)
            _preview.ShowPreview(rect);
        else
            _preview.HidePreview();
    }

    private void RestorePreviousSize(NativePoint cursor)
    {
        if (!_restoreBounds.TryGetValue(_targetWindow, out var previous))
            return;

        var width = Math.Max(320, previous.Right - previous.Left);
        var height = Math.Max(240, previous.Bottom - previous.Top);
        SetWindowPos(
            _targetWindow,
            IntPtr.Zero,
            cursor.X - width / 2,
            cursor.Y - 20,
            width,
            height,
            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
    }

    private static NativeRect? CalculateTarget(NativePoint cursor, NativeRect work)
    {
        var width = work.Right - work.Left;
        var height = work.Bottom - work.Top;
        if (width <= 0 || height <= 0)
            return null;

        var left = cursor.X <= work.Left + EdgeThreshold;
        var right = cursor.X >= work.Right - EdgeThreshold;
        var top = cursor.Y <= work.Top + EdgeThreshold;
        var bottom = cursor.Y >= work.Bottom - EdgeThreshold;
        var midX = work.Left + width / 2;
        var midY = work.Top + height / 2;

        // Corners take priority over the full-height side zones.
        if (left && top) return Area(work.Left, work.Top, midX, midY);
        if (right && top) return Area(midX, work.Top, work.Right, midY);
        if (left && bottom) return Area(work.Left, midY, midX, work.Bottom);
        if (right && bottom) return Area(midX, midY, work.Right, work.Bottom);

        if (left) return Area(work.Left, work.Top, midX, work.Bottom);
        if (right) return Area(midX, work.Top, work.Right, work.Bottom);
        if (top) return Area(work.Left, work.Top, work.Right, work.Bottom);
        if (bottom) return Area(work.Left, midY, work.Right, work.Bottom);

        return null;
    }

    private static NativeRect Area(int left, int top, int right, int bottom)
    {
        // A small gutter keeps adjacent windows visually separated.
        return new NativeRect
        {
            Left = left + WindowGap,
            Top = top + WindowGap,
            Right = right - WindowGap,
            Bottom = bottom - WindowGap
        };
    }

    private void FinishDrag()
    {
        if (!_tracking)
        {
            _preview.HidePreview();
            return;
        }

        var target = _preview.CurrentTarget;
        if (_hasMoved && target is NativeRect bounds && IsWindow(_targetWindow))
        {
            if (!_restoreBounds.ContainsKey(_targetWindow) &&
                GetWindowRect(_targetWindow, out var original))
            {
                _restoreBounds[_targetWindow] = original;
            }

            SetWindowPos(
                _targetWindow,
                IntPtr.Zero,
                bounds.Left,
                bounds.Top,
                bounds.Right - bounds.Left,
                bounds.Bottom - bounds.Top,
                SwpNoZOrder | SwpNoActivate | SwpShowWindow);
        }

        _preview.HidePreview();
        _tracking = false;
        _hasMoved = false;
        _targetWindow = IntPtr.Zero;
    }

    private void CancelDrag()
    {
        _preview.HidePreview();
        _tracking = false;
        _hasMoved = false;
        _targetWindow = IntPtr.Zero;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _preview.Close();
        _restoreBounds.Clear();
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

    private sealed class SnapPreviewWindow : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExNoActivate = 0x08000000;
        private static readonly IntPtr HwndTopmost = new(-1);
        private const uint PreviewFlags = 0x0010 | 0x0040;

        private IntPtr _handle;
        public NativeRect? CurrentTarget { get; private set; }

        public SnapPreviewWindow()
        {
            Width = 240;
            Height = 160;
            Left = -32000;
            Top = -32000;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            IsHitTestVisible = false;
            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(76, 116, 178, 206)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(210, 186, 226, 240)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(2)
            };
            SourceInitialized += (_, _) =>
            {
                _handle = new WindowInteropHelper(this).Handle;
                var style = GetPreviewWindowLong(_handle, GwlExStyle);
                SetPreviewWindowLong(_handle, GwlExStyle,
                    style | WsExTransparent | WsExToolWindow | WsExNoActivate);
            };
        }

        public void ShowPreview(NativeRect bounds)
        {
            CurrentTarget = bounds;
            if (!IsVisible)
                Show();

            if (_handle == IntPtr.Zero)
                _handle = new WindowInteropHelper(this).Handle;

            SetPreviewWindowPos(
                _handle, HwndTopmost,
                bounds.Left, bounds.Top,
                bounds.Right - bounds.Left,
                bounds.Bottom - bounds.Top,
                PreviewFlags);
        }

        public void HidePreview()
        {
            CurrentTarget = null;
            if (IsVisible)
                Hide();
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetPreviewWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetPreviewWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetPreviewWindowPos(
            IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    }
}