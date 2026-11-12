using System.Diagnostics;
using System.IO;
using System.Windows;
using FROST.Core;

namespace FROST.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        GlassEngine.ApplyTo(this);
        SystemVersion.Text = "Система: " + Environment.OSVersion.VersionString;
        AppVersionText.Text = $"Релиз № {VersionInfo.Current}";
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FROST");

        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show("Не удалось открыть папку настроек FROST.", "FROST",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}