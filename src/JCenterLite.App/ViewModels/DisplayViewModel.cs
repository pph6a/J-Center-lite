using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JCenterLite.Core.Models;
using JCenterLite.Core.Protocol;
using JCenterLite.Core.Services;

namespace JCenterLite.App.ViewModels;

public sealed partial class DisplayViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly AdbCli _adb;
    private readonly JCenterDeviceClient _device;
    private readonly ThemeStore _store;
    private readonly ThemeLibrary _library;
    private readonly HardwareMonitorService _hardware;
    private readonly Dictionary<ThemeElementKind, string> _liveValues = new();

    public DisplayViewModel(SettingsService settings, ShellViewModel shell, AdbCli adb, JCenterDeviceClient device, ThemeStore store, ThemeLibrary library, ThemeEditorViewModel editor, HardwareMonitorService hardware)
    {
        _settings = settings;
        _adb = adb;
        _device = device;
        _store = store;
        _library = library;
        _hardware = hardware;
        Shell = shell;
        Editor = editor;
        Brightness = settings.Current.Brightness;
        foreach (var theme in library.Themes)
        {
            Themes.Add(theme);
        }

        SelectedTheme = Themes.FirstOrDefault();
    }

    public DisplayViewModel()
    {
        _settings = new SettingsService();
        _adb = new AdbCli(() => string.Empty);
        _device = new JCenterDeviceClient();
        _store = new ThemeStore();
        _library = ThemeLibrary.CreateDefault();
        _hardware = new HardwareMonitorService();
        Shell = new ShellViewModel();
        Editor = new ThemeEditorViewModel(_library);
        foreach (var theme in _library.Themes)
        {
            Themes.Add(theme);
        }
    }

    public ShellViewModel Shell { get; }
    public ThemeEditorViewModel Editor { get; }
    public ObservableCollection<ThemeDefinition> Themes { get; } = new();

    [ObservableProperty]
    private ThemeDefinition? _selectedTheme;

    [ObservableProperty]
    private double _brightness = 100;

    [ObservableProperty]
    private int _playModeIndex = 2;

    [ObservableProperty]
    private bool _screenOn = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _deviceVersion = "--";

    [ObservableProperty]
    private ThemeDefinition? _appliedTheme;

    /// <summary>Raised after live sensor values were refreshed so the preview can repaint.</summary>
    public event EventHandler? PreviewUpdated;

    public string AppliedThemeName => AppliedTheme is null ? "尚未应用主题" : $"当前副屏主题：{AppliedTheme.Name}";

    public ThemeDefinition? PreviewTheme => AppliedTheme ?? SelectedTheme;

    /// <summary>Live value lookup used by the 480x480 preview; null falls back to the theme label.</summary>
    public string? ValueFor(ThemeElementKind kind) =>
        _liveValues.TryGetValue(kind, out var value) ? value : null;

    public async Task RefreshPreviewAsync()
    {
        try
        {
            var telemetry = await Task.Run(_hardware.BuildTelemetry).ConfigureAwait(true);
            UpdateLiveValues(telemetry);
        }
        catch
        {
            // Sensors can be unavailable without elevation; keep the last known values.
        }

        PreviewUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateLiveValues(FanTelemetry telemetry)
    {
        _liveValues[ThemeElementKind.CpuTemperature] = $"{telemetry.SurfaceTemperatureDeciCelsius / 10.0:0.#} °C";
        _liveValues[ThemeElementKind.CpuFrequency] = $"{telemetry.CpuSpeedMhz} MHz";
        _liveValues[ThemeElementKind.CpuUsage] = $"{telemetry.CpuUsageDeciPercent / 10.0:0.#} %";
        _liveValues[ThemeElementKind.CpuVoltage] = $"{telemetry.VoltageMillivolts / 1000.0:0.00} V";
        _liveValues[ThemeElementKind.CpuPower] = $"{telemetry.CpuPowerDeciWatts / 10.0:0.#} W";
        _liveValues[ThemeElementKind.GpuTemperature] = $"{telemetry.GpuTemperatureCelsius} °C";
        _liveValues[ThemeElementKind.GpuUsage] = $"{telemetry.GpuLoadPercent} %";
        _liveValues[ThemeElementKind.GpuFrequency] = $"{telemetry.GpuFrequencyMhz} MHz";
        _liveValues[ThemeElementKind.RamTotal] = $"{telemetry.RamTotalGb} GB";
        _liveValues[ThemeElementKind.RamUsed] = $"{telemetry.RamUsedGb} GB";
        _liveValues[ThemeElementKind.RamFree] = $"{telemetry.RamFreeGb} GB";
        _liveValues[ThemeElementKind.RamUsage] = $"{telemetry.MemoryUsagePercent} %";
        _liveValues[ThemeElementKind.CpuFanRpm] = $"{telemetry.CpuFanRpm} RPM";
        _liveValues[ThemeElementKind.HddTemperature] = $"{telemetry.HddTemperatureCelsius} °C";
        _liveValues[ThemeElementKind.HddTotal] = $"{telemetry.HddCapacityGb} GB";
        _liveValues[ThemeElementKind.HddUsed] = $"{telemetry.HddUsedGb} GB";
        _liveValues[ThemeElementKind.HddFree] = $"{telemetry.HddFreeGb} GB";
        _liveValues[ThemeElementKind.HddUsage] = $"{telemetry.HddUsagePercent} %";
        _liveValues[ThemeElementKind.LanUpload] = $"{telemetry.LanUploadBytesPerSecond} B/s";
        _liveValues[ThemeElementKind.LanDownload] = $"{telemetry.LanDownloadBytesPerSecond} B/s";

        var now = DateTime.Now;
        _liveValues[ThemeElementKind.Time] = now.ToString("HH:mm", CultureInfo.InvariantCulture);
        _liveValues[ThemeElementKind.Date] = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _liveValues[ThemeElementKind.Weekday] = now.ToString("ddd", CultureInfo.GetCultureInfo("zh-CN"));
    }

    partial void OnAppliedThemeChanged(ThemeDefinition? value)
    {
        OnPropertyChanged(nameof(AppliedThemeName));
        OnPropertyChanged(nameof(PreviewTheme));
    }

    partial void OnSelectedThemeChanged(ThemeDefinition? value) => OnPropertyChanged(nameof(PreviewTheme));

    public void ReloadThemes(ThemeLibrary library)
    {
        _library.Themes.Clear();
        _library.Themes.AddRange(library.Themes);
        Themes.Clear();
        foreach (var theme in library.Themes)
        {
            Themes.Add(theme);
        }

        AppliedTheme = AppliedTheme is null ? null : Themes.FirstOrDefault(t => t.Id == AppliedTheme.Id);
        SelectedTheme = Themes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SendBrightnessAsync()
    {
        _settings.Current.Brightness = (byte)Math.Clamp(Brightness, 0, 100);
        await _store.SaveAsync(_library).ConfigureAwait(true);
        await SendAsync(JCenterCommandBuilder.Brightness(_settings.Current.Brightness)).ConfigureAwait(true);
    }

    [RelayCommand]
    private Task SetLoopAsync()
    {
        PlayModeIndex = 0;
        return SendAsync(JCenterCommandBuilder.VideoLoop(true));
    }

    [RelayCommand]
    private Task SetRandomAsync()
    {
        PlayModeIndex = 1;
        return SendAsync(JCenterCommandBuilder.VideoRandom(true));
    }

    [RelayCommand]
    private Task SetOffAsync()
    {
        PlayModeIndex = 2;
        return SendAsync(JCenterCommandBuilder.VideoLoop(false));
    }

    [RelayCommand]
    private Task ToggleScreenAsync()
    {
        ScreenOn = !ScreenOn;
        return SendAsync(JCenterCommandBuilder.Screen(ScreenOn ? ScreenPower.On : ScreenPower.Off));
    }

    [RelayCommand]
    private async Task QueryVersionAsync()
    {
        if (!await EnsureConnectedAsync().ConfigureAwait(true))
        {
            return;
        }

        DeviceVersion = await _device.QueryVersionAsync().ConfigureAwait(true);
        StatusMessage = $"终端版本：{DeviceVersion}";
    }

    [RelayCommand]
    private async Task ApplyThemeAsync()
    {
        if (SelectedTheme is null)
        {
            return;
        }

        await ApplyThemeAsync(SelectedTheme).ConfigureAwait(true);
    }

    public async Task ApplyThemeAsync(ThemeDefinition theme)
    {
        if (!await EnsureConnectedAsync().ConfigureAwait(true))
        {
            return;
        }

        StatusMessage = $"正在应用：{theme.Name}…";
        try
        {
            var backgroundName = string.Empty;
            if (!string.IsNullOrWhiteSpace(theme.BackgroundFile) && File.Exists(theme.BackgroundFile))
            {
                backgroundName = await PushMediaAsync(theme.BackgroundFile).ConfigureAwait(true);
                var extension = Path.GetExtension(backgroundName).ToLowerInvariant();
                if (extension is ".mp4" or ".gif" or ".webm")
                {
                    await SendAsync(JCenterCommandBuilder.SelectMedia(0, backgroundName)).ConfigureAwait(true);
                }
                else
                {
                    await SendAsync(JCenterCommandBuilder.SetBackgroundImage(1, 0, string.Empty, backgroundName)).ConfigureAwait(true);
                }
            }

            await SendAsync(JCenterCustomThemeBuilder.BuildHeader(backgroundName)).ConfigureAwait(true);
            foreach (var chunk in JCenterCustomThemeBuilder.BuildChunks(theme.Elements))
            {
                await SendAsync(chunk).ConfigureAwait(true);
                await Task.Delay(100).ConfigureAwait(true);
            }

            StatusMessage = $"已应用主题：{theme.Name}";
            AppliedTheme = theme;
        }
        catch (Exception ex)
        {
            StatusMessage = $"主题应用失败：{ex.Message}";
        }
    }

    public async Task UploadAndApplyMediaAsync(string localPath)
    {
        if (!await EnsureConnectedAsync().ConfigureAwait(true))
        {
            return;
        }

        var name = await PushMediaAsync(localPath).ConfigureAwait(true);
        await SendAsync(JCenterCommandBuilder.SelectMedia(0, name)).ConfigureAwait(true);
        await SendAsync(JCenterCommandBuilder.SetBackgroundImage(1, 0, string.Empty, name)).ConfigureAwait(true);
        StatusMessage = $"已推送媒体：{name}";
    }

    private async Task<string> PushMediaAsync(string localPath)
    {
        AppPaths.EnsureCreated();
        var extension = Path.GetExtension(localPath);
        var name = $"jc_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        var localCopy = Path.Combine(AppPaths.MediaDirectory, name);
        File.Copy(localPath, localCopy, overwrite: true);
        var remote = $"/sdcard/{name}";
        var push = await _adb.PushAsync(localCopy, remote).ConfigureAwait(true);
        if (push.ExitCode != 0)
        {
            throw new InvalidOperationException(push.Error);
        }

        return name;
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (Shell.Status.DeviceConnected && _device.IsConnected)
        {
            return true;
        }

        await Shell.ConnectAsync().ConfigureAwait(true);
        var ok = Shell.Status.DeviceConnected && _device.IsConnected;
        if (!ok)
        {
            StatusMessage = "设备未连接。请在设置中填写 adb.exe 路径。";
        }

        return ok;
    }

    private async Task SendAsync(byte[] frame)
    {
        if (!_device.IsConnected)
        {
            StatusMessage = "设备未连接";
            return;
        }

        await _device.SendAsync(frame).ConfigureAwait(true);
    }
}
