namespace JCenterLite.Core.Protocol;

/// <summary>Telemetry pushed to the display roughly ten times per second.</summary>
public sealed record FanTelemetry
{
    public ushort SurfaceTemperatureDeciCelsius { get; init; }
    public ushort CoreTemperatureDeciCelsius { get; init; }
    public ushort PackageTemperatureDeciCelsius { get; init; }
    public uint VoltageMillivolts { get; init; }
    public ushort CpuPowerDeciWatts { get; init; }
    public ushort CpuUsageDeciPercent { get; init; }
    public ushort MemoryUsagePercent { get; init; }
    public ushort CpuSpeedMhz { get; init; }
    public ushort RamTotalGb { get; init; }
    public ushort GpuTemperatureCelsius { get; init; }
    public ushort GpuLoadPercent { get; init; }
    public ushort GpuFrequencyMhz { get; init; }
    public ushort CpuFanRpm { get; init; }
    public ushort GpuMemoryUsedMb { get; init; }
    public ushort GpuMemoryLoadPercent { get; init; }
    public ushort GpuLoadPercentAlt { get; init; }
    public ushort GpuVoltageMillivolts { get; init; }
    public ushort GpuPowerDeciWatts { get; init; }
    public ushort GpuFanRpm { get; init; }
    public ushort RamUsedGb { get; init; }
    public ushort PumpRpm { get; init; }
    public ushort HddTemperatureCelsius { get; init; }
    public ushort HddCapacityGb { get; init; }
    public ushort HddUsedGb { get; init; }
    public ushort HddFreeGb { get; init; }
    public ushort HddUsagePercent { get; init; }
    public ushort LanUploadBytesPerSecond { get; init; }
    public ushort LanDownloadBytesPerSecond { get; init; }
    public ushort RamFreeGb { get; init; }
}
