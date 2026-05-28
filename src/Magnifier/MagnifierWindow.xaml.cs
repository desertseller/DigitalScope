using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DigitalScope.Core;

namespace DigitalScope.Magnifier;

public partial class MagnifierWindow : Window
{
    [DllImport("user32.dll")] private static extern int  GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int  SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    [DllImport("user32.dll")] private static extern int  GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] private static extern int  ShowCursor(bool bShow);
    [DllImport("dwmapi.dll")] private static extern int  DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int  GWL_EXSTYLE              = -20;
    private const int  WS_EX_TRANSPARENT        = 0x00000020;
    private const int  WS_EX_LAYERED            = 0x00080000;
    private const int  WS_EX_NOACTIVATE         = 0x08000000;
    private const int  DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const uint WDA_EXCLUDEFROMCAPTURE   = 0x00000011;
    private const int  SM_CXSCREEN              = 0;
    private const int  SM_CYSCREEN              = 1;
    private const int  WM_NCHITTEST             = 0x0084;
    private const int  HTTRANSPARENT            = -1;

    private AppConfig       _config;
    private WriteableBitmap? _bitmap;
    private bool            _active = false;
    private bool            _renderHooked = false;
    private bool            _cursorHidden = false;

    public MagnifierWindow(AppConfig config)
    {
        _config = config;
        InitializeComponent();
        Loaded  += OnLoaded;
        Closed  += OnClosed;
        ApplyConfig();
    }

    public new bool IsActive => _active;

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        Dispatcher.Invoke(ApplyConfig);
    }

    public new void Activate()
    {
        _active = true;
        if (!_renderHooked)
        {
            CompositionTarget.Rendering += OnRendering;
            _renderHooked = true;
        }
        Show();
        PositionWindow();
        HideCursor();
        AppLogger.Info("Magnifier activated.");
    }

    public void Deactivate()
    {
        _active = false;
        Hide();
        RestoreCursor();
        AppLogger.Info("Magnifier deactivated.");
    }

    public void Toggle()
    {
        if (_active) Deactivate();
        else         Activate();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MakeClickThrough();
        Hide();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_renderHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderHooked = false;
        }
        RestoreCursor();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_active || !IsVisible || _bitmap is null) return;

        int outW = _bitmap.PixelWidth;
        int outH = _bitmap.PixelHeight;

        var ps = PresentationSource.FromVisual(this);
        double dpiX = ps?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiY = ps?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        int centerX = (int)Math.Round((Left + ActualWidth  / 2) * dpiX);
        int centerY = (int)Math.Round((Top  + ActualHeight / 2) * dpiY);

        var (srcX, srcY, srcW, srcH) =
            MagnifierEngine.ComputeSourceRect(centerX, centerY, outW, outH, _config.ZoomFactor);

        MagnifierEngine.UpdateFrame(_bitmap, srcX, srcY, srcW, srcH);
    }

    private void ApplyConfig()
    {
        int w = Math.Clamp(_config.MagnifierWidth,  100, 1920);
        int h = Math.Clamp(_config.MagnifierHeight, 100, 1080);

        if (_bitmap is null || _bitmap.PixelWidth != w || _bitmap.PixelHeight != h)
        {
            _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgr32, null);
            MagImage.Source = _bitmap;
        }

        Width  = w;
        Height = h;

        UpdateCrosshair(w, h);

        if (IsVisible) PositionWindow();

        AppLogger.Info($"Magnifier config applied: {w}×{h}, zoom={_config.ZoomFactor}x");
    }

    private void UpdateCrosshair(int w, int h)
    {
        if (_config.ShowCrosshair)
        {
            CrosshairCanvas.Visibility = Visibility.Visible;
            var color = TryParseColor(_config.CrosshairColor) ?? Colors.Red;
            var brush = new SolidColorBrush(color);

            // horizontal line
            CrosshairH.X1     = 0;
            CrosshairH.Y1     = h / 2.0;
            CrosshairH.X2     = w;
            CrosshairH.Y2     = h / 2.0;
            CrosshairH.Stroke = brush;

            // vertical line
            CrosshairV.X1     = w / 2.0;
            CrosshairV.Y1     = 0;
            CrosshairV.X2     = w / 2.0;
            CrosshairV.Y2     = h;
            CrosshairV.Stroke = brush;
        }
        else
        {
            CrosshairCanvas.Visibility = Visibility.Collapsed;
        }
    }

    private void PositionWindow()
    {
        UpdateLayout();
        var screen = SystemParameters.WorkArea;
        double w   = ActualWidth  > 0 ? ActualWidth  : Width;
        double h   = ActualHeight > 0 ? ActualHeight : Height;

        Left = screen.Left + (screen.Width  - w) / 2;
        Top  = screen.Top  + (screen.Height - h) / 2;
    }

    private void HideCursor()
    {
        if (_cursorHidden) return;
        ShowCursor(false);
        _cursorHidden = true;
    }

    private void RestoreCursor()
    {
        if (!_cursorHidden) return;
        ShowCursor(true);
        _cursorHidden = false;
    }

    private void MakeClickThrough()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE,
                ex | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE);
            SetWindowDisplayAffinity(handle, WDA_EXCLUDEFROMCAPTURE);
            var source = HwndSource.FromHwnd(handle);
            source?.AddHook(ClickThroughWndProc);
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"MakeClickThrough failed: {ex.Message}");
        }
    }

    private static IntPtr ClickThroughWndProc(
        IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            handled = true;
            return new IntPtr(HTTRANSPARENT);
        }
        return IntPtr.Zero;
    }

    private static Color? TryParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return null; }
    }
}
