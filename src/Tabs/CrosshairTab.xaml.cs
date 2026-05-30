using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DigitalScope.Core;
using DigitalScope.Crosshair;

namespace DigitalScope.Tabs;

public partial class CrosshairTab : UserControl
{
    private AppConfig     _config  = null!;
    private ConfigManager _manager = null!;
    private bool          _loading;

    public event Action? ConfigChanged;

    public CrosshairTab()
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
        if (_config is null) return;
        LoadValues();
    }

    private void LoadValues()
    {
        _loading = true;

        ChkEnabled.IsChecked = _config.OverlayEnabled;

        //type
        SelectComboItem(CbType, _config.OverlayCrosshairType);

        // sliders
        SlOpacity.Value   = (int)Math.Round(_config.OverlayCrosshairOpacity * 100);
        LblOpacity.Text   = $"{SlOpacity.Value}%";

        SlSize.Value      = _config.OverlayCrosshairSize;
        LblSize.Text      = $"{_config.OverlayCrosshairSize}";

        SlThickness.Value = _config.OverlayCrosshairThickness;
        LblThickness.Text = $"{_config.OverlayCrosshairThickness:F1}";

        SlGap.Value       = _config.OverlayCrosshairGap;
        LblGap.Text       = $"{_config.OverlayCrosshairGap}";

        // color swatch
        SetSwatch(PrvColor, _config.OverlayCrosshairColor);

        UpdateGapVisibility();
        RefreshPreview();

        _loading = false;
    }

    private void ChkEnabled_Changed(object s, RoutedEventArgs e)
    {
        if (_loading || _config is null) return;
        _config.OverlayEnabled = ChkEnabled.IsChecked == true;
        Save();
    }

    public bool IsOverlayEnabled => ChkEnabled.IsChecked == true;

    private void CbType_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (_loading || _config is null) return;

        string type = SelectedTag(CbType) ?? "Cross";
        _config.OverlayCrosshairType = type;
        UpdateGapVisibility();
        Save();
    }

    private void SlOpacity_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblOpacity != null) LblOpacity.Text = $"{(int)e.NewValue}%";
        if (_loading || _config is null) return;
        _config.OverlayCrosshairOpacity = e.NewValue / 100.0;
        Save();
    }

    private void SlSize_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblSize != null) LblSize.Text = $"{(int)e.NewValue}";
        if (_loading || _config is null) return;
        _config.OverlayCrosshairSize = (int)e.NewValue;
        Save();
    }

    private void SlThickness_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblThickness != null) LblThickness.Text = $"{e.NewValue:F1}";
        if (_loading || _config is null) return;
        _config.OverlayCrosshairThickness = e.NewValue;
        Save();
    }

    private void SlGap_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblGap != null) LblGap.Text = $"{(int)e.NewValue}";
        if (_loading || _config is null) return;
        _config.OverlayCrosshairGap = (int)e.NewValue;
        Save();
    }

    private void PrvColor_Click(object s, MouseButtonEventArgs e)
    {
        if (_config is null) return;

        if (ColorPickerPopup.IsOpen)
        {
            ColorPickerPopup.IsOpen = false;
            return;
        }

        Picker.SetHex(_config.OverlayCrosshairColor);
        ColorPickerPopup.PlacementTarget = (UIElement)s;
        ColorPickerPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        ColorPickerPopup.IsOpen = true;
    }

    private void OnColorPicked(string hex)
    {
        ColorPickerPopup.IsOpen = false;
        if (_config is null) return;
        _config.OverlayCrosshairColor = hex;
        SetSwatch(PrvColor, hex);
        Save();
    }

    private void Tab_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ColorPickerPopup.IsOpen) return;
        var child = ColorPickerPopup.Child as UIElement;
        if (child != null && child.IsMouseOver) return;
        ColorPickerPopup.IsOpen = false;
    }

    private void UpdateGapVisibility()
    {
        bool showGap = _config?.OverlayCrosshairType != "Dot";
        ThicknessRow.Visibility       = showGap ? Visibility.Visible   : Visibility.Collapsed;
        ThicknessSeparator.Visibility = showGap ? Visibility.Visible   : Visibility.Collapsed;
        GapRow.Visibility             = showGap ? Visibility.Visible   : Visibility.Collapsed;
        GapSeparator.Visibility       = showGap ? Visibility.Visible   : Visibility.Collapsed;
    }

    private void RefreshPreview()
    {
        if (_config is null) return;

        double side = CrosshairRenderer.CanvasSize(_config);
        const double maxPreviewSize = 110.0;
        double scale = Math.Min(maxPreviewSize / side, 1.0);

        var previewConfig = new AppConfig
        {
            OverlayCrosshairType      = _config.OverlayCrosshairType,
            OverlayCrosshairColor     = _config.OverlayCrosshairColor,
            OverlayCrosshairSize      = Math.Max((int)(_config.OverlayCrosshairSize * scale), AppSettings.MinOverlayCrosshairSize),
            OverlayCrosshairOpacity   = _config.OverlayCrosshairOpacity,
            OverlayCrosshairThickness = Math.Clamp(_config.OverlayCrosshairThickness * scale, AppSettings.MinOverlayCrosshairThickness, AppSettings.MaxOverlayCrosshairThickness),
            OverlayCrosshairGap       = (int)(_config.OverlayCrosshairGap * scale),
        };

        double previewSide = CrosshairRenderer.CanvasSize(previewConfig);
        PreviewCanvas.Width  = previewSide;
        PreviewCanvas.Height = previewSide;
        CrosshairRenderer.Draw(PreviewCanvas, previewConfig);
    }

    private void Save()
    {
        _manager.Save();
        RefreshPreview();
        ConfigChanged?.Invoke();
    }

    private static void SetSwatch(Border swatch, string hex)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            swatch.Background = new SolidColorBrush(c);
        }
        catch { }
    }

    private static void SelectComboItem(ComboBox cb, string tag)
    {
        foreach (ComboBoxItem item in cb.Items)
        {
            if (item.Tag as string == tag)
            {
                cb.SelectedItem = item;
                return;
            }
        }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        if (_config is null) return;
        _config.OverlayEnabled            = AppSettings.DefaultOverlayEnabled;
        _config.OverlayCrosshairType      = AppSettings.DefaultOverlayCrosshairType;
        _config.OverlayCrosshairColor     = AppSettings.DefaultOverlayCrosshairColor;
        _config.OverlayCrosshairSize      = AppSettings.DefaultOverlayCrosshairSize;
        _config.OverlayCrosshairOpacity   = AppSettings.DefaultOverlayCrosshairOpacity;
        _config.OverlayCrosshairGap       = AppSettings.DefaultOverlayCrosshairGap;
        _config.OverlayCrosshairThickness = AppSettings.DefaultOverlayCrosshairThickness;
        Save();
        LoadValues();
    }

    private static string? SelectedTag(ComboBox cb)
        => (cb.SelectedItem as ComboBoxItem)?.Tag as string;
}
