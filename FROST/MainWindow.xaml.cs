using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FROST.Core;
using FROST.Services;
using FROST.Views;

namespace FROST;

public partial class MainWindow : Window
{
    private readonly SnowEngine _snowEngine = new();
    private readonly AppSettings _settings = SettingsService.Load();
    private readonly WindowSnapService _windowSnap = new();
    private readonly DispatcherTimer _statusTimer = new();
    private HotkeyManager? _hotkeys;

    public MainWindow()
    {
        InitializeComponent();
        GlassEngine.ApplyTo(this);

        VersionLabel.Text = $"Релиз № {VersionInfo.Current}";

        Loaded += async (_, _) =>
        {
            var background = await BackgroundImageService.LoadAsync();
            if (background is not null && IsLoaded)
                BackgroundPhoto.Source = background;
        };

        _snowEngine.Configure(_settings.SnowDensity, _settings.SnowSpeed);
        _snowEngine.StateChanged += SnowEngine_StateChanged;
        if (_settings.SnowEnabled)
            _snowEngine.Enable();

        _windowSnap.Start();

        _statusTimer.Interval = TimeSpan.FromSeconds(1);
        _statusTimer.Tick += (_, _) => UpdateSystemStatus();
        _statusTimer.Start();
        NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
        UpdateSystemStatus();

        SourceInitialized += (_, _) =>
        {
            _hotkeys = new HotkeyManager(
                new WindowInteropHelper(this).Handle,
                ToggleSnow);
        };

        Closed += (_, _) =>
        {
            _settings.SnowEnabled = _snowEngine.IsEnabled;
            SettingsService.Save(_settings);
            _snowEngine.StateChanged -= SnowEngine_StateChanged;
            _hotkeys?.Dispose();
            _snowEngine.Dispose();
            _windowSnap.Dispose();
            _statusTimer.Stop();
            NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChanged;
        };

        UpdateSnowButton();
    }

    private void NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        Dispatcher.BeginInvoke(UpdateSystemStatus);
    }

    private void UpdateSystemStatus()
    {
        var now = DateTime.Now;
        var russian = CultureInfo.GetCultureInfo("ru-RU");
        TopDate.Text = now.ToString("dddd, d MMMM", russian);
        TopClock.Text = now.ToString("HH:mm");

        var connected = NetworkInterface.GetIsNetworkAvailable();
        NetworkStatus.Text = connected ? "Сеть подключена" : "Нет сети";
        NetworkDot.Fill = connected
            ? new SolidColorBrush(Color.FromRgb(147, 183, 141))
            : new SolidColorBrush(Color.FromRgb(199, 139, 135));
    }

    private void SnowEngine_StateChanged(object? sender, EventArgs e)
    {
        _settings.SnowDensity = _snowEngine.Density;
        _settings.SnowSpeed = _snowEngine.Speed;
        _settings.SnowEnabled = _snowEngine.IsEnabled;
        SettingsService.Save(_settings);
        UpdateSnowButton();
    }

    private void SetPage(string title, string subtitle, string eyebrow)
    {
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        PageEyebrow.Text = eyebrow;
    }

    private void Overview_Click(object sender, RoutedEventArgs e) =>
        SetPage("Главная", "Настройте зимнее оформление рабочего стола.", "FROST / ГЛАВНАЯ");

    private void Snow_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Снег", "Настройте количество снежинок и скорость их падения.", "FROST / СНЕГ");
        var window = new SnowSettingsWindow(_snowEngine) { Owner = this };
        window.ShowDialog();
        UpdateSnowButton();
    }

    private void ToggleSnow_Click(object sender, RoutedEventArgs e) => ToggleSnow();

    private void ToggleSnow()
    {
        _snowEngine.Toggle();
        UpdateSnowButton();
    }

    private void UpdateSnowButton()
    {
        SnowToggle.Content = _snowEngine.IsEnabled ? "Выключить" : "Включить";
    }

    private static void Launch(string target, string name)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show(
                $"Не удалось запустить приложение «{name}».",
                "FROST",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void Explorer_Click(object sender, RoutedEventArgs e) => Launch("explorer.exe", "Проводник");
    private void Browser_Click(object sender, RoutedEventArgs e) => Launch("https://www.google.com", "Браузер");
    private void Notepad_Click(object sender, RoutedEventArgs e) => Launch("notepad.exe", "Блокнот");
    private void Calculator_Click(object sender, RoutedEventArgs e) => Launch("calc.exe", "Калькулятор");
}