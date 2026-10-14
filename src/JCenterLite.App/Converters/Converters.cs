using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using JCenterLite.Core.Models;

namespace JCenterLite.App.Converters;

/// <summary>Chinese label for a <see cref="ThemeElementKind"/> in lists and pickers.</summary>
public sealed class ElementKindLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ThemeElementKind kind ? JCenterLite.App.Helpers.ElementKindInfo.Label(kind) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Turns "#AARRGGBB" / "#RRGGBB" element colours into a brush for list swatches.</summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var hex = (value as string ?? string.Empty).TrimStart('#');
            if (hex.Length == 6)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(
                    255,
                    System.Convert.ToByte(hex[..2], 16),
                    System.Convert.ToByte(hex.Substring(2, 2), 16),
                    System.Convert.ToByte(hex.Substring(4, 2), 16)));
            }

            if (hex.Length == 8)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(
                    System.Convert.ToByte(hex[..2], 16),
                    System.Convert.ToByte(hex.Substring(2, 2), 16),
                    System.Convert.ToByte(hex.Substring(4, 2), 16),
                    System.Convert.ToByte(hex.Substring(6, 2), 16)));
            }
        }
        catch
        {
        }

        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

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

/// <summary>Visible while the bound flag is false; used for empty-state hints.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Collapsed;
}
