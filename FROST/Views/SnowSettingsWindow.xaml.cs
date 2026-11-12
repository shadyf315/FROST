using System.Globalization;
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
        UpdateValues();
    }

    private void Settings_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || DensityValue is null || SpeedValue is null) return;

        var density = (int)Math.Round(DensitySlider.Value);
        var speed = Math.Round(SpeedSlider.Value, 1);
        UpdateValues();
        _engine.Configure(density, speed);
    }

    private void UpdateValues()
    {
        DensityValue.Text = ((int)Math.Round(DensitySlider.Value)).ToString();
        SpeedValue.Text = SpeedSlider.Value.ToString("0.0", CultureInfo.GetCultureInfo("ru-RU")) + "×";
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        _engine.Toggle();
        RefreshState();
    }

    private void RefreshState()
    {
        var enabled = _engine.IsEnabled;
        ToggleButton.Content = enabled ? "Выключить снег" : "Включить снег";
        StatusText.Text = enabled
            ? "Снег сейчас отображается поверх рабочего стола."
            : "Снег выключен.";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}