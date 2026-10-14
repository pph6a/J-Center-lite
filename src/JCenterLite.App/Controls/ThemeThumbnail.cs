using JCenterLite.App.Services;
using JCenterLite.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Color = Windows.UI.Color;

namespace JCenterLite.App.Controls;

/// <summary>
/// Small square theme preview used by the theme lists. Image themes render their own
/// media; video themes render a frame extracted by <see cref="ThemeThumbnailService"/>
/// and fall back to a play glyph when no frame can be produced.
/// </summary>
public sealed class ThemeThumbnail : Grid
{
    private const double DefaultSize = 48;

    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme),
        typeof(ThemeDefinition),
        typeof(ThemeThumbnail),
        new PropertyMetadata(null, OnThemeChanged));

    private readonly Border _frame;
    private readonly Image _image;
    private readonly FontIcon _placeholder;
    private readonly TextBlock _textPreview;
    private ThemeDefinition? _subscribedTheme;
    private int _generation;

    public ThemeThumbnail()
    {
        Width = DefaultSize;
        Height = DefaultSize;

        _image = new Image
        {
            Stretch = Stretch.UniformToFill,
            IsHitTestVisible = false,
        };

        _placeholder = new FontIcon
        {
            Glyph = "\uE768",
            FontSize = DefaultSize * 0.38,
            Foreground = new SolidColorBrush(Colors.White),
            Opacity = 0.85,
            IsHitTestVisible = false,
        };

        _textPreview = new TextBlock
        {
            Text = "Aa",
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontSize = DefaultSize * 0.3,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };

        _frame = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(255, 11, 18, 32)),
            Child = new Grid { Children = { _image, _textPreview, _placeholder } },
        };

        Children.Add(_frame);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        Loaded += OnLoaded;
    }

    public ThemeDefinition? Theme
    {
        get => (ThemeDefinition?)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ThemeThumbnail thumbnail)
        {
            return;
        }

        thumbnail.Subscribe(thumbnail.Theme);
        thumbnail.Update();
    }

    /// <summary>Reloads the tile when the theme's background changes in the editor.</summary>
    private void OnThemePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThemeDefinition.BackgroundFile) or nameof(ThemeDefinition.ThumbnailFile))
        {
            Update();
        }
    }

    private void Subscribe(ThemeDefinition? theme)
    {
        if (ReferenceEquals(_subscribedTheme, theme))
        {
            return;
        }

        if (_subscribedTheme is not null)
        {
            _subscribedTheme.PropertyChanged -= OnThemePropertyChanged;
        }

        _subscribedTheme = theme;
        if (theme is not null)
        {
            theme.PropertyChanged += OnThemePropertyChanged;
        }
    }

    private void Update()
    {
        var generation = ++_generation;
        var theme = Theme;

        _image.Source = null;
        _frame.Background = new SolidColorBrush(theme is null ? Color.FromArgb(255, 11, 18, 32) : ParseColor(theme.BackgroundColor));

        var hasMedia = theme is not null
            && !string.IsNullOrWhiteSpace(theme.BackgroundFile)
            && File.Exists(theme.BackgroundFile);

        // Media themes show a play badge until a frame is ready; element-only themes
        // preview their first label instead, which is what the user recognises.
        _placeholder.Visibility = hasMedia ? Visibility.Visible : Visibility.Collapsed;
        _textPreview.Visibility = hasMedia ? Visibility.Collapsed : Visibility.Visible;
        _textPreview.Text = FirstLabel(theme);
        _textPreview.Foreground = new SolidColorBrush(ParseColor(theme?.Elements.FirstOrDefault()?.Color ?? "#FFFFFFFF"));

        if (theme is null)
        {
            return;
        }

        _ = LoadAsync(theme, generation);
    }

    private async Task LoadAsync(ThemeDefinition theme, int generation)
    {
        string? path = null;
        try
        {
            path = await ThemeThumbnailService.EnsureAsync(theme);
        }
        catch
        {
            return;
        }

        if (path is null || generation != _generation || !ReferenceEquals(theme, Theme))
        {
            return;
        }

        try
        {
            _image.Source = new BitmapImage
            {
                DecodePixelWidth = 128,
                UriSource = new Uri(path, UriKind.Absolute),
            };
            _placeholder.Visibility = Visibility.Collapsed;
            _textPreview.Visibility = Visibility.Collapsed;
        }
        catch
        {
            // Keep whichever placeholder Update() selected; the row stays readable.
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var edge = Math.Min(ActualWidth, ActualHeight);
        if (edge > 0)
        {
            _placeholder.FontSize = Math.Max(10, edge * 0.38);
            _textPreview.FontSize = Math.Max(9, edge * 0.3);
            _textPreview.MaxWidth = Math.Max(24, edge - 8);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Subscribe(Theme);

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Invalidate any in-flight load so a recycled list item cannot paint stale media.
        _generation++;
        Subscribe(null);
    }

    private static string FirstLabel(ThemeDefinition? theme)
    {
        var text = theme?.Elements.FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(text) ? "Aa" : text.Trim();
    }

    private static Color ParseColor(string value)
    {
        try
        {
            var hex = (value ?? string.Empty).TrimStart('#');
            if (hex.Length == 6)
            {
                return Color.FromArgb(
                    255,
                    System.Convert.ToByte(hex[..2], 16),
                    System.Convert.ToByte(hex.Substring(2, 2), 16),
                    System.Convert.ToByte(hex.Substring(4, 2), 16));
            }

            if (hex.Length == 8)
            {
                return Color.FromArgb(
                    System.Convert.ToByte(hex[..2], 16),
                    System.Convert.ToByte(hex.Substring(2, 2), 16),
                    System.Convert.ToByte(hex.Substring(4, 2), 16),
                    System.Convert.ToByte(hex.Substring(6, 2), 16));
            }
        }
        catch
        {
        }

        return Color.FromArgb(255, 11, 18, 32);
    }
}
