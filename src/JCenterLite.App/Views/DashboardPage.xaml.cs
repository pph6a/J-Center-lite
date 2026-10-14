using JCenterLite.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JCenterLite.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        ViewModel = AppServices.Dashboard;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.RefreshAsync();
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.Shell.ConnectAsync();
        if (ViewModel.Shell.NeedsTakeover)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "需要接管副屏",
                Content = ViewModel.Shell.TakeoverHint,
                PrimaryButtonText = "结束原 J-Center 并接管",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.Shell.TakeOverAsync();
            }
        }

        await ViewModel.RefreshAsync();
    }

    public ViewModels.DashboardViewModel ViewModel { get; }
}
