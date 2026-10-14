using System.Net.Http;
using JCenterLite.App.ViewModels;
using JCenterLite.Core.Services;

namespace JCenterLite.App.Services;

public static class AppServices
{
    private static bool _initialized;
    public static nint MainWindowHandle { get; set; }

    public static SettingsService Settings { get; private set; } = new();
    public static ThemeStore Themes { get; private set; } = new();
    public static AdbCli Adb { get; private set; } = new(() => string.Empty);
    public static JCenterDeviceClient Device { get; private set; } = new();
    public static HardwareMonitorService Hardware { get; private set; } = new();
    public static TelemetryPump Telemetry { get; private set; } = new(new HardwareMonitorService(), new JCenterDeviceClient());
    public static UpdateService Updates { get; private set; } = new(new HttpClient(), "J-Center-Lite/J-Center-Lite");

    public static ShellViewModel Shell { get; private set; } = new();
    public static DashboardViewModel Dashboard { get; private set; } = new();
    public static DisplayViewModel Display { get; private set; } = new();
    public static ThemeEditorViewModel ThemeEditor { get; private set; } = new();
    public static SettingsViewModel SettingsViewModel { get; private set; } = new();

    public static async Task ReloadThemesAsync()
    {
        var library = await Themes.LoadAsync().ConfigureAwait(true);
        Display.ReloadThemes(library);
        ThemeEditor.Reload(library);
    }

    public static async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        AppPaths.EnsureCreated();
        try
        {
            await Settings.LoadAsync().ConfigureAwait(true);
        }
        catch
        {
            // Keep defaults when the settings file is unreadable or corrupted.
        }

        JCenterLite.Core.Models.ThemeLibrary library;
        try
        {
            library = await Themes.LoadAsync().ConfigureAwait(true);
        }
        catch
        {
            library = JCenterLite.Core.Models.ThemeLibrary.CreateDefault();
        }

        Adb = new AdbCli(() => Settings.Current.AdbPath);
        Device = new JCenterDeviceClient();
        Hardware = new HardwareMonitorService();
        Telemetry = new TelemetryPump(Hardware, Device);
        Updates = new UpdateService(new HttpClient(), "J-Center-Lite/J-Center-Lite");

        Shell = new ShellViewModel(Settings, Adb, Device, Hardware);
        Dashboard = new DashboardViewModel(Settings, Shell, Hardware, Telemetry);
        ThemeEditor = new ThemeEditorViewModel(library);
        Display = new DisplayViewModel(Settings, Shell, Adb, Device, Themes, library, ThemeEditor, Hardware);
        SettingsViewModel = new SettingsViewModel(Settings, Adb, Updates, Hardware);
    }
}
