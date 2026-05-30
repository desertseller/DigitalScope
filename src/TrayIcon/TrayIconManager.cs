using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using DigitalScope.Core;

namespace DigitalScope.TrayIcon;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon       _appIcon;

    public event Action? ShowRequested;
    public event Action? CrosshairToggleRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        _appIcon = LoadAppIcon();

        _icon = new NotifyIcon
        {
            Text    = AppSettings.AppName,
            Icon    = _appIcon,
            Visible = true,
        };

        _icon.MouseClick += OnIconMouseClick;
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    private void OnIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var menu = new TrayContextMenu();
            menu.ShowRequested             += () => ShowRequested?.Invoke();
            menu.CrosshairToggleRequested  += () => CrosshairToggleRequested?.Invoke();
            menu.ExitRequested             += () => ExitRequested?.Invoke();
            menu.ShowAtCursor();
        });
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _appIcon.Dispose();
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                using var extracted = Icon.ExtractAssociatedIcon(processPath);
                if (extracted is not null)
                    return (Icon)extracted.Clone();
            }
        }
        catch (Exception ex) { AppLogger.Warn($"LoadAppIcon failed: {ex.Message}"); }

        return (Icon)SystemIcons.Application.Clone();
    }
}
