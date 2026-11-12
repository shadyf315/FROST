using System.Windows;
using System.Windows.Interop;
using FROST.Core;
using FROST.Views;

namespace FROST;

public partial class MainWindow : Window
{
    private readonly SnowEngine _snowEngine = new();
    private HotkeyManager? _hotkeys;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _hotkeys = new HotkeyManager(
                new WindowInteropHelper(this).Handle,
                ShowMissionControl,
                ToggleSnow);
        };
        Closed += (_, _) =>
        {
            _hotkeys?.Dispose();
            _snowEngine.Dispose();
        };
        UpdateSnowButton();
    }

    private void SetPage(string title, string subtitle, string eyebrow)
    {
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        PageEyebrow.Text = eyebrow;
    }

    private void Overview_Click(object sender, RoutedEventArgs e) =>
        SetPage("Good evening.", "A calmer, more considered desktop starts here.", "YOUR WINDOWS, REIMAGINED");

    private void Snow_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Snowfall", "Tune the atmosphere and bring a little winter to your desktop.", "FROST / SNOW");
        var window = new SnowSettingsWindow(_snowEngine) { Owner = this };
        window.ShowDialog();
        UpdateSnowButton();
    }

    private void MissionControl_Click(object sender, RoutedEventArgs e)
    {
        SetPage("Mission Control", "Your open windows, brought together in one overview.", "FROST / WINDOW MANAGER");
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

    private void Dock_Click(object sender, RoutedEventArgs e) =>
        SetPage("Dock", "A familiar launch space for the apps you use every day.", "FROST / DOCK");

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        SetPage("Settings", "Make FROST feel right on your Windows desktop.", "FROST / PREFERENCES");

    private void ToggleSnow_Click(object sender, RoutedEventArgs e) => ToggleSnow();

    private void ToggleSnow()
    {
        _snowEngine.Toggle();
        UpdateSnowButton();
    }

    private void UpdateSnowButton()
    {
        SnowToggle.Content = _snowEngine.IsEnabled ? "Disable snowfall" : "Enable snowfall";
    }
}
