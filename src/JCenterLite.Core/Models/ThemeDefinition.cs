namespace JCenterLite.Core.Models;

public enum ThemeElementKind
{
    CpuTemperature = 0,
    CpuFrequency = 1,
    CpuUsage = 2,
    CpuVoltage = 3,
    CpuPower = 4,
    GpuTemperature = 5,
    GpuUsage = 6,
    GpuFrequency = 7,
    RamTotal = 8,
    RamUsed = 9,
    RamFree = 10,
    RamUsage = 11,
    CpuFanRpm = 12,
    HddTemperature = 13,
    HddTotal = 14,
    HddUsed = 15,
    HddFree = 16,
    HddUsage = 17,
    LanUpload = 18,
    LanDownload = 19,
    Time = 20,
    Date = 21,
    Weekday = 22,
    CpuModel = 23,
    GpuModel = 24,
    FreeText = 25,
}

public sealed class ThemeElementModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ThemeElementKind Kind { get; set; } = ThemeElementKind.CpuTemperature;
    public string Text { get; set; } = "CPU";
    public string Unit { get; set; } = "°C";
    public string Color { get; set; } = "#FFFFFFFF";
    public double X { get; set; } = 40;
    public double Y { get; set; } = 40;
    public double Scale { get; set; } = 1.0;
    public double Rotation { get; set; }
    public double FontSize { get; set; } = 40;
    public int ZIndex { get; set; }
}

public sealed class ThemeDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "My theme";
    public string BackgroundFile { get; set; } = string.Empty;
    public string ThumbnailFile { get; set; } = string.Empty;
    public bool BackgroundIsVideo { get; set; }
    public string BackgroundColor { get; set; } = "#FF0B1220";
    public List<ThemeElementModel> Elements { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class ThemeLibrary
{
    public List<ThemeDefinition> Themes { get; set; } = new();

    public static ThemeLibrary CreateDefault()
    {
        return new ThemeLibrary
        {
            Themes =
            {
                new ThemeDefinition
                {
                    Name = "Minimal clock",
                    Elements =
                    {
                        new ThemeElementModel { Kind = ThemeElementKind.Time, Text = "12:00", Unit = string.Empty, X = 120, Y = 90, FontSize = 72, ZIndex = 2 },
                        new ThemeElementModel { Kind = ThemeElementKind.Date, Text = "2026-10-14", Unit = string.Empty, X = 140, Y = 190, FontSize = 24, ZIndex = 1 },
                    },
                },
                new ThemeDefinition
                {
                    Name = "Thermal dashboard",
                    Elements =
                    {
                        new ThemeElementModel { Kind = ThemeElementKind.CpuTemperature, Text = "CPU", X = 60, Y = 80, FontSize = 56 },
                        new ThemeElementModel { Kind = ThemeElementKind.GpuTemperature, Text = "GPU", X = 300, Y = 80, FontSize = 56 },
                        new ThemeElementModel { Kind = ThemeElementKind.CpuFanRpm, Text = "FAN", Unit = "RPM", X = 60, Y = 260, FontSize = 40 },
                    },
                },
            },
        };
    }
}
