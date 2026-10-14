namespace JCenterLite.Core.Models;

public enum SensorKind
{
    CpuTemperature,
    CpuPackageTemperature,
    CpuLoad,
    CpuClock,
    CpuPower,
    CpuVoltage,
    GpuTemperature,
    GpuLoad,
    GpuClock,
    GpuPower,
    GpuVoltage,
    GpuMemoryUsed,
    GpuMemoryLoad,
    MemoryUsed,
    MemoryAvailable,
    MemoryLoad,
    CpuFan,
    PumpFan,
    SystemFan,
    StorageTemperature,
    StorageUsed,
    StorageFree,
    LanUpload,
    LanDownload,
    AmbientNoise,
}

public readonly record struct SensorReading(SensorKind Kind, string Name, float Value, string Unit)
{
    public string DisplayValue => Unit switch
    {
        "MHz" or "RPM" or "GB" or "MB" or "%" or "W" or "V" or "°C" or "dB" => $"{Value:0.#} {Unit}",
        "B/s" => FormatBytes(Value),
        _ => $"{Value:0.##} {Unit}",
    };

    private static string FormatBytes(float value)
    {
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        var index = 0;
        var v = Math.Max(0, value);
        while (v >= 1024 && index < units.Length - 1)
        {
            v /= 1024;
            index++;
        }

        return $"{v:0.#} {units[index]}";
    }
}

public sealed record DeviceStatus
{
    public bool AdbAvailable { get; init; }
    public bool DeviceConnected { get; init; }
    public string DeviceSerial { get; init; } = string.Empty;
    public string DeviceModel { get; init; } = string.Empty;
    public string TerminalVersion { get; init; } = string.Empty;
    public string AdbPath { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class AppSettings
{
    public string AdbPath { get; set; } = string.Empty;
    public string FfmpegPath { get; set; } = string.Empty;
    public int LocalForwardPort { get; set; } = 35353;
    public bool AutoConnect { get; set; } = true;
    public bool StartTelemetry { get; set; } = true;
    public int TelemetryIntervalMs { get; set; } = 100;
    public byte Brightness { get; set; } = 100;
    public byte LanguageIndex { get; set; }
    public string TemperatureUnit { get; set; } = "C";
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool AutoCheckUpdates { get; set; } = true;
    public string ThemeMode { get; set; } = "System";
}
