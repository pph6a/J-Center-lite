using JCenterLite.App.Services;
using JCenterLite.App.Views;
using Microsoft.UI.Xaml;

namespace JCenterLite.App;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await AppServices.InitializeAsync().ConfigureAwait(true);
        _window = new MainWindow();
        _window.Activate();
        if (AppServices.Settings.Current.AutoConnect)
        {
            _ = AppServices.Shell.ConnectAsync();
        }
    }
}
