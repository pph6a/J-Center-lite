using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using JCenterLite.Core.Models;
using JCenterLite.Core.Protocol;
using LibreHardwareMonitor.Hardware;

namespace JCenterLite.Core.Services;

/// <summary>
/// Reads sensor values through LibreHardwareMonitor. Reading MSRs/SuperIO
/// values usually requires the process to run elevated; the service degrades
/// to the values Windows exposes when that is not possible.
/// </summary>
public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly object _gate = new();
    private bool _opened;
    private long _lastLanRx;
    private long _lastLanTx;
    private DateTimeOffset _lastLanSample = DateTimeOffset.MinValue;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
        };
    }

    public IReadOnlyList<SensorReading> LastReadings { get; private set; } = Array.Empty<SensorReading>();

    public void Open()
    {
        if (_opened)
        {
            return;
        }

        _computer.Open();
        _opened = true;
    }

    public IReadOnlyList<SensorReading> Read()
    {
        // The telemetry pump, the dashboard and the display preview can all read at once;
        // LibreHardwareMonitor is not thread safe, so every read is serialised here.
        lock (_gate)
        {
            return ReadCore();
        }
    }

    private IReadOnlyList<SensorReading> ReadCore()
    {
        Open();
        var readings = new List<SensorReading>();

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
                Collect(hardware, sub, readings);
            }

            Collect(hardware, hardware, readings);
        }

        LastReadings = readings;
        AddMemoryFallback(readings);
        AddNetwork(readings);
        return readings;
    }

    public FanTelemetry BuildTelemetry()
    {
        lock (_gate)
        {
            return BuildTelemetryCore();
        }
    }

    private FanTelemetry BuildTelemetryCore()
    {
        var readings = Read();
        LastReadings = readings;
        var cpuTemp = Value(SensorKind.CpuTemperature);
        var package = Value(SensorKind.CpuPackageTemperature);
        var gpuTemp = Value(SensorKind.GpuTemperature);
        var memory = GetMemory();
        var storage = GetSystemStorage();
        var network = GetNetworkRates();

        return new FanTelemetry
        {
            SurfaceTemperatureDeciCelsius = ToU16(cpuTemp * 10),
            CoreTemperatureDeciCelsius = ToU16((package > 0 ? package : cpuTemp) * 10),
            PackageTemperatureDeciCelsius = ToU16((package > 0 ? package : cpuTemp) * 10),
            VoltageMillivolts = 0,
            CpuPowerDeciWatts = ToU16(Value(SensorKind.CpuPower) * 10),
            CpuUsageDeciPercent = ToU16(Value(SensorKind.CpuLoad) * 10),
            MemoryUsagePercent = ToU16(memory.Load),
            CpuSpeedMhz = ToU16(Value(SensorKind.CpuClock)),
            RamTotalGb = ToU16(memory.TotalGb),
            GpuTemperatureCelsius = ToU16(gpuTemp),
            GpuLoadPercent = ToU16(Value(SensorKind.GpuLoad)),
            GpuFrequencyMhz = ToU16(Value(SensorKind.GpuClock)),
            CpuFanRpm = ToU16(Value(SensorKind.CpuFan)),
            GpuMemoryUsedMb = ToU16(Value(SensorKind.GpuMemoryUsed)),
            GpuMemoryLoadPercent = ToU16(Value(SensorKind.GpuMemoryLoad)),
            GpuLoadPercentAlt = ToU16(Value(SensorKind.GpuLoad)),
            GpuVoltageMillivolts = 0,
            GpuPowerDeciWatts = ToU16(Value(SensorKind.GpuPower) * 10),
            GpuFanRpm = ToU16(Value(SensorKind.SystemFan)),
            RamUsedGb = ToU16(memory.UsedGb),
            PumpRpm = ToU16(Value(SensorKind.PumpFan)),
            HddTemperatureCelsius = ToU16(Value(SensorKind.StorageTemperature)),
            HddCapacityGb = ToU16(storage.TotalGb),
            HddUsedGb = ToU16(storage.UsedGb),
            HddFreeGb = ToU16(storage.FreeGb),
            HddUsagePercent = ToU16(storage.Load),
            LanUploadBytesPerSecond = ToU16(network.UploadBytesPerSecond),
            LanDownloadBytesPerSecond = ToU16(network.DownloadBytesPerSecond),
            RamFreeGb = ToU16(memory.FreeGb),
        };
    }

    private float Value(SensorKind kind)
    {
        var reading = LastReadings.FirstOrDefault(r => r.Kind == kind);
        return reading.Value;
    }

    private static void Collect(IHardware root, IHardware hardware, List<SensorReading> readings)
    {
        var hardwareName = hardware.Name;
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not float value || float.IsNaN(value))
            {
                continue;
            }

            var kind = Map(root.HardwareType, sensor.SensorType, sensor.Name);
            if (kind is null)
            {
                continue;
            }

            var unit = sensor.SensorType switch
            {
                SensorType.Temperature => "°C",
                SensorType.Fan => "RPM",
                SensorType.Clock => "MHz",
                SensorType.Load => "%",
                SensorType.Power => "W",
                SensorType.Voltage => "V",
                SensorType.Data => "GB",
                SensorType.SmallData => "MB",
                SensorType.Throughput => "B/s",
                _ => string.Empty,
            };

            readings.Add(new SensorReading(kind.Value, $"{hardwareName} / {sensor.Name}", value, unit));
        }
    }

    private static SensorKind? Map(HardwareType type, SensorType sensorType, string name)
    {
        return (type, sensorType) switch
        {
            (HardwareType.Cpu, SensorType.Temperature) when name.Contains("Package", StringComparison.OrdinalIgnoreCase) => SensorKind.CpuPackageTemperature,
            (HardwareType.Cpu, SensorType.Temperature) => SensorKind.CpuTemperature,
            (HardwareType.Cpu, SensorType.Load) when name.Contains("Total", StringComparison.OrdinalIgnoreCase) => SensorKind.CpuLoad,
            (HardwareType.Cpu, SensorType.Clock) => SensorKind.CpuClock,
            (HardwareType.Cpu, SensorType.Power) => SensorKind.CpuPower,
            (HardwareType.Cpu, SensorType.Voltage) => SensorKind.CpuVoltage,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Temperature) => SensorKind.GpuTemperature,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Load) when name.Contains("Memory", StringComparison.OrdinalIgnoreCase) => SensorKind.GpuMemoryLoad,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Load) => SensorKind.GpuLoad,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Clock) when name.Contains("Core", StringComparison.OrdinalIgnoreCase) => SensorKind.GpuClock,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Power) => SensorKind.GpuPower,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.Voltage) => SensorKind.GpuVoltage,
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel, SensorType.SmallData) when name.Contains("Used", StringComparison.OrdinalIgnoreCase) => SensorKind.GpuMemoryUsed,
            (HardwareType.Memory, SensorType.Data) when name.Contains("Used", StringComparison.OrdinalIgnoreCase) => SensorKind.MemoryUsed,
            (HardwareType.Memory, SensorType.Data) when name.Contains("Available", StringComparison.OrdinalIgnoreCase) => SensorKind.MemoryAvailable,
            (HardwareType.Memory, SensorType.Load) => SensorKind.MemoryLoad,
            (HardwareType.Storage, SensorType.Temperature) => SensorKind.StorageTemperature,
            (HardwareType.Storage, SensorType.Load) when name.Contains("Used", StringComparison.OrdinalIgnoreCase) => SensorKind.StorageUsed,
            (HardwareType.Motherboard or HardwareType.SuperIO, SensorType.Fan) when name.Contains("Pump", StringComparison.OrdinalIgnoreCase) => SensorKind.PumpFan,
            (HardwareType.Motherboard or HardwareType.SuperIO, SensorType.Fan) => SensorKind.CpuFan,
            _ => null,
        };
    }

    private void AddMemoryFallback(List<SensorReading> readings)
    {
        var memory = GetMemory();
        readings.Add(new SensorReading(SensorKind.MemoryUsed, "System memory used", (float)memory.UsedGb, "GB"));
        readings.Add(new SensorReading(SensorKind.MemoryAvailable, "System memory free", (float)memory.FreeGb, "GB"));
        readings.Add(new SensorReading(SensorKind.MemoryLoad, "System memory load", (float)memory.Load, "%"));
    }

    private void AddNetwork(List<SensorReading> readings)
    {
        var rates = GetNetworkRates();
        readings.Add(new SensorReading(SensorKind.LanUpload, "Network upload", (float)rates.UploadBytesPerSecond, "B/s"));
        readings.Add(new SensorReading(SensorKind.LanDownload, "Network download", (float)rates.DownloadBytesPerSecond, "B/s"));
    }

    private static (double TotalGb, double UsedGb, double FreeGb, double Load) GetMemory()
    {
        var status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf(status);
        if (!GlobalMemoryStatusEx(ref status))
        {
            return (0, 0, 0, 0);
        }

        var total = status.ullTotalPhys / 1024d / 1024d / 1024d;
        var free = status.ullAvailPhys / 1024d / 1024d / 1024d;
        var used = total - free;
        return (total, used, free, total <= 0 ? 0 : used / total * 100);
    }

    private static (double TotalGb, double UsedGb, double FreeGb, double Load) GetSystemStorage()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(root);
            var total = drive.TotalSize / 1024d / 1024d / 1024d;
            var free = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
            var used = total - free;
            return (total, used, free, total <= 0 ? 0 : used / total * 100);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private (double UploadBytesPerSecond, double DownloadBytesPerSecond) GetNetworkRates()
    {
        long rx = 0;
        long tx = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            var stats = nic.GetIPv4Statistics();
            rx += stats.BytesReceived;
            tx += stats.BytesSent;
        }

        var now = DateTimeOffset.UtcNow;
        if (_lastLanSample == DateTimeOffset.MinValue)
        {
            _lastLanSample = now;
            _lastLanRx = rx;
            _lastLanTx = tx;
            return (0, 0);
        }

        var seconds = Math.Max(0.001, (now - _lastLanSample).TotalSeconds);
        var upload = Math.Max(0, tx - _lastLanTx) / seconds;
        var download = Math.Max(0, rx - _lastLanRx) / seconds;
        _lastLanSample = now;
        _lastLanRx = rx;
        _lastLanTx = tx;
        return (upload, download);
    }

    private static ushort ToU16(double value) => (ushort)Math.Clamp(Math.Round(value), 0, ushort.MaxValue);

    public void Dispose()
    {
        if (_opened)
        {
            _computer.Close();
            _opened = false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
