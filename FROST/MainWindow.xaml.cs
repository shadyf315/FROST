using System.Windows;
using System.Windows.Interop;
using FROST.Core;
using FROST.Services;
using FROST.Views;

namespace FROST;

public partial class MainWindow : Window
{
    private readonly SnowEngine _snowEngine = new();
    private readonly AppSettings _settings = SettingsService.Load();
    private HotkeyManager? _hotkeys;
    private DockWindow? _dockWindow;

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

        SourceInitialized += (_, _) =>
        {
            _hotkeys = new HotkeyManager(
                new WindowInteropHelper(this).Handle,
                ShowMissionControl,
                ToggleSnow);
        };

        Closed += (_, _) =>
        {
            _settings.SnowEnabled = _snowEngine.IsEnabled;
            SettingsService.Save(_settings);
            _snowEngine.StateChanged -= SnowEngine_StateChanged;
            _hotkeys?.Dispose();
            _snowEngine.Dispose();
        };

        UpdateSnowButton();
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
        SetPage("Рабочий стол", "Управляйте эффектами и окнами Windows.", "FROST / ОБЗОР");

    private void Snow_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Снег", "Настройте количество снежинок и скорость их падения.", "FROST / ЭФФЕКТЫ");
        var window = new SnowSettingsWindow(_snowEngine) { Owner = this };
        window.ShowDialog();
        UpdateSnowButton();
    }

    private void MissionControl_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Обзор окон", "Все открытые приложения в одном списке.", "FROST / ОКНА");
        ShowMissionControl();
    }

    private void ShowMissionControl()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        var window = new OverviewWindow { Owner = this };
        window.ShowDialog();
    }

    private void Dock_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Панель запуска", "Быстрый доступ к часто используемым приложениям.", "FROST / ЗАПУСК");
        if (_dockWindow is { IsVisible: true })
        {
            _dockWindow.Activate();
            return;
        }

        _dockWindow = new DockWindow { Owner = this };
        _dockWindow.Closed += (_, _) => _dockWindow = null;
        _dockWindow.Show();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Настройки", "Сведения о приложении и локальных параметрах.", "FROST / НАСТРОЙКИ");
        var window = new SettingsWindow { Owner = this };
        window.ShowDialog();
    }

    private void ToggleSnow_Click(object sender, RoutedEventArgs e) => ToggleSnow();

    private void ToggleSnow()
    {
        _snowEngine.Toggle();
        UpdateSnowButton();
    }

    private void UpdateSnowButton()
    {
        SnowToggle.Content = _snowEngine.IsEnabled ? "Выключить снег" : "Включить снег";
    }
}