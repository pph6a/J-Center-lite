using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using JCenterLite.Core.Models;

namespace JCenterLite.Core.Services;

public sealed record ThemeMigrationResult(
    int ImportedThemes,
    int CustomMediaThemes,
    int LayoutThemes,
    int BuiltinThemes,
    int CopiedAssets,
    IReadOnlyList<string> MissingFiles,
    string LibraryPath,
    string MediaDirectory);

/// <summary>
/// Imports themes from a local J-Center installation into the current user's
/// J-Center-Lite library. Nothing is written into the source installation and
/// no vendor asset is copied into the repository; all imported files live under
/// %LOCALAPPDATA%\JCenterLite\media\jcenter-import.
/// </summary>
public sealed class JCenterThemeMigrator
{
    private static readonly string[] MediaExtensions = [".mp4", ".gif", ".webm", ".mov", ".avi", ".mkv", ".png", ".jpg", ".jpeg", ".bmp"];

    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".mov", ".avi", ".mkv"];

    private static readonly (string Preview, ThemeElementKind[] Kinds)[] BuiltinThemes =
    [
        ("LocalVideo/ThemeImage/logo230313.gif", [ThemeElementKind.CpuTemperature]),
        ("LocalVideo/ThemeImage/theme2.png", [ThemeElementKind.CpuTemperature, ThemeElementKind.GpuTemperature, ThemeElementKind.CpuFanRpm]),
        ("LocalVideo/ThemeImage/theme3.png", [ThemeElementKind.CpuTemperature, ThemeElementKind.GpuTemperature]),
        ("LocalVideo/ThemeImage/theme4.gif", [ThemeElementKind.CpuTemperature, ThemeElementKind.CpuUsage, ThemeElementKind.GpuTemperature, ThemeElementKind.GpuUsage]),
        ("LocalVideo/ThemeImage/theme5.gif", [ThemeElementKind.CpuTemperature]),
        ("LocalVideo/ThemeImage/theme6.png", [ThemeElementKind.CpuModel, ThemeElementKind.GpuTemperature, ThemeElementKind.CpuTemperature, ThemeElementKind.CpuFanRpm]),
        ("LocalVideo/ThemeImage/theme7.png", [ThemeElementKind.CpuModel, ThemeElementKind.CpuTemperature]),
        ("LocalVideo/ThemeImage/theme8.png", [ThemeElementKind.CpuTemperature]),
    ];

    private readonly ThemeStore _store;

    public JCenterThemeMigrator(ThemeStore? store = null)
    {
        _store = store ?? new ThemeStore();
    }

    public async Task<ThemeMigrationResult> ImportAsync(string sourceRoot, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"J-Center install directory not found: {sourceRoot}");
        }

        AppPaths.EnsureCreated();
        var importDirectory = Path.Combine(AppPaths.MediaDirectory, "jcenter-import");
        Directory.CreateDirectory(importDirectory);

        var library = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        library.Themes.RemoveAll(theme => theme.Id.StartsWith("jc-import-", StringComparison.OrdinalIgnoreCase));

        var missing = new List<string>();
        var copiedAssets = 0;
        var imported = new List<ThemeDefinition>();

        var customRoot = Path.Combine(sourceRoot, "LocalVideo", "CustomThemeImage");
        var customThemes = ImportCustomMediaThemes(sourceRoot, customRoot, importDirectory, missing, ref copiedAssets);
        imported.AddRange(customThemes);

        var layoutThemes = ImportLayoutThemes(sourceRoot, importDirectory, missing, ref copiedAssets);
        imported.AddRange(layoutThemes);

        var builtinThemes = ImportBuiltinThemes(sourceRoot, importDirectory, missing, ref copiedAssets);
        imported.AddRange(builtinThemes);

        EnsureUniqueNames(imported);

        library.Themes.AddRange(imported);
        await _store.SaveAsync(library, cancellationToken).ConfigureAwait(false);

        return new ThemeMigrationResult(
            imported.Count,
            customThemes.Count,
            layoutThemes.Count,
            builtinThemes.Count,
            copiedAssets,
            missing,
            AppPaths.ThemeLibraryFile,
            importDirectory);
    }

    private static List<ThemeDefinition> ImportCustomMediaThemes(
        string sourceRoot,
        string customRoot,
        string importDirectory,
        List<string> missing,
        ref int copiedAssets)
    {
        var themes = new List<ThemeDefinition>();
        if (!Directory.Exists(customRoot))
        {
            missing.Add(customRoot);
            return themes;
        }

        var configPath = Path.Combine(customRoot, "CustomConfigData.xml");
        var configEntries = File.Exists(configPath) ? ParseCustomConfig(configPath) : new List<CustomConfigEntry>();
        var mediaFiles = Directory.EnumerateFiles(customRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(file => MediaExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToList();
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in configEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.MediaPath))
            {
                continue;
            }

            var mediaFullPath = Path.Combine(sourceRoot, entry.MediaPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(mediaFullPath))
            {
                missing.Add(entry.MediaPath);
                continue;
            }

            var fileName = Path.GetFileName(mediaFullPath);
            if (!referenced.Add(fileName))
            {
                continue;
            }

            var copied = CopyAsset(mediaFullPath, importDirectory, ref copiedAssets);
            themes.Add(CreateMediaTheme(
                DeterministicId("custom:" + entry.MediaPath),
                FriendlyName(entry.ThemeTitle, fileName),
                copied,
                mediaFullPath,
                "JC · 自定义媒体"));
        }

        foreach (var mediaFile in mediaFiles)
        {
            var fileName = Path.GetFileName(mediaFile);
            if (!referenced.Add(fileName))
            {
                continue;
            }

            var copied = CopyAsset(mediaFile, importDirectory, ref copiedAssets);
            themes.Add(CreateMediaTheme(
                DeterministicId("custom:" + fileName),
                FriendlyName(string.Empty, fileName),
                copied,
                mediaFile,
                "JC · 自定义媒体"));
        }

        return themes;
    }

    private static List<ThemeDefinition> ImportLayoutThemes(
        string sourceRoot,
        string importDirectory,
        List<string> missing,
        ref int copiedAssets)
    {
        var themes = new List<ThemeDefinition>();
        var themeRoot = Path.Combine(sourceRoot, "Theme");
        if (!Directory.Exists(themeRoot))
        {
            return themes;
        }

        foreach (var xmlPath in Directory.EnumerateFiles(themeRoot, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var document = XDocument.Load(xmlPath);
                var root = document.Root;
                if (root is null)
                {
                    continue;
                }

                var name = Value(root, "Name");
                var elementNodes = root.Descendants().Where(node => node.Name.LocalName == "ThemeElement").ToList();
                var elements = new List<ThemeElementModel>();
                foreach (var node in elementNodes)
                {
                    var elementType = ParseInt(Value(node, "ElementType"));
                    var dataType = ParseInt(Value(node, "DataType"));
                    if (elementType != 2 || dataType is < 0 or > 22)
                    {
                        continue;
                    }

                    var scaleX = ParseDouble(Value(node, "ScaleX"), 1.0);
                    var kind = (ThemeElementKind)dataType;
                    elements.Add(new ThemeElementModel
                    {
                        Kind = kind,
                        Text = Value(node, "Content"),
                        Unit = UnitFor(kind),
                        Color = NormalizeColor(Value(node, "ColorString")),
                        X = ParseDouble(Value(node, "X"), 40),
                        Y = ParseDouble(Value(node, "Y"), 40),
                        Scale = scaleX,
                        Rotation = ParseDouble(Value(node, "Rotate"), 0),
                        FontSize = Math.Clamp(scaleX * 14, 8, 160),
                        ZIndex = ParseInt(Value(node, "ZIndex")),
                    });
                }

                var background = Path.Combine(themeRoot, Path.GetFileNameWithoutExtension(xmlPath) + ".png");
                var backgroundFile = File.Exists(background) ? CopyAsset(background, importDirectory, ref copiedAssets) : string.Empty;
                themes.Add(new ThemeDefinition
                {
                    Id = DeterministicId("layout:" + xmlPath),
                    Name = "JC · 自定义布局 " + (string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(xmlPath) : name),
                    BackgroundFile = backgroundFile,
                    ThumbnailFile = backgroundFile,
                    BackgroundIsVideo = false,
                    BackgroundColor = "#FF0B1220",
                    Elements = elements,
                    UpdatedAt = File.GetLastWriteTime(xmlPath),
                });
            }
            catch
            {
                missing.Add(xmlPath);
            }
        }

        return themes;
    }

    private static List<ThemeDefinition> ImportBuiltinThemes(
        string sourceRoot,
        string importDirectory,
        List<string> missing,
        ref int copiedAssets)
    {
        var themes = new List<ThemeDefinition>();
        for (var index = 0; index < BuiltinThemes.Length; index++)
        {
            var (relativePreview, kinds) = BuiltinThemes[index];
            var previewPath = Path.Combine(sourceRoot, relativePreview.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(previewPath))
            {
                missing.Add(relativePreview);
                continue;
            }

            var copied = CopyAsset(previewPath, importDirectory, ref copiedAssets);
            themes.Add(new ThemeDefinition
            {
                Id = DeterministicId("builtin:" + relativePreview),
                Name = $"JC · 默认主题 {index + 1}",
                BackgroundFile = copied,
                ThumbnailFile = copied,
                BackgroundIsVideo = Path.GetExtension(previewPath).Equals(".gif", StringComparison.OrdinalIgnoreCase),
                BackgroundColor = "#FF0B1220",
                Elements = CreateBuiltinElements(kinds),
                UpdatedAt = File.GetLastWriteTime(previewPath),
            });
        }

        themes.Add(new ThemeDefinition
        {
            Id = DeterministicId("builtin:blank"),
            Name = "JC · 空白主题",
            BackgroundColor = "#FF0B1220",
            Elements = [],
        });

        return themes;
    }

    private static List<ThemeElementModel> CreateBuiltinElements(ThemeElementKind[] kinds)
    {
        var positions = kinds.Length switch
        {
            1 => new[] { (170d, 190d) },
            2 => new[] { (70d, 150d), (270d, 150d) },
            3 => new[] { (70d, 100d), (270d, 100d), (170d, 260d) },
            4 => new[] { (60d, 100d), (280d, 100d), (60d, 260d), (280d, 260d) },
            _ => new[] { (60d, 100d), (280d, 100d), (60d, 260d), (280d, 260d) },
        };

        var fontSize = kinds.Length switch
        {
            1 => 72d,
            2 => 56d,
            3 => 48d,
            _ => 44d,
        };

        var elements = new List<ThemeElementModel>();
        for (var index = 0; index < kinds.Length; index++)
        {
            var kind = kinds[index];
            var (x, y) = positions[Math.Min(index, positions.Length - 1)];
            elements.Add(new ThemeElementModel
            {
                Kind = kind,
                Text = kind == ThemeElementKind.CpuModel ? "CPU Model" : kind.ToString(),
                Unit = UnitFor(kind),
                Color = "#FFFFFFFF",
                X = x,
                Y = y,
                Scale = fontSize / 14,
                FontSize = fontSize,
                ZIndex = index,
            });
        }

        return elements;
    }

    private static ThemeDefinition CreateMediaTheme(string id, string name, string copiedPath, string sourcePath, string namePrefix)
    {
        var extension = Path.GetExtension(sourcePath);
        // Video themes get a real frame extracted by the app; images and GIFs act as their own thumbnail.
        var thumbnail = VideoExtensions.Contains(extension.ToLowerInvariant()) ? string.Empty : copiedPath;
        return new ThemeDefinition
        {
            Id = id,
            Name = namePrefix + " · " + name,
            BackgroundFile = copiedPath,
            ThumbnailFile = thumbnail,
            BackgroundIsVideo = extension is ".mp4" or ".gif" or ".webm" or ".mov" or ".avi" or ".mkv",
            BackgroundColor = "#FF000000",
            Elements = [],
            UpdatedAt = File.GetLastWriteTime(sourcePath),
        };
    }

    private static string CopyAsset(string sourcePath, string importDirectory, ref int copiedCount)
    {
        var safeName = Sanitize(Path.GetFileName(sourcePath));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(sourcePath).ToLowerInvariant())))[..8];
        var targetPath = Path.Combine(importDirectory, $"{hash}-{safeName}");
        var sourceInfo = new FileInfo(sourcePath);
        var targetInfo = new FileInfo(targetPath);
        if (!targetInfo.Exists || targetInfo.Length != sourceInfo.Length)
        {
            File.Copy(sourcePath, targetPath, overwrite: true);
        }

        copiedCount++;
        return targetPath;
    }

    private static string DeterministicId(string key) =>
        "jc-import-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16].ToLowerInvariant();

    private static void EnsureUniqueNames(List<ThemeDefinition> themes)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var theme in themes)
        {
            if (seen.TryGetValue(theme.Name, out var count))
            {
                seen[theme.Name] = count + 1;
                theme.Name = $"{theme.Name} ({count + 1})";
            }
            else
            {
                seen[theme.Name] = 1;
            }
        }
    }

    private static string FriendlyName(string title, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var match = System.Text.RegularExpressions.Regex.Match(title, @"_(\d{14})$");
            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            {
                return timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            return title;
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static string UnitFor(ThemeElementKind kind) => kind switch
    {
        ThemeElementKind.CpuTemperature or ThemeElementKind.GpuTemperature or ThemeElementKind.HddTemperature => "°C",
        ThemeElementKind.CpuFrequency or ThemeElementKind.GpuFrequency => "MHz",
        ThemeElementKind.CpuUsage or ThemeElementKind.GpuUsage or ThemeElementKind.RamUsage or ThemeElementKind.HddUsage => "%",
        ThemeElementKind.CpuFanRpm => "RPM",
        ThemeElementKind.LanUpload or ThemeElementKind.LanDownload => "KB/s",
        ThemeElementKind.RamTotal or ThemeElementKind.RamUsed or ThemeElementKind.RamFree or ThemeElementKind.HddTotal or ThemeElementKind.HddUsed or ThemeElementKind.HddFree => "GB",
        _ => string.Empty,
    };

    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#FFFFFFFF";
        }

        var value = color.Trim();
        return value.StartsWith('#') ? value : "#" + value;
    }

    private static string Value(XElement element, string name) =>
        element.Elements().FirstOrDefault(child => child.Name.LocalName == name)?.Value ?? string.Empty;

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;

    private static double ParseDouble(string value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;

    private static string Sanitize(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var character in fileName)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private sealed record CustomConfigEntry(
        string ThemeTitle,
        string ThemeName,
        int DeviceIndex,
        string MediaPath,
        string ThumbPath,
        double Angle,
        string Type);

    private static List<CustomConfigEntry> ParseCustomConfig(string path)
    {
        var entries = new List<CustomConfigEntry>();
        var document = XDocument.Load(path);
        foreach (var node in document.Descendants().Where(element => element.Name.LocalName == "DefaultThemeModel"))
        {
            entries.Add(new CustomConfigEntry(
                Value(node, "ThemeTitle"),
                Value(node, "ThemeName"),
                ParseInt(Value(node, "ThemeIndexForDevice")),
                Value(node, "ImageFilePath"),
                Value(node, "ThumbImageFilePath"),
                ParseDouble(Value(node, "Angle"), 0),
                Value(node, "Type")));
        }

        return entries;
    }
}
