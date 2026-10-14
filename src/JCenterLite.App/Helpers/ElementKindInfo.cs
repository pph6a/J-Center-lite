using JCenterLite.Core.Models;

namespace JCenterLite.App.Helpers;

/// <summary>
/// Chinese labels plus sensible text/unit defaults for theme element kinds, so the editor
/// never shows raw enum names such as <c>CpuTemperature</c> to the user.
/// </summary>
public static class ElementKindInfo
{
    public static IReadOnlyList<ElementKindOption> All { get; } = Build();

    public static string Label(ThemeElementKind kind) => kind switch
    {
        ThemeElementKind.CpuTemperature => "CPU 温度",
        ThemeElementKind.CpuFrequency => "CPU 频率",
        ThemeElementKind.CpuUsage => "CPU 占用",
        ThemeElementKind.CpuVoltage => "CPU 电压",
        ThemeElementKind.CpuPower => "CPU 功耗",
        ThemeElementKind.GpuTemperature => "GPU 温度",
        ThemeElementKind.GpuUsage => "GPU 占用",
        ThemeElementKind.GpuFrequency => "GPU 频率",
        ThemeElementKind.RamTotal => "内存总量",
        ThemeElementKind.RamUsed => "内存已用",
        ThemeElementKind.RamFree => "内存可用",
        ThemeElementKind.RamUsage => "内存占用",
        ThemeElementKind.CpuFanRpm => "风扇转速",
        ThemeElementKind.HddTemperature => "硬盘温度",
        ThemeElementKind.HddTotal => "硬盘总量",
        ThemeElementKind.HddUsed => "硬盘已用",
        ThemeElementKind.HddFree => "硬盘可用",
        ThemeElementKind.HddUsage => "硬盘占用",
        ThemeElementKind.LanUpload => "上传速率",
        ThemeElementKind.LanDownload => "下载速率",
        ThemeElementKind.Time => "时间",
        ThemeElementKind.Date => "日期",
        ThemeElementKind.Weekday => "星期",
        ThemeElementKind.CpuModel => "CPU 型号",
        ThemeElementKind.GpuModel => "GPU 型号",
        ThemeElementKind.FreeText => "自定义文本",
        _ => kind.ToString(),
    };

    /// <summary>Short caption used on the canvas until live values replace it.</summary>
    public static string DefaultText(ThemeElementKind kind) => kind switch
    {
        ThemeElementKind.CpuTemperature or ThemeElementKind.CpuFrequency or ThemeElementKind.CpuUsage
            or ThemeElementKind.CpuVoltage or ThemeElementKind.CpuPower or ThemeElementKind.CpuModel => "CPU",
        ThemeElementKind.GpuTemperature or ThemeElementKind.GpuUsage or ThemeElementKind.GpuFrequency
            or ThemeElementKind.GpuModel => "GPU",
        ThemeElementKind.RamTotal or ThemeElementKind.RamUsed or ThemeElementKind.RamFree
            or ThemeElementKind.RamUsage => "RAM",
        ThemeElementKind.CpuFanRpm => "FAN",
        ThemeElementKind.HddTemperature or ThemeElementKind.HddTotal or ThemeElementKind.HddUsed
            or ThemeElementKind.HddFree or ThemeElementKind.HddUsage => "SSD",
        ThemeElementKind.LanUpload => "UP",
        ThemeElementKind.LanDownload => "DOWN",
        ThemeElementKind.Time => "12:00",
        ThemeElementKind.Date => "2026-01-01",
        ThemeElementKind.Weekday => "周一",
        ThemeElementKind.FreeText => "文本",
        _ => kind.ToString(),
    };

    public static string DefaultUnit(ThemeElementKind kind) => kind switch
    {
        ThemeElementKind.CpuTemperature or ThemeElementKind.GpuTemperature
            or ThemeElementKind.HddTemperature => "°C",
        ThemeElementKind.CpuFrequency or ThemeElementKind.GpuFrequency => "MHz",
        ThemeElementKind.CpuUsage or ThemeElementKind.GpuUsage
            or ThemeElementKind.RamUsage or ThemeElementKind.HddUsage => "%",
        ThemeElementKind.CpuFanRpm => "RPM",
        ThemeElementKind.LanUpload or ThemeElementKind.LanDownload => "B/s",
        ThemeElementKind.RamTotal or ThemeElementKind.RamUsed or ThemeElementKind.RamFree
            or ThemeElementKind.HddTotal or ThemeElementKind.HddUsed or ThemeElementKind.HddFree => "GB",
        _ => string.Empty,
    };

    private static IReadOnlyList<ElementKindOption> Build() =>
        Enum.GetValues<ThemeElementKind>()
            .Select(kind => new ElementKindOption(kind, Label(kind)))
            .ToList();
}

public sealed record ElementKindOption(ThemeElementKind Kind, string Label)
{
    public override string ToString() => Label;
}
