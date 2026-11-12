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

    private void Launch(string target, string appName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show(
                $"Не удалось запустить приложение: {appName}.",
                "Панель запуска FROST",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void Explorer_Click(object sender, RoutedEventArgs e) => Launch("explorer.exe", "Проводник");
    private void Notepad_Click(object sender, RoutedEventArgs e) => Launch("notepad.exe", "Блокнот");
    private void Calculator_Click(object sender, RoutedEventArgs e) => Launch("calc.exe", "Калькулятор");
    private void Settings_Click(object sender, RoutedEventArgs e) => Launch("ms-settings:", "Параметры Windows");
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}