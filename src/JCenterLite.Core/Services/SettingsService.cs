using System.Text.Json;
using JCenterLite.Core.Models;

namespace JCenterLite.Core.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppSettings Current { get; private set; } = new();

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            AppPaths.EnsureCreated();
            if (!File.Exists(AppPaths.SettingsFile))
            {
                Current = new AppSettings();
                await SaveCoreAsync(Current, cancellationToken).ConfigureAwait(false);
                return Current;
            }

            AppSettings loaded;
            await using (var stream = File.OpenRead(AppPaths.SettingsFile))
            {
                loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? new AppSettings();
            }

            Current = loaded;
            if (Current.LocalForwardPort == 3535)
            {
                // 3535 is the vendor client's default; avoid a guaranteed conflict.
                Current.LocalForwardPort = 35353;
                await SaveCoreAsync(Current, cancellationToken).ConfigureAwait(false);
            }

            return Current;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Current = settings;
            await SaveCoreAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task SaveCoreAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.SettingsFile);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ThemeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ThemeLibrary> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        if (!File.Exists(AppPaths.ThemeLibraryFile))
        {
            var library = ThemeLibrary.CreateDefault();
            await SaveAsync(library, cancellationToken).ConfigureAwait(false);
            return library;
        }

        await using var stream = File.OpenRead(AppPaths.ThemeLibraryFile);
        var loaded = await JsonSerializer.DeserializeAsync<ThemeLibrary>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? ThemeLibrary.CreateDefault();
        Normalize(loaded);
        return loaded;
    }

    /// <summary>Repairs element units written by older versions, e.g. "12:00 °C".</summary>
    private static void Normalize(ThemeLibrary library)
    {
        foreach (var theme in library.Themes)
        {
            foreach (var element in theme.Elements)
            {
                if (element.Kind is ThemeElementKind.Time or ThemeElementKind.Date or ThemeElementKind.Weekday or ThemeElementKind.CpuModel or ThemeElementKind.GpuModel)
                {
                    element.Unit = string.Empty;
                }
            }
        }
    }

    public async Task SaveAsync(ThemeLibrary library, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.ThemeLibraryFile);
        await JsonSerializer.SerializeAsync(stream, library, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
