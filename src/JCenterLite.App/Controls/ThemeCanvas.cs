using JCenterLite.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Color = Windows.UI.Color;

namespace JCenterLite.App.Controls;

public sealed class ThemeCanvas : Canvas
{
    private const double SurfaceSize = 480;

    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".mov", ".avi", ".mkv"];

    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme),
        typeof(ThemeDefinition),
        typeof(ThemeCanvas),
        new PropertyMetadata(null, OnThemeChanged));

    private FrameworkElement? _dragTarget;
    private Point _dragStart;
    private double _startX;
    private double _startY;
    private MediaPlayer? _backgroundPlayer;

    public ThemeDefinition? Theme
    {
        get => (ThemeDefinition?)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public ThemeElementModel? SelectedElement { get; private set; }

    public event EventHandler<ThemeElementModel?>? SelectionChanged;

    public event EventHandler? ElementMoved;

    /// <summary>Optional live value source: receives the element kind and returns its "value unit" text.</summary>
    public Func<ThemeElementKind, string?>? ValueProvider { get; set; }

    public ThemeCanvas()
    {
        Width = 480;
        Height = 480;
        Background = new SolidColorBrush(Color.FromArgb(255, 11, 18, 32));
    }

    public void Refresh()
    {
        ReleaseBackgroundPlayer();
        Children.Clear();
        SelectedElement = null;
        if (Theme is null)
        {
            return;
        }

        Background = ParseBrush(Theme.BackgroundColor);
        AddBackgroundLayer(Theme);
        foreach (var element in Theme.Elements.OrderBy(e => e.ZIndex))
        {
            var text = new TextBlock
            {
                Text = FormatElementText(element),
                FontSize = Math.Clamp(element.FontSize, 8, 160),
                Foreground = ParseBrush(element.Color),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };

            var host = new Border
            {
                Child = text,
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Colors.Transparent),
                Tag = element,
            };

            host.PointerPressed += OnPointerPressed;
            host.PointerMoved += OnPointerMoved;
            host.PointerReleased += OnPointerReleased;
            SetLeft(host, element.X);
            SetTop(host, element.Y);
            host.RenderTransformOrigin = new Point(0.5, 0.5);
            host.RenderTransform = new RotateTransform { Angle = element.Rotation };
            Children.Add(host);
        }
    }

    /// <summary>Stops and releases the looping background player, if one is active.</summary>
    public void ReleaseBackgroundPlayer()
    {
        if (_backgroundPlayer is null)
        {
            return;
        }

        try
        {
            _backgroundPlayer.Pause();
            _backgroundPlayer.Source = null;
            _backgroundPlayer.Dispose();
        }
        catch
        {
            // Playback teardown is best effort; it must never break the preview.
        }

        _backgroundPlayer = null;
    }

    /// <summary>Draws the theme background media underneath the sensor elements.</summary>
    private void AddBackgroundLayer(ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(theme.BackgroundFile) || !File.Exists(theme.BackgroundFile))
        {
            return;
        }

        var path = theme.BackgroundFile;
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (VideoExtensions.Contains(extension))
        {
            var player = new MediaPlayer
            {
                AutoPlay = true,
                IsLoopingEnabled = true,
                IsMuted = true,
            };
            player.Source = MediaSource.CreateFromUri(new Uri(path, UriKind.Absolute));

            var element = new MediaPlayerElement
            {
                Width = SurfaceSize,
                Height = SurfaceSize,
                Stretch = Stretch.UniformToFill,
                AreTransportControlsEnabled = false,
                IsHitTestVisible = false,
            };
            element.SetMediaPlayer(player);

            _backgroundPlayer = player;
            Children.Add(element);
            player.Play();
            return;
        }

        var image = new Image
        {
            Width = SurfaceSize,
            Height = SurfaceSize,
            Stretch = Stretch.UniformToFill,
            IsHitTestVisible = false,
            Source = CreateBitmap(path),
        };
        Children.Add(image);
    }

    private static BitmapImage CreateBitmap(string path)
    {
        var bitmap = new BitmapImage { DecodePixelWidth = (int)SurfaceSize };
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        return bitmap;
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ThemeCanvas canvas)
        {
            canvas.Refresh();
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement host || host.Tag is not ThemeElementModel element)
        {
            return;
        }

        SelectedElement = element;
        SelectionChanged?.Invoke(this, element);
        _dragTarget = host;
        _dragStart = e.GetCurrentPoint(this).Position;
        _startX = element.X;
        _startY = element.Y;
        host.Opacity = 0.75;
        host.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragTarget is null || _dragTarget.Tag is not ThemeElementModel element)
        {
            return;
        }

        var point = e.GetCurrentPoint(this).Position;
        element.X = Math.Clamp(_startX + point.X - _dragStart.X, 0, 480 - _dragTarget.ActualWidth);
        element.Y = Math.Clamp(_startY + point.Y - _dragStart.Y, 0, 480 - _dragTarget.ActualHeight);
        SetLeft(_dragTarget, element.X);
        SetTop(_dragTarget, element.Y);
        ElementMoved?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragTarget is not null)
        {
            _dragTarget.Opacity = 1;
            _dragTarget.ReleasePointerCapture(e.Pointer);
        }

        _dragTarget = null;
        e.Handled = true;
    }

    private string FormatElementText(ThemeElementModel element)
    {
        if (element.Kind == ThemeElementKind.FreeText)
        {
            return element.Text;
        }

        var live = ValueProvider?.Invoke(element.Kind);
        if (string.IsNullOrWhiteSpace(live))
        {
            return $"{element.Text} {element.Unit}".Trim();
        }

        // Clock-style elements carry a placeholder value as their text, so the live
        // value replaces it instead of being appended to it.
        return HasLabel(element.Kind) ? $"{element.Text} {live}".Trim() : live;
    }

    private static bool HasLabel(ThemeElementKind kind) =>
        kind is not (ThemeElementKind.Time or ThemeElementKind.Date or ThemeElementKind.Weekday);

    private static SolidColorBrush ParseBrush(string value)
    {
        try
        {
            var hex = (value ?? string.Empty).TrimStart('#');
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
}
