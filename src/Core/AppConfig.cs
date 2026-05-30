namespace DigitalScope.Core;

public class AppConfig
{
    public int MagnifierWidth  { get; set; } = AppSettings.DefaultMagnifierWidth;
    public int MagnifierHeight { get; set; } = AppSettings.DefaultMagnifierHeight;

    public double ZoomFactor { get; set; } = AppSettings.DefaultZoomFactor;

    public bool   ShowCrosshair  { get; set; } = AppSettings.DefaultShowCrosshair;
    public string CrosshairColor { get; set; } = AppSettings.DefaultCrosshairColor;

    // crosshair overlay
    public bool   OverlayEnabled            { get; set; } = AppSettings.DefaultOverlayEnabled;
    public string OverlayCrosshairType      { get; set; } = AppSettings.DefaultOverlayCrosshairType;
    public string OverlayCrosshairColor     { get; set; } = AppSettings.DefaultOverlayCrosshairColor;
    public int    OverlayCrosshairSize      { get; set; } = AppSettings.DefaultOverlayCrosshairSize;
    public double OverlayCrosshairOpacity   { get; set; } = AppSettings.DefaultOverlayCrosshairOpacity;
    public int    OverlayCrosshairGap       { get; set; } = AppSettings.DefaultOverlayCrosshairGap;
    public double OverlayCrosshairThickness  { get; set; } = AppSettings.DefaultOverlayCrosshairThickness;

    public string HotkeyToggle     { get; set; } = AppSettings.DefaultHotkeyToggle;
    public string HotkeyCrosshair  { get; set; } = AppSettings.DefaultHotkeyCrosshair;
}
