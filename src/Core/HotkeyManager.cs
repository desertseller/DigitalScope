using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DigitalScope.Core;

public sealed class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const uint MOD_ALT      = 0x0001;
    private const uint MOD_CONTROL  = 0x0002;
    private const uint MOD_SHIFT    = 0x0004;
    private const uint MOD_WIN      = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int WM_HOTKEY = 0x0312;
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    #pragma warning disable CS0649
    private readonly struct MouseHookStruct
    {
        public readonly int X;
        public readonly int Y;
        public readonly IntPtr MouseData;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly IntPtr ExtraInfo;
    }
    #pragma warning restore CS0649

    private IntPtr      _hwnd;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _callbacks = new();
    private readonly Dictionary<int, string> _mouseHotkeys = new();
    private int _nextId = 9000;
    private IntPtr _mouseHook = IntPtr.Zero;
    private HookProc? _mouseHookProc;

    public void Attach(Window window)
    {
        _hwnd   = new WindowInteropHelper(window).EnsureHandle();
        _source?.RemoveHook(WndProc);
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);
        AppLogger.Info("HotkeyManager attached to window handle.");
    }

    public int Register(string hotkey, Action callback)
    {
        if (IsMouseButton(hotkey, out int mouseButton))
        {
            int id = _nextId++;
            _mouseHotkeys[id] = hotkey;
            _callbacks[id] = callback;
            AppLogger.Info($"Mouse hotkey registered: '{hotkey}' → id={id}");
            EnsureMouseHookInstalled();
            return id;
        }

        if (!ParseHotkey(hotkey, out uint mods, out uint vk))
        {
            AppLogger.Warn($"Cannot parse hotkey: '{hotkey}'");
            return -1;
        }

        int id2 = _nextId++;
        if (!RegisterHotKey(_hwnd, id2, mods | MOD_NOREPEAT, vk))
        {
            AppLogger.Warn($"RegisterHotKey failed for '{hotkey}' (id={id2})");
            return -1;
        }

        _callbacks[id2] = callback;
        AppLogger.Info($"Hotkey registered: '{hotkey}' → id={id2}");
        return id2;
    }

    public void Unregister(int id)
    {
        _callbacks.Remove(id);
        if (_mouseHotkeys.Remove(id))
        {
            if (_mouseHotkeys.Count == 0)
                UninstallMouseHook();
        }
        else
        {
            UnregisterHotKey(_hwnd, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys.ToList())
        {
            if (!_mouseHotkeys.ContainsKey(id))
                UnregisterHotKey(_hwnd, id);
        }
        _callbacks.Clear();
        _mouseHotkeys.Clear();
        UninstallMouseHook();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var cb))
            {
                cb.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private static bool ParseHotkey(string hotkey, out uint mods, out uint vk)
    {
        mods = 0;
        vk   = 0;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        string? keyPart = null;

        foreach (var p in parts)
        {
            switch (p.Trim().ToLowerInvariant())
            {
                case "ctrl":  case "control": mods |= MOD_CONTROL; break;
                case "shift":                 mods |= MOD_SHIFT;   break;
                case "alt":                   mods |= MOD_ALT;     break;
                case "win":                   mods |= MOD_WIN;     break;
                default:      keyPart = p.Trim(); break;
            }
        }

        if (keyPart is null) return false;

        if (keyPart.Length == 1)
        {
            vk = (uint)char.ToUpperInvariant(keyPart[0]);
            return true;
        }

        vk = keyPart.ToUpperInvariant() switch
        {
            "F1"  => 0x70, "F2"  => 0x71, "F3"  => 0x72, "F4"  => 0x73,
            "F5"  => 0x74, "F6"  => 0x75, "F7"  => 0x76, "F8"  => 0x77,
            "F9"  => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            "TAB" => 0x09, "SPACE" => 0x20, "RETURN" => 0x0D,
            "INSERT" => 0x2D, "DELETE" => 0x2E,
            "HOME" => 0x24, "END" => 0x23,
            "PAGEUP" => 0x21, "PAGEDOWN" => 0x22,
            _ => 0
        };

        return vk != 0;
    }

    private static bool IsMouseButton(string hotkey, out int mouseButton)
    {
        mouseButton = 0;
        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        string? keyPart = null;

        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (trimmed.Equals("ctrl", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("control", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("shift", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("alt", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("win", StringComparison.OrdinalIgnoreCase))
                continue;
            keyPart = trimmed;
        }

        if (keyPart is null) return false;

        mouseButton = keyPart.ToUpperInvariant() switch
        {
            "LBUTTON" or "LEFTBUTTON" or "MOUSE1" => 1,
            "RBUTTON" or "RIGHTBUTTON" or "MOUSE2" => 2,
            "MBUTTON" or "MIDDLEBUTTON" or "MOUSE3" => 3,
            "XBUTTON1" or "MOUSE4" => 4,
            "XBUTTON2" or "MOUSE5" => 5,
            _ => 0
        };

        return mouseButton != 0;
    }

    private void EnsureMouseHookInstalled()
    {
        if (_mouseHook != IntPtr.Zero) return;

        _mouseHookProc = MouseHookCallback;
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, IntPtr.Zero, 0);

        if (_mouseHook == IntPtr.Zero)
        {
            AppLogger.Warn("Failed to install global mouse hook.");
            return;
        }

        AppLogger.Info("Global mouse hook installed.");
    }

    private void UninstallMouseHook()
    {
        if (_mouseHook == IntPtr.Zero) return;

        UnhookWindowsHookEx(_mouseHook);
        _mouseHook = IntPtr.Zero;
        _mouseHookProc = null;
        AppLogger.Info("Global mouse hook uninstalled.");
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);

        int mouseButton = wParam switch
        {
            WM_LBUTTONDOWN => 1,
            WM_RBUTTONDOWN => 2,
            WM_MBUTTONDOWN => 3,
            WM_XBUTTONDOWN => ExtractXButton(lParam),
            _ => 0
        };

        if (mouseButton != 0)
        {
            var matchingIds = _mouseHotkeys.Where(kvp => IsMouseButtonMatch(kvp.Value, mouseButton)).ToList();
            foreach (var kvp in matchingIds)
            {
                if (_callbacks.TryGetValue(kvp.Key, out var cb))
                {
                    cb.Invoke();
                }
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private static int ExtractXButton(IntPtr lParam)
    {
        try
        {
            var hookStruct = Marshal.PtrToStructure<MouseHookStruct>(lParam);
            int xButton = (int)((long)hookStruct.MouseData >> 16) & 0xFFFF;
            return xButton switch
            {
                1 => 4, // XBUTTON1
                2 => 5, // XBUTTON2
                _ => 0
            };
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"ExtractXButton failed: {ex.Message}");
            return 0;
        }
    }

    private static bool IsMouseButtonMatch(string hotkey, int pressedButton)
    {
        return IsMouseButton(hotkey, out int registeredButton) && registeredButton == pressedButton;
    }

    public void Dispose()
    {
        UninstallMouseHook();
        UnregisterAll();
        _source?.RemoveHook(WndProc);
        _source?.Dispose();
    }
}
