using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DigitalScope.Core;

namespace DigitalScope.Tabs;

public partial class HotkeysTab : UserControl
{
    private AppConfig     _config  = null!;
    private ConfigManager _manager = null!;

    private string _prevToggle    = "";
    private string _prevCrosshair = "";

    public event Action? HotkeysChanged;

    public bool IsAnyHotkeyFocused =>
        TbToggleHotkey.IsKeyboardFocused || TbCrosshairHotkey.IsKeyboardFocused;

    public HotkeysTab()
    {
        InitializeComponent();
        PreviewMouseDown += HotkeysTab_PreviewMouseDown;
    }

    private void HotkeysTab_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!IsAnyHotkeyFocused) return;

        e.Handled = true;
        var focusedElement = Keyboard.FocusedElement;
        if (focusedElement is not TextBox focusedTextBox) return;

        string? buttonName = e.ChangedButton switch
        {
            System.Windows.Input.MouseButton.Left => "LButton",
            System.Windows.Input.MouseButton.Right => "RButton",
            System.Windows.Input.MouseButton.Middle => "MButton",
            System.Windows.Input.MouseButton.XButton1 => "XButton1",
            System.Windows.Input.MouseButton.XButton2 => "XButton2",
            _ => null
        };

        if (buttonName is null) return;

        var mods = new List<string>();
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) mods.Add("Ctrl");
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods.Add("Shift");
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) mods.Add("Alt");

        mods.Add(buttonName);
        focusedTextBox.Text = string.Join("+", mods);

        if      (focusedTextBox == TbToggleHotkey)    SaveToggleHotkey(focusedTextBox.Text);
        else if (focusedTextBox == TbCrosshairHotkey) SaveCrosshairHotkey(focusedTextBox.Text);
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
        TbToggleHotkey.Text    = _config.HotkeyToggle;
        TbCrosshairHotkey.Text = _config.HotkeyCrosshair;
        LblStatus.Text         = string.Empty;
    }

    public void TbHotkey_GotFocus(object s, RoutedEventArgs e)
    {
        if (s is TextBox tb)
        {
            tb.SelectAll();
            if (tb == TbToggleHotkey)    _prevToggle    = tb.Text;
        if (tb == TbCrosshairHotkey) _prevCrosshair = tb.Text;
        }
        LblStatus.Text = "Press a key combination or mouse button (e.g. Ctrl+M or LButton). Press ESC to cancel.";
    }

    private void TbToggleHotkey_KeyDown(object s, KeyEventArgs e)
    {
        if (IsEscape(e)) { CancelEdit(TbToggleHotkey, _prevToggle, e); return; }
        if (CaptureHotkey(TbToggleHotkey, e))
            SaveToggleHotkey(TbToggleHotkey.Text);
    }

    private void TbCrosshairHotkey_KeyDown(object s, KeyEventArgs e)
    {
        if (IsEscape(e)) { CancelEdit(TbCrosshairHotkey, _prevCrosshair, e); return; }
        if (CaptureHotkey(TbCrosshairHotkey, e))
            SaveCrosshairHotkey(TbCrosshairHotkey.Text);
    }

    private static bool IsEscape(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        return key == Key.Escape;
    }

    private void CancelEdit(TextBox tb, string original, KeyEventArgs e)
    {
        e.Handled = true;
        tb.Text   = original;
        LblStatus.Text = string.Empty;
        Keyboard.ClearFocus();
    }

    private static bool CaptureHotkey(TextBox tb, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or
                   Key.LeftShift or Key.RightShift or
                   Key.LeftAlt or Key.RightAlt or
                   Key.LWin or Key.RWin or Key.Escape)
            return false;

        var mods = new List<string>();
        if (Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl))  mods.Add("Ctrl");
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods.Add("Shift");
        if (Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt))   mods.Add("Alt");
        mods.Add(key.ToString());
        tb.Text = string.Join("+", mods);
        return true;
    }

    private bool HasConflict(string candidate, string skip)
    {
        var all = new[] { _config.HotkeyToggle, _config.HotkeyCrosshair };
        foreach (var h in all)
        {
            if (h.Equals(skip, StringComparison.OrdinalIgnoreCase)) continue;
            if (h.Equals(candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void SaveToggleHotkey(string newHotkey)
    {
        newHotkey = newHotkey.Trim();
        if (string.IsNullOrEmpty(newHotkey)) return;

        if (HasConflict(newHotkey, _config.HotkeyToggle))
        {
            LblStatus.Text        = "⚠  Conflict: this hotkey is already used by another action.";
            TbToggleHotkey.Text   = _prevToggle;
            return;
        }

        _config.HotkeyToggle = newHotkey;
        _manager.Save();
        HotkeysChanged?.Invoke();
        LblStatus.Text = string.Empty;
        AppLogger.Info($"Toggle hotkey updated: {newHotkey}");
        Keyboard.ClearFocus();
    }

    private void SaveCrosshairHotkey(string newHotkey)
    {
        newHotkey = newHotkey.Trim();
        if (string.IsNullOrEmpty(newHotkey)) return;

        if (HasConflict(newHotkey, _config.HotkeyCrosshair))
        {
            LblStatus.Text          = "⚠  Conflict: this hotkey is already used by another action.";
            TbCrosshairHotkey.Text  = _prevCrosshair;
            return;
        }

        _config.HotkeyCrosshair = newHotkey;
        _manager.Save();
        HotkeysChanged?.Invoke();
        LblStatus.Text = string.Empty;
        AppLogger.Info($"Crosshair hotkey updated: {newHotkey}");
        Keyboard.ClearFocus();
    }
}
