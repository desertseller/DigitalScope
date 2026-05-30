using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DigitalScope.Core;
using DigitalScope.Crosshair;
using DigitalScope.Magnifier;

namespace DigitalScope.Views;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly ConfigManager    _cfgManager;
    private readonly HotkeyManager    _hotkeys;
    private readonly MagnifierWindow  _magnifier;
    private readonly CrosshairWindow  _crosshair;

    private int  _hkToggleId    = -1;
    private int  _hkCrosshairId = -1;
    private bool _exitRequested;

    public MainWindow()
    {
        _cfgManager = new ConfigManager();
        _cfgManager.Load();

        _hotkeys   = new HotkeyManager();
        _magnifier = new MagnifierWindow(_cfgManager.Config);
        _crosshair = new CrosshairWindow(_cfgManager.Config);

        InitializeComponent();

        TabGeneral.Initialise   (_cfgManager.Config, _cfgManager);
        TabCrosshair.Initialise (_cfgManager.Config, _cfgManager);
        TabHotkeys.Initialise   (_cfgManager.Config, _cfgManager);

        TabGeneral.ConfigChanged    += OnConfigChanged;
        TabCrosshair.ConfigChanged  += OnCrosshairTabChanged;
        TabHotkeys.HotkeysChanged   += RegisterHotkeys;

        TbVersion.Text = AppSettings.AppVersion;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnableDarkTitleBar();
        _hotkeys.Attach(this);
        RegisterHotkeys();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _hotkeys.Dispose();
        _magnifier.Close();
        _crosshair.Close();
        AppLogger.Info("MainWindow closed.");
        AppLogger.Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            Hide();
            AppLogger.Info("Window hidden to tray.");
            return;
        }
        base.OnClosing(e);
    }

    public void RequestExit()
    {
        _exitRequested = true;
        Close();
    }

    public void ToggleCrosshair()
    {
        _crosshair.Toggle();
        _cfgManager.Config.OverlayEnabled = _crosshair.IsActive;
        _cfgManager.Save();
        TabCrosshair.Refresh();
    }

    private void EnableDarkTitleBar()
    {
        try
        {
            var hwnd  = new WindowInteropHelper(this).Handle;
            int value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Could not set dark title bar: {ex.Message}");
        }
    }

    private void RegisterHotkeys()
    {
        if (_hkToggleId    >= 0) _hotkeys.Unregister(_hkToggleId);
        if (_hkCrosshairId >= 0) _hotkeys.Unregister(_hkCrosshairId);

        _hkToggleId = _hotkeys.Register(
            _cfgManager.Config.HotkeyToggle,
            () =>
            {
                if (!TabHotkeys.IsAnyHotkeyFocused)
                    Dispatcher.Invoke(() => _magnifier.Toggle());
            });

        _hkCrosshairId = _hotkeys.Register(
            _cfgManager.Config.HotkeyCrosshair,
            () =>
            {
                if (!TabHotkeys.IsAnyHotkeyFocused)
                    Dispatcher.Invoke(() => ToggleCrosshair());
            });

        AppLogger.Info($"Hotkeys: toggle={_cfgManager.Config.HotkeyToggle}, crosshair={_cfgManager.Config.HotkeyCrosshair}");
    }

    private void OnConfigChanged()
    {
        _magnifier.UpdateConfig(_cfgManager.Config);
        _crosshair.UpdateConfig(_cfgManager.Config);
    }

    private void OnCrosshairTabChanged()
    {
        _crosshair.UpdateConfig(_cfgManager.Config);
        if (TabCrosshair.IsOverlayEnabled && !_crosshair.IsActive)
            _crosshair.Activate();
        else if (!TabCrosshair.IsOverlayEnabled && _crosshair.IsActive)
            _crosshair.Deactivate();
    }

    private void SubTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb) return;
        if (TabGeneral is null || TabHotkeys is null) return;

        TabGeneral.Visibility    = Visibility.Collapsed;
        TabCrosshair.Visibility  = Visibility.Collapsed;
        TabHotkeys.Visibility    = Visibility.Collapsed;

        UIElement? target = rb.Tag?.ToString() switch
        {
            "Magnifier"  => TabGeneral,
            "Crosshair"  => TabCrosshair,
            "Hotkeys"    => TabHotkeys,
            _ => null,
        };

        if (target is not null)
        {
            target.Visibility = Visibility.Visible;
            AnimateTab(target);
        }
    }

    private static void AnimateTab(UIElement tab)
    {
        tab.RenderTransform = null;

        var fadeIn = new DoubleAnimation
        {
            From     = 0.0,
            To       = 1.0,
             Duration = new Duration(TimeSpan.FromMilliseconds(160)),
        };
        tab.BeginAnimation(UIElement.OpacityProperty,       fadeIn);
    }
}
