using JCenterLite.App.Helpers;
using JCenterLite.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Windows.UI;

namespace JCenterLite.App.Views;

public sealed partial class ThemeEditorPage : Page
{
    public ThemeEditorPage()
    {
        ViewModel = AppServices.ThemeEditor;
        InitializeComponent();
        Canvas.SelectionChanged += (_, element) => ViewModel.SelectedElement = element;
        Canvas.ElementMoved += (_, _) => UpdateColorPicker();
        ViewModel.ThemeChanged += (_, theme) => Canvas.Theme = theme;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ViewModel.SelectedTheme))
            {
                Canvas.Theme = ViewModel.SelectedTheme;
            }
        };

        UpdateColorPicker();
        // Stop looping preview media as soon as the page is left.
        Unloaded += (_, _) => Canvas.ReleaseBackgroundPlayer();
    }

    public ViewModels.ThemeEditorViewModel ViewModel { get; }

    private void RefreshPreview_Click(object sender, RoutedEventArgs e) => Canvas.Refresh();

    private void ElementColor_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (ViewModel.SelectedElement is null)
        {
            return;
        }

        ViewModel.SelectedElement.Color = $"#{args.NewColor.A:X2}{args.NewColor.R:X2}{args.NewColor.G:X2}{args.NewColor.B:X2}";
        Canvas.Refresh();
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

        ViewModel.SelectedTheme.BackgroundFile = file.Path;
        ViewModel.SelectedTheme.BackgroundIsVideo = file.FileType is ".mp4" or ".webm" or ".gif";
        ViewModel.StatusMessage = $"背景：{file.Name}";
    }

    private void UpdateColorPicker()
    {
        if (ViewModel.SelectedElement is null)
        {
            return;
        }

        try
        {
            var hex = ViewModel.SelectedElement.Color.TrimStart('#');
            if (hex.Length == 8)
            {
                ElementColor.Color = ColorHelper.FromArgb(System.Convert.ToByte(hex[..2], 16), System.Convert.ToByte(hex.Substring(2, 2), 16), System.Convert.ToByte(hex.Substring(4, 2), 16), System.Convert.ToByte(hex.Substring(6, 2), 16));
            }
        }
        catch
        {
        }
    }
}
