using System.Buffers.Binary;

namespace JCenterLite.Core.Protocol;

public static class JCenterTelemetryCodec
{
    public const ushort TelemetryWordCount = 38;

    public static byte[] Build(FanTelemetry telemetry)
    {
        var payload = new byte[71];
        var offset = 0;

        WriteU16(payload, ref offset, telemetry.SurfaceTemperatureDeciCelsius);
        WriteU16(payload, ref offset, telemetry.CoreTemperatureDeciCelsius);
        WriteU16(payload, ref offset, telemetry.PackageTemperatureDeciCelsius);

        payload[offset++] = (byte)(telemetry.VoltageMillivolts & 0xFF);
        payload[offset++] = (byte)((telemetry.VoltageMillivolts >> 8) & 0xFF);
        payload[offset++] = (byte)((telemetry.VoltageMillivolts >> 16) & 0xFF);

        WriteU16(payload, ref offset, telemetry.CpuPowerDeciWatts);
        WriteU16(payload, ref offset, telemetry.CpuUsageDeciPercent);
        WriteU16(payload, ref offset, telemetry.MemoryUsagePercent);
        WriteU16(payload, ref offset, telemetry.CpuSpeedMhz);

        offset += 4; // reserved u32
        WriteU16(payload, ref offset, 0);
        WriteU16(payload, ref offset, 0);
        WriteU16(payload, ref offset, 0);
        WriteU16(payload, ref offset, telemetry.RamTotalGb);
        WriteU16(payload, ref offset, telemetry.GpuTemperatureCelsius);
        WriteU16(payload, ref offset, telemetry.GpuLoadPercent);
        WriteU16(payload, ref offset, telemetry.GpuFrequencyMhz);
        WriteU16(payload, ref offset, 0);
        WriteU16(payload, ref offset, telemetry.CpuFanRpm);
        WriteU16(payload, ref offset, telemetry.GpuMemoryUsedMb);
        WriteU16(payload, ref offset, telemetry.GpuMemoryLoadPercent);
        WriteU16(payload, ref offset, telemetry.GpuLoadPercentAlt);
        WriteU16(payload, ref offset, telemetry.GpuVoltageMillivolts);
        WriteU16(payload, ref offset, telemetry.GpuPowerDeciWatts);
        WriteU16(payload, ref offset, telemetry.GpuFanRpm);
        WriteU16(payload, ref offset, telemetry.RamUsedGb);
        WriteU16(payload, ref offset, telemetry.PumpRpm);
        WriteU16(payload, ref offset, telemetry.HddTemperatureCelsius);
        WriteU16(payload, ref offset, telemetry.HddCapacityGb);
        WriteU16(payload, ref offset, telemetry.HddUsedGb);
        WriteU16(payload, ref offset, telemetry.HddFreeGb);
        WriteU16(payload, ref offset, telemetry.HddUsagePercent);
        WriteU16(payload, ref offset, telemetry.LanUploadBytesPerSecond);
        WriteU16(payload, ref offset, telemetry.LanDownloadBytesPerSecond);
        WriteU16(payload, ref offset, telemetry.RamFreeGb);

        return JCenterFrame.Build(JCenterCommands.FanTelemetry, payload, TelemetryWordCount);
    }

    private static void WriteU16(byte[] buffer, ref int offset, ushort value)
    {
        buffer[offset++] = (byte)(value & 0xFF);
        buffer[offset++] = (byte)(value >> 8);
    }
}
