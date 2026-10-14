using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using JCenterLite.Core.Models;

namespace JCenterLite.App.Converters;

/// <summary>One-line description of a theme for the display grid, e.g. "动态 · 3 个元素".</summary>
public sealed class ThemeSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ThemeDefinition theme)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(theme.BackgroundFile))
        {
            var extension = Path.GetExtension(theme.BackgroundFile).ToLowerInvariant();
            var isMotion = extension is ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv" or ".gif";
            parts.Add(isMotion ? "动态" : "图片");
        }

        if (theme.Elements.Count > 0)
        {
            parts.Add($"{theme.Elements.Count} 个元素");
        }

        return parts.Count == 0 ? "空白" : string.Join(" · ", parts);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is not true;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
