using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DigitalScope.Core;

namespace DigitalScope.Tabs;

public partial class GeneralTab : UserControl
{
    private AppConfig?     _config;
    private ConfigManager? _manager;
    private bool           _loading;

    public event Action? ConfigChanged;

    public GeneralTab()
    {
        InitializeComponent();
        Picker.ColorPicked += OnColorPicked;
    }

    public void Initialise(AppConfig config, ConfigManager manager)
    {
        _config  = config;
        _manager = manager;
        LoadValues();
    }

    public void Refresh()
    {
        if (_config is not null) LoadValues();
    }

    private void LoadValues()
    {
        _loading = true;

        SetSlider(SlWidth,  LblWidth,  _config!.MagnifierWidth);
        SetSlider(SlHeight, LblHeight, _config!.MagnifierHeight);

        SlZoom.Value  = (int)(_config!.ZoomFactor * 10);
        LblZoom.Text  = $"{_config!.ZoomFactor:F1}x";

        ChkCrosshair.IsChecked = _config.ShowCrosshair;
        SetSwatch(PrvCrosshair, _config.CrosshairColor);

        _loading = false;
    }

    private static void SetSlider(System.Windows.Controls.Slider sl,
                                  TextBlock lbl, double value)
    {
        sl.Value = Math.Clamp(value, sl.Minimum, sl.Maximum);
        lbl.Text = ((int)sl.Value).ToString();
    }

    private void SlWidth_ValueChanged(object s, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblWidth != null) LblWidth.Text = ((int)e.NewValue).ToString();
        if (_loading || _config is null) return;
        _config.MagnifierWidth = (int)SlWidth.Value;
        Save();
    }

    private void SlHeight_ValueChanged(object s, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblHeight != null) LblHeight.Text = ((int)e.NewValue).ToString();
        if (_loading || _config is null) return;
        _config.MagnifierHeight = (int)SlHeight.Value;
        Save();
    }

    private void SlZoom_ValueChanged(object s, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        double zoom = e.NewValue / 10.0;
        if (LblZoom != null) LblZoom.Text = $"{zoom:F1}x";
        if (_loading || _config is null) return;
        _config.ZoomFactor = zoom;
        Save();
    }

    private void Save()
    {
        _manager?.Save();
        ConfigChanged?.Invoke();
    }

    private void ChkCrosshair_Changed(object s, RoutedEventArgs e)
    {
        if (_loading || _config is null) return;
        _config.ShowCrosshair = ChkCrosshair.IsChecked == true;
        Save();
    }

    private void PrvCrosshair_Click(object s, MouseButtonEventArgs e)
    {
        if (_config is null) return;
        if (ColorPickerPopup.IsOpen)
        {
            ColorPickerPopup.IsOpen = false;
            return;
        }
        Picker.SetHex(_config.CrosshairColor);
        ColorPickerPopup.PlacementTarget = (UIElement)s;
        ColorPickerPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        ColorPickerPopup.IsOpen = true;
    }

    private void OnColorPicked(string hex)
    {
        ColorPickerPopup.IsOpen = false;
        if (_config is null) return;
        _config.CrosshairColor = hex;
        SetSwatch(PrvCrosshair, hex);
        Save();
    }

    private void Tab_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ColorPickerPopup.IsOpen) return;
        var child = ColorPickerPopup.Child as UIElement;
        if (child != null && child.IsMouseOver) return;
        ColorPickerPopup.IsOpen = false;
    }

    private static void SetSwatch(Border swatch, string hex)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            swatch.Background = new SolidColorBrush(c);
        }
        catch (Exception ex) { AppLogger.Warn($"SetSwatch failed for '{hex}': {ex.Message}"); }
    }
}
