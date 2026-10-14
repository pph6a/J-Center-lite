using System.Collections.Concurrent;
using JCenterLite.Core.Models;
using JCenterLite.Core.Services;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;

namespace JCenterLite.App.Services;

/// <summary>
/// Resolves a thumbnail image file for a theme. Image and GIF themes use their own
/// background media; video themes get a real first-frame snapshot extracted once and
/// cached under %LOCALAPPDATA%\JCenterLite\media\thumbs.
/// </summary>
public static class ThemeThumbnailService
{
    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".mov", ".avi", ".mkv"];

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private static readonly ConcurrentDictionary<string, Task<string?>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Task<string?> EnsureAsync(ThemeDefinition? theme, CancellationToken cancellationToken = default)
    {
        if (theme is null)
        {
            return Task.FromResult<string?>(null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var key = string.IsNullOrWhiteSpace(theme.Id)
            ? "path:" + theme.BackgroundFile
            : "id:" + theme.Id;
        return Cache.GetOrAdd(key, _ => ResolveAsync(theme));
    }

    private static async Task<string?> ResolveAsync(ThemeDefinition theme)
    {
        try
        {
            var background = theme.BackgroundFile;
            if (string.IsNullOrWhiteSpace(background) || !File.Exists(background))
            {
                return null;
            }

            var extension = Path.GetExtension(background).ToLowerInvariant();
            if (VideoExtensions.Contains(extension))
            {
                return await ResolveVideoAsync(theme, background).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(theme.ThumbnailFile) && File.Exists(theme.ThumbnailFile))
            {
                return theme.ThumbnailFile;
            }

            return ImageExtensions.Contains(extension) ? background : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> ResolveVideoAsync(ThemeDefinition theme, string background)
    {
        var cached = Path.Combine(AppPaths.ThumbnailDirectory, Sanitize(theme.Id) + ".jpg");
        if (File.Exists(cached))
        {
            theme.ThumbnailFile = cached;
            return cached;
        }

        var extracted = await TryExtractFrameAsync(background, cached).ConfigureAwait(false);
        if (extracted is not null)
        {
            theme.ThumbnailFile = extracted;
            return extracted;
        }

        // Fall back to a thumbnail shipped with the theme, if the codec is unavailable.
        return !string.IsNullOrWhiteSpace(theme.ThumbnailFile) && File.Exists(theme.ThumbnailFile)
            ? theme.ThumbnailFile
            : null;
    }

    private static async Task<string?> TryExtractFrameAsync(string path, string target)
    {
        try
        {
            AppPaths.EnsureCreated();
            var file = await StorageFile.GetFileFromPathAsync(path);
            var clip = await MediaClip.CreateFromFileAsync(file);
            var composition = new MediaComposition();
            composition.Clips.Add(clip);

            var position = composition.Duration > TimeSpan.FromSeconds(1)
                ? TimeSpan.FromMilliseconds(500)
                : TimeSpan.Zero;

            using var thumbnail = await composition.GetThumbnailAsync(position, 480, 480, VideoFramePrecision.NearestFrame);
            if (thumbnail is null)
            {
                return null;
            }

            Directory.CreateDirectory(AppPaths.ThumbnailDirectory);
            var folder = await StorageFolder.GetFolderFromPathAsync(AppPaths.ThumbnailDirectory);
            var targetFile = await folder.CreateFileAsync(Path.GetFileName(target), CreationCollisionOption.ReplaceExisting);
            using var output = await targetFile.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAsync(thumbnail, output);
            return File.Exists(target) ? target : null;
        }
        catch
        {
            // Unsupported codecs, missing media components or denied file access all
            // degrade to the caller's placeholder; they must never surface as an error.
            return null;
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "theme";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }
}
