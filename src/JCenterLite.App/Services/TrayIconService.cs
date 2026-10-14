using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace JCenterLite.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _icon;

    public TrayIconService(Action showWindow, Action exitApplication)
    {
        var openItem = new MenuFlyoutItem { Text = "打开 J-Center-Lite" };
        openItem.Click += (_, _) => showWindow();
        var exitItem = new MenuFlyoutItem { Text = "退出" };
        exitItem.Click += (_, _) => exitApplication();

        var menu = new MenuFlyout();
        menu.Items.Add(openItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exitItem);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        _icon = new TaskbarIcon
        {
            ToolTipText = "J-Center-Lite",
            IconSource = new BitmapImage(new Uri(iconPath)),
            ContextFlyout = menu,
            LeftClickCommand = new RelayCommand(showWindow),
            NoLeftClickDelay = true,
        };
        _icon.ForceCreate();
    }

    public void Dispose() => _icon.Dispose();
}
