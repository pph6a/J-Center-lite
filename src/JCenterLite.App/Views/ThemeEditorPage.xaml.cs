using JCenterLite.App.Helpers;
using JCenterLite.App.Services;
using JCenterLite.Core.Models;
using JCenterLite.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace JCenterLite.App.Views;

public sealed partial class ThemeEditorPage : Page
{
    private bool _subscribed;
    private bool _syncingColor;

    public ThemeEditorPage()
    {
        ViewModel = AppServices.ThemeEditor;
        InitializeComponent();

        Canvas.SelectionChanged += (_, element) => ViewModel.SelectedElement = element;
        Canvas.ElementMoved += (_, _) => UpdateColorPicker();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ViewModels.ThemeEditorViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_subscribed)
        {
            ViewModel.ThemeChanged += OnThemeChanged;
            ViewModel.ElementChanged += OnElementChanged;
            _subscribed = true;
        }

        Canvas.Refresh();
        UpdateColorPicker();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_subscribed)
        {
            ViewModel.ThemeChanged -= OnThemeChanged;
            ViewModel.ElementChanged -= OnElementChanged;
            _subscribed = false;
        }

        Canvas.ReleaseBackgroundPlayer();
    }

    private void OnThemeChanged(object? sender, ThemeDefinition theme)
    {
        Canvas.Refresh();

        // Let the element list pick up the new item first, then bring it into view.
        DispatcherQueue.TryEnqueue(ScrollSelectedElementIntoView);
    }

    private void ScrollSelectedElementIntoView()
    {
        var element = ViewModel.SelectedElement;
        if (element is null)
        {
            return;
        }

        try
        {
            ElementList.ScrollIntoView(element);
        }
        catch
        {
            // Scrolling is cosmetic; never let it break the edit flow.
        }
    }

    private void OnElementChanged(object? sender, EventArgs e)
    {
        Canvas.SyncElements();
        UpdateColorPicker();
    }

    private void ElementColor_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_syncingColor || ViewModel.SelectedElement is null)
        {
            return;
        }

        ViewModel.SelectedElement.Color = $"#{args.NewColor.A:X2}{args.NewColor.R:X2}{args.NewColor.G:X2}{args.NewColor.B:X2}";
    }

    private async void DeleteTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTheme is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "删除主题",
            Content = $"确定要删除“{ViewModel.SelectedTheme.Name}”吗？此操作不可撤销。",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.DeleteThemeCommand.Execute(null);
        }
    }

    private async void ApplyTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTheme is null)
        {
            return;
        }

        await ViewModel.SaveAsync();
        await AppServices.Display.ApplyThemeAsync(ViewModel.SelectedTheme);
    }

    private async void PickBackground_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTheme is null)
        {
            return;
        }

        var file = await PickerHelper.PickFileAsync(AppServices.MainWindowHandle, ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".mp4", ".webm");
        if (file is null)
        {
            return;
        }

        ApplyBackground(ViewModel.SelectedTheme, file.Path);
    }

    private void ClearBackground_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTheme is null)
        {
            return;
        }

        ApplyBackground(ViewModel.SelectedTheme, string.Empty);
    }

    /// <summary>Swaps the theme background and drops every cached preview of it.</summary>
    private void ApplyBackground(ThemeDefinition theme, string path)
    {
        theme.BackgroundFile = path;
        theme.BackgroundIsVideo = path.Length > 0 && Path.GetExtension(path).ToLowerInvariant()
            is ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv" or ".gif";
        theme.ThumbnailFile = string.Empty;

        ThemeThumbnailService.Invalidate(theme);
        DeleteCachedThumbnail(theme.Id);
        Canvas.Refresh();

        ViewModel.StatusMessage = path.Length == 0 ? "已清除背景" : $"背景：{Path.GetFileName(path)}";
    }

    private static void DeleteCachedThumbnail(string themeId)
    {
        try
        {
            var path = Path.Combine(AppPaths.ThumbnailDirectory, themeId + ".jpg");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // A stale cached frame is harmless; never block the editor because of it.
        }
    }

    private void UpdateColorPicker()
    {
        var element = ViewModel.SelectedElement;
        if (element is null)
        {
            return;
        }

        try
        {
            var hex = element.Color.TrimStart('#');
            if (hex.Length != 8)
            {
                return;
            }

            var color = ColorHelper.FromArgb(
                System.Convert.ToByte(hex[..2], 16),
                System.Convert.ToByte(hex.Substring(2, 2), 16),
                System.Convert.ToByte(hex.Substring(4, 2), 16),
                System.Convert.ToByte(hex.Substring(6, 2), 16));

            if (ElementColor.Color != color)
            {
                _syncingColor = true;
                ElementColor.Color = color;
            }
        }
        catch
        {
            // Invalid colour strings simply leave the picker untouched.
        }
        finally
        {
            _syncingColor = false;
        }
    }
}
