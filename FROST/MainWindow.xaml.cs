using System.Windows;
using FROST.Core;
using FROST.Views;

namespace FROST;

public partial class MainWindow : Window
{
    private readonly SnowEngine _snowEngine = new();

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _snowEngine.Dispose();
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

    private void MissionControl_Click(object sender, RoutedEventArgs e) =>
        SetPage("Mission Control", "Your open windows, brought together in one overview.", "FROST / WINDOW MANAGER");

    private void Dock_Click(object sender, RoutedEventArgs e) =>
        SetPage("Dock", "A familiar launch space for the apps you use every day.", "FROST / DOCK");

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        SetPage("Settings", "Make FROST feel right on your Windows desktop.", "FROST / PREFERENCES");

    private void ToggleSnow_Click(object sender, RoutedEventArgs e)
    {
        _snowEngine.Toggle();
        UpdateSnowButton();
    }

    private void UpdateSnowButton()
    {
        SnowToggle.Content = _snowEngine.IsEnabled ? "Disable snowfall" : "Enable snowfall";
    }
}
