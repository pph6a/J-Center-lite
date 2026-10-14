using JCenterLite.App.Services;
using JCenterLite.App.Views;
using JCenterLite.Core.Services;
using Microsoft.UI.Xaml;

namespace JCenterLite.App;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();

        // A failed theme switch or a media hiccup must leave a trace instead of
        // closing the window without a word.
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
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

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WriteLog("XamlUnhandled", e.Exception);

        // Keep the window alive: one bad operation should not end the session.
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e) =>
        WriteLog("DomainUnhandled", e.ExceptionObject as Exception);

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteLog("TaskUnobserved", e.Exception);
        e.SetObserved();
    }

    private static void WriteLog(string source, Exception? exception)
    {
        try
        {
            AppPaths.EnsureCreated();
            var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {source}: {exception}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(AppPaths.LogDirectory, "crash.log"), line);
        }
        catch
        {
            // Logging must never take down the app.
        }
    }
}
