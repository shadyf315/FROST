using System.Windows;

namespace FROST;

public partial class MainWindow : Window
{
    private bool _snowEnabled;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SetPage(string title, string subtitle, string eyebrow)
    {
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        PageEyebrow.Text = eyebrow;
    }

    private void Overview_Click(object sender, RoutedEventArgs e) =>
        SetPage("Good evening.", "A calmer, more considered desktop starts here.", "YOUR WINDOWS, REIMAGINED");

    private void Snow_Click(object sender, RoutedEventArgs e) =>
        SetPage("Snowfall", "A dedicated space for the winter atmosphere engine.", "FROST / SNOW");

    private void MissionControl_Click(object sender, RoutedEventArgs e) =>
        SetPage("Mission Control", "Your open windows, brought together in one overview.", "FROST / WINDOW MANAGER");

    private void Dock_Click(object sender, RoutedEventArgs e) =>
        SetPage("Dock", "A familiar launch space for the apps you use every day.", "FROST / DOCK");

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        SetPage("Settings", "Make FROST feel right on your Windows desktop.", "FROST / PREFERENCES");

    private void ToggleSnow_Click(object sender, RoutedEventArgs e)
    {
        _snowEnabled = !_snowEnabled;
        SnowToggle.Content = _snowEnabled ? "Disable snowfall" : "Enable snowfall";
        MessageBox.Show(
            _snowEnabled
                ? "Snowfall controls are ready for the next development step."
                : "Snowfall preview has been switched off.",
            "FROST",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
