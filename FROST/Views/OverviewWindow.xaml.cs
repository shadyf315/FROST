using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FROST.Services;

namespace FROST.Views;

public partial class OverviewWindow : Window
{
    private readonly ObservableCollection<WindowInfo> _windows = [];

    public OverviewWindow()
    {
        InitializeComponent();
        WindowList.ItemsSource = _windows;
        RefreshWindows();
    }

    private void RefreshWindows()
    {
        WindowList.SelectedItem = null;
        _windows.Clear();

        foreach (var window in WindowService.GetOpenWindows())
            _windows.Add(window);

        WindowCount.Text = $"{_windows.Count} OPEN WINDOW{(_windows.Count == 1 ? "" : "S")}";
        LastUpdated.Text = $"Updated {DateTime.Now:HH:mm:ss}";
        EmptyState.Visibility = _windows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshWindows();

    private void WindowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WindowList.SelectedItem is not WindowInfo selected)
            return;

        var activated = WindowService.Activate(selected);
        if (activated)
            Close();
        else
        {
            MessageBox.Show(
                "Windows did not allow FROST to activate this window. It may have closed or changed desktops.",
                "FROST",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            RefreshWindows();
        }
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
