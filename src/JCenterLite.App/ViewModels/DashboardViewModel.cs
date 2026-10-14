using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JCenterLite.Core.Models;
using JCenterLite.Core.Services;
using Microsoft.UI.Dispatching;

namespace JCenterLite.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly HardwareMonitorService _hardware;
    private readonly TelemetryPump _pump;
    private readonly DispatcherQueue? _dispatcher = DispatcherQueue.GetForCurrentThread();

    public DashboardViewModel(SettingsService settings, ShellViewModel shell, HardwareMonitorService hardware, TelemetryPump pump)
    {
        _settings = settings;
        Shell = shell;
        _hardware = hardware;
        _pump = pump;
        if (_dispatcher is not null)
        {
            _pump.TelemetrySent += OnTelemetrySent;
        }
        Shell.StatusChanged += OnShellStatusChanged;
    }

    public DashboardViewModel()
    {
        _settings = new SettingsService();
        Shell = new ShellViewModel();
        _hardware = new HardwareMonitorService();
        _pump = new TelemetryPump(_hardware, new JCenterDeviceClient());
    }

    public ShellViewModel Shell { get; }

    public ObservableCollection<SensorReading> Readings { get; } = new();

    [ObservableProperty]
    private string _cpuTemperature = "--";

    [ObservableProperty]
    private string _gpuTemperature = "--";

    [ObservableProperty]
    private string _cpuLoad = "--";

    [ObservableProperty]
    private string _gpuLoad = "--";

    [ObservableProperty]
    private string _cpuFan = "--";

    [ObservableProperty]
    private string _memory = "--";

    [ObservableProperty]
    private string _storage = "--";

    [ObservableProperty]
    private string _network = "--";

    [ObservableProperty]
    private bool _isStreaming;

    public string StreamingButtonText => IsStreaming ? "停止推送" : "开始推送到副屏";

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            var readings = await Task.Run(_hardware.Read).ConfigureAwait(true);
            Readings.Clear();
            foreach (var reading in readings)
            {
                Readings.Add(reading);
            }

            ApplyReadings();
        }
        catch (Exception ex)
        {
            Network = $"传感器读取失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleStreamingAsync()
    {
        if (IsStreaming)
        {
            await _pump.StopAsync().ConfigureAwait(true);
            IsStreaming = false;
            return;
        }

        if (!Shell.Status.DeviceConnected)
        {
            await Shell.ConnectAsync().ConfigureAwait(true);
        }

        if (!Shell.Status.DeviceConnected)
        {
            return;
        }

        StartTelemetry();
    }

    public void StartTelemetry()
    {
        if (IsStreaming || !Shell.Status.DeviceConnected)
        {
            return;
        }

        _pump.Start(TimeSpan.FromMilliseconds(Math.Clamp(_settings.Current.TelemetryIntervalMs, 50, 5000)));
        IsStreaming = true;
    }

    private void OnShellStatusChanged(object? sender, DeviceStatus status)
    {
        if (status.DeviceConnected && _settings.Current.StartTelemetry)
        {
            StartTelemetry();
        }
    }

    [RelayCommand]
    private Task ConnectAsync() => Shell.ConnectAsync();

    private void OnTelemetrySent(object? sender, Core.Protocol.FanTelemetry telemetry)
    {
        if (_dispatcher is null || _dispatcher.HasThreadAccess)
        {
            Apply(telemetry);
            return;
        }

        _dispatcher.TryEnqueue(() => Apply(telemetry));
    }

    partial void OnIsStreamingChanged(bool value) => OnPropertyChanged(nameof(StreamingButtonText));

    private void Apply(Core.Protocol.FanTelemetry telemetry)
    {
        CpuTemperature = $"{telemetry.SurfaceTemperatureDeciCelsius / 10.0:0.#} °C";
        GpuTemperature = $"{telemetry.GpuTemperatureCelsius} °C";
        CpuLoad = $"{telemetry.CpuUsageDeciPercent / 10.0:0.#} %";
        GpuLoad = $"{telemetry.GpuLoadPercent} %";
        CpuFan = $"{telemetry.CpuFanRpm} RPM";
        Memory = $"{telemetry.RamUsedGb} / {telemetry.RamTotalGb} GB";
        Storage = $"{telemetry.HddUsedGb} / {telemetry.HddCapacityGb} GB";
        Network = $"↓ {telemetry.LanDownloadBytesPerSecond} B/s  ↑ {telemetry.LanUploadBytesPerSecond} B/s";
    }

    private void ApplyReadings()
    {
        var telemetry = _hardware.BuildTelemetry();
        Apply(telemetry);
    }
}
