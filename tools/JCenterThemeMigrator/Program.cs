using JCenterLite.Core.Services;

var source = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\horiemtech\J-Center";
Console.WriteLine($"source: {source}");
Console.WriteLine($"library: {AppPaths.ThemeLibraryFile}");

try
{
    var result = await new JCenterThemeMigrator().ImportAsync(source);
    Console.WriteLine($"imported={result.ImportedThemes} (custom media={result.CustomMediaThemes}, layouts={result.LayoutThemes}, builtin={result.BuiltinThemes})");
    Console.WriteLine($"copied assets={result.CopiedAssets}");
    Console.WriteLine($"media directory: {result.MediaDirectory}");
    if (result.MissingFiles.Count > 0)
    {
        Console.WriteLine("missing files:");
        foreach (var missing in result.MissingFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  - {missing}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"migration failed: {ex.Message}");
    return 1;
}

return 0;
