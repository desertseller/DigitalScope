using System.IO;
using System.Text.Json;

namespace DigitalScope.Core;

public class ConfigManager
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented               = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _configPath;

    public AppConfig Config { get; private set; } = new AppConfig();

    public ConfigManager()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppSettings.AppName);

        try   { Directory.CreateDirectory(dir); }
        catch { AppLogger.Warn($"Could not create config directory '{dir}', using base dir."); dir = AppDomain.CurrentDomain.BaseDirectory; }

        _configPath = Path.Combine(dir, "settings.json");
        AppLogger.Info($"Config path: {_configPath}");
    }

    public void Load()
    {
        if (!File.Exists(_configPath))
        {
            AppLogger.Info("No config file found – using defaults.");
            Config = new AppConfig();
            return;
        }

        try
        {
            var json   = File.ReadAllText(_configPath);
            var loaded = JsonSerializer.Deserialize<AppConfig>(json, _jsonOpts);
            if (loaded is not null)
            {
                Config = loaded;
                Sanitize();
                AppLogger.Info("Config loaded successfully.");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load config: {ex.Message}");
            Config = new AppConfig();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOpts);
            File.WriteAllText(_configPath, json);
            AppLogger.Info("Config saved.");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save config: {ex.Message}");
        }
    }

    private void Sanitize()
    {
        Config.MagnifierWidth  = Math.Clamp(Config.MagnifierWidth,  100, 1920);
        Config.MagnifierHeight = Math.Clamp(Config.MagnifierHeight, 100, 1080);
        Config.ZoomFactor      = Math.Clamp(Config.ZoomFactor, AppSettings.MinZoomFactor, AppSettings.MaxZoomFactor);

        Config.OverlayCrosshairSize    = Math.Clamp(Config.OverlayCrosshairSize, AppSettings.MinOverlayCrosshairSize, AppSettings.MaxOverlayCrosshairSize);
        Config.OverlayCrosshairOpacity = Math.Clamp(Config.OverlayCrosshairOpacity, 0.1, 1.0);
        Config.OverlayCrosshairGap       = Math.Clamp(Config.OverlayCrosshairGap, 0, 40);
        Config.OverlayCrosshairThickness = Math.Clamp(Config.OverlayCrosshairThickness, AppSettings.MinOverlayCrosshairThickness, AppSettings.MaxOverlayCrosshairThickness);

        Config.HotkeyToggle = string.IsNullOrWhiteSpace(Config.HotkeyToggle)
            ? AppSettings.DefaultHotkeyToggle
            : Config.HotkeyToggle.Trim();

        Config.HotkeyCrosshair = string.IsNullOrWhiteSpace(Config.HotkeyCrosshair)
            ? AppSettings.DefaultHotkeyCrosshair
            : Config.HotkeyCrosshair.Trim();
    }
}
