using JCenterLite.App.Helpers;
using JCenterLite.App.Services;
using JCenterLite.Core.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JCenterLite.App.Views;

public sealed partial class DisplayPage : Page
{
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _subscribed;

    public DisplayPage()
    {
        ViewModel = AppServices.Display;
        InitializeComponent();

        // The preview reuses the editor's renderer, but with live sensor text instead
        // of the theme's static labels.
        Preview.ValueProvider = ViewModel.ValueFor;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ViewModels.DisplayViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_subscribed)
        {
            ViewModel.PreviewUpdated += OnPreviewUpdated;
            ViewModel.Shell.StatusChanged += OnShellStatusChanged;
            _previewTimer.Tick += OnPreviewTimerTick;
            _subscribed = true;
        }

        UpdatePreviewStatus(ViewModel.Shell.Status);
        Preview.Refresh();
        _previewTimer.Start();
        _ = ViewModel.RefreshPreviewAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _previewTimer.Stop();
        Preview.ReleaseBackgroundPlayer();

        if (_subscribed)
        {
            ViewModel.PreviewUpdated -= OnPreviewUpdated;
            ViewModel.Shell.StatusChanged -= OnShellStatusChanged;
            _previewTimer.Tick -= OnPreviewTimerTick;
            _subscribed = false;
        }
    }

    private void OnPreviewTimerTick(object? sender, object e) => _ = ViewModel.RefreshPreviewAsync();

    private void OnPreviewUpdated(object? sender, EventArgs e) => Preview.Refresh();

    private void OnShellStatusChanged(object? sender, DeviceStatus status) => UpdatePreviewStatus(status);

    private void UpdatePreviewStatus(DeviceStatus status)
    {
        PreviewStatusDot.Fill = new SolidColorBrush(status.DeviceConnected
            ? Windows.UI.Color.FromArgb(255, 52, 211, 153)
            : Windows.UI.Color.FromArgb(255, 248, 113, 113));
        PreviewStatusText.Text = status.DeviceConnected
            ? $"已连接 · {status.DeviceModel}"
            : status.AdbAvailable ? "设备未连接" : "缺少 adb.exe";
    }

    private async void ThemeGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ThemeDefinition theme)
        {
            await ViewModel.ApplyThemeAsync(theme);
        }
    }

    private async void RebootDevice_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Shell.Status.DeviceConnected)
        {
            await ViewModel.Shell.ConnectAsync();
        }

        if (ViewModel.Shell.Status.DeviceConnected)
        {
            await AppServices.Adb.ShellAsync("reboot");
        }
    }

    private async void UploadMedia_Click(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickFileAsync(AppServices.MainWindowHandle, ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".mp4", ".webm");
        if (file is null)
        {
            return;
        }

        await ViewModel.UploadAndApplyMediaAsync(file.Path);
    }
}
