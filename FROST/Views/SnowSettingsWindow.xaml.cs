using System.Windows;
using FROST.Core;

namespace FROST.Views;

public partial class SnowSettingsWindow : Window
{
    private readonly SnowEngine _engine;
    private bool _updating;

    public SnowSettingsWindow(SnowEngine engine)
    {
        _engine = engine;
        InitializeComponent();
        GlassEngine.ApplyTo(this);

        _updating = true;
        DensitySlider.Value = engine.Density;
        SpeedSlider.Value = engine.Speed;
        _updating = false;

        RefreshState();
    }

    private void Settings_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || DensityValue is null || SpeedValue is null) return;

        var density = (int)Math.Round(DensitySlider.Value);
        var speed = Math.Round(SpeedSlider.Value, 1);
        DensityValue.Text = $"{density} flakes";
        SpeedValue.Text = $"{speed:0.0}×";
        _engine.Configure(density, speed);
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        _engine.Toggle();
        RefreshState();
    }

    private void RefreshState()
    {
        var enabled = _engine.IsEnabled;
        ToggleButton.Content = enabled ? "Disable snowfall" : "Enable snowfall";
        StatusText.Text = enabled
            ? "Snowfall is active across your virtual desktop."
            : "Snowfall is currently disabled.";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
