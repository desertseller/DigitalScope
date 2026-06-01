using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DigitalScope.Core;

namespace DigitalScope.Crosshair;

public partial class CrosshairWindow : Window
{
    [DllImport("user32.dll")] private static extern int  GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int  SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE       = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED     = 0x00080000;
    private const int WS_EX_NOACTIVATE  = 0x08000000;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;

    private AppConfig _config;
    private bool      _active;

    public new bool IsActive => _active;

    public CrosshairWindow(AppConfig config)
    {
        _config = config;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        Dispatcher.Invoke(() =>
        {
            ApplyConfig();
            if (_active) PositionWindow();
        });
    }

    public new void Activate()
    {
        _active = true;
        ApplyConfig();
        Show();
        PositionWindow();
        AppLogger.Info("Crosshair overlay activated.");
    }

    public void Deactivate()
    {
        _active = false;
        Hide();
        AppLogger.Info("Crosshair overlay deactivated.");
    }

    public void Toggle()
    {
        if (_active) Deactivate();
        else         Activate();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MakeClickThrough();
        if (!_active) Hide();
    }

    private void ApplyConfig()
    {
        double side = CrosshairRenderer.CanvasSize(_config);
        DrawCanvas.Width  = side;
        DrawCanvas.Height = side;
        Width  = side;
        Height = side;

        Opacity = Math.Clamp(_config.OverlayCrosshairOpacity, 0.1, 1.0);

        CrosshairRenderer.Draw(DrawCanvas, _config);
    }

    private void PositionWindow()
    {
        UpdateLayout();
        double sw = SystemParameters.PrimaryScreenWidth;
        double sh = SystemParameters.PrimaryScreenHeight;
        double w  = ActualWidth  > 0 ? ActualWidth  : Width;
        double h  = ActualHeight > 0 ? ActualHeight : Height;

        Left = (sw - w) / 2;
        Top  = (sh - h) / 2;
    }

    private void MakeClickThrough()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE,
                ex | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"CrosshairWindow MakeClickThrough failed: {ex.Message}");
        }
    }
}
