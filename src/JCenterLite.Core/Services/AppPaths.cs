namespace JCenterLite.Core.Services;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JCenterLite");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string ThemeLibraryFile => Path.Combine(Root, "themes.json");
    public static string MediaDirectory => Path.Combine(Root, "media");
    public static string ThumbnailDirectory => Path.Combine(MediaDirectory, "thumbs");
    public static string LogDirectory => Path.Combine(Root, "logs");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(MediaDirectory);
        Directory.CreateDirectory(ThumbnailDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
