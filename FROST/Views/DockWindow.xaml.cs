using System.Diagnostics;
using System.Windows;

namespace FROST.Views;

public partial class DockWindow : Window
{
    public DockWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = SystemParameters.WorkArea.Bottom - Height - 8;
        };
    }

    private void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show(
                $"Windows could not open {target}.",
                "FROST Dock",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void Explorer_Click(object sender, RoutedEventArgs e) => Launch("explorer.exe");
    private void Notepad_Click(object sender, RoutedEventArgs e) => Launch("notepad.exe");
    private void Calculator_Click(object sender, RoutedEventArgs e) => Launch("calc.exe");
    private void Settings_Click(object sender, RoutedEventArgs e) => Launch("ms-settings:");
}
