using JCenterLite.App.Helpers;
using JCenterLite.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JCenterLite.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = AppServices.SettingsViewModel;
        InitializeComponent();
    }

    public ViewModels.SettingsViewModel ViewModel { get; }

    private async void PushOta_Click(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickFileAsync(AppServices.MainWindowHandle, ".zip", ".bin", ".img", ".apk");
        if (file is null)
        {
            return;
        }

        var result = await AppServices.Adb.PushAsync(file.Path, "/sdcard/ota_package/" + file.Name);
        ViewModel.StatusMessage = result.ExitCode == 0 ? $"OTA 包已推送：{file.Name}" : $"OTA 推送失败：{result.Error}";
    }

    private async void BrowseAdb_Click(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickExecutableAsync(AppServices.MainWindowHandle);
        if (file is not null)
        {
            ViewModel.AdbPath = file.Path;
        }
    }
}
