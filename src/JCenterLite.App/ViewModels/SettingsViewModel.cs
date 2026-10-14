using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JCenterLite.App.Services;
using JCenterLite.Core.Protocol;
using JCenterLite.Core.Services;

namespace JCenterLite.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly AdbCli _adb;
    private readonly UpdateService _updates;

    public SettingsViewModel(SettingsService settings, AdbCli adb, UpdateService updates, HardwareMonitorService hardware)
    {
        _settings = settings;
        _adb = adb;
        _updates = updates;
        Load();
    }

    public SettingsViewModel()
    {
        _settings = new SettingsService();
        _adb = new AdbCli(() => string.Empty);
        _updates = new UpdateService(new HttpClient(), "J-Center-Lite/J-Center-Lite");
        Load();
    }

    [ObservableProperty]
    private string _adbPath = string.Empty;

    [ObservableProperty]
    private string _ffmpegPath = string.Empty;

    [ObservableProperty]
    private int _forwardPort = 3535;

    [ObservableProperty]
    private int _telemetryIntervalMs = 100;

    [ObservableProperty]
    private bool _autoConnect = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _closeToTray = true;

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private int _languageIndex;

    [ObservableProperty]
    private string _temperatureUnit = "C";

    [ObservableProperty]
    private string _themeMode = "System";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string DetectedAdb => _adb.ResolveAdbPath();

    [RelayCommand]
    private async Task ImportJCenterThemesAsync()
    {
        var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "horiemtech", "J-Center");
        if (!Directory.Exists(source))
        {
            StatusMessage = "未找到原 J-Center 安装目录";
            return;
        }

        var result = await new JCenterThemeMigrator().ImportAsync(source).ConfigureAwait(true);
        await AppServices.ReloadThemesAsync().ConfigureAwait(true);
        StatusMessage = $"已导入 {result.ImportedThemes} 个主题（媒体 {result.CustomMediaThemes}，布局 {result.LayoutThemes}，内置 {result.BuiltinThemes}）";
        if (result.MissingFiles.Count > 0)
        {
            StatusMessage += $"；{result.MissingFiles.Distinct(StringComparer.OrdinalIgnoreCase).Count()} 个源文件缺失";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var current = _settings.Current;
        var languageChanged = current.LanguageIndex != (byte)Math.Clamp(LanguageIndex, 0, 1);
        current.AdbPath = AdbPath;
        current.FfmpegPath = FfmpegPath;
        current.LocalForwardPort = Math.Clamp(ForwardPort, 1024, 65535);
        current.TelemetryIntervalMs = Math.Clamp(TelemetryIntervalMs, 50, 5000);
        current.AutoConnect = AutoConnect;
        current.StartWithWindows = StartWithWindows;
        current.MinimizeToTray = MinimizeToTray;
        current.CloseToTray = CloseToTray;
        current.AutoCheckUpdates = AutoCheckUpdates;
        current.LanguageIndex = (byte)Math.Clamp(LanguageIndex, 0, 1);
        current.TemperatureUnit = TemperatureUnit;
        current.ThemeMode = ThemeMode;

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            StartupService.SetEnabled(StartWithWindows, Environment.ProcessPath);
        }

        await _settings.SaveAsync(current).ConfigureAwait(true);
        if (languageChanged && AppServices.Device.IsConnected)
        {
            await AppServices.Device.SendAsync(JCenterCommandBuilder.Language((byte)Math.Clamp(LanguageIndex, 0, 1))).ConfigureAwait(true);
        }
        StatusMessage = "设置已保存";
        OnPropertyChanged(nameof(DetectedAdb));
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        var release = await _updates.GetLatestAsync(includePrerelease: false).ConfigureAwait(true);
        StatusMessage = release is null ? "没有找到更新信息" : $"最新版本：{release.TagName}";
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        AppPaths.EnsureCreated();
        Process.Start(new ProcessStartInfo { FileName = AppPaths.Root, UseShellExecute = true });
    }

    private void Load()
    {
        var current = _settings.Current;
        AdbPath = current.AdbPath;
        FfmpegPath = current.FfmpegPath;
        ForwardPort = current.LocalForwardPort;
        TelemetryIntervalMs = current.TelemetryIntervalMs;
        AutoConnect = current.AutoConnect;
        StartWithWindows = current.StartWithWindows || StartupService.IsEnabled();
        MinimizeToTray = current.MinimizeToTray;
        CloseToTray = current.CloseToTray;
        AutoCheckUpdates = current.AutoCheckUpdates;
        LanguageIndex = current.LanguageIndex;
        TemperatureUnit = current.TemperatureUnit;
        ThemeMode = current.ThemeMode;
    }
}
