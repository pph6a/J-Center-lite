using JCenterLite.App.Services;
using JCenterLite.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace JCenterLite.App;

public sealed partial class MainWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly TrayIconService _tray;
    private bool _exiting;

    private const double DefaultWidth = 1440;
    private const double DefaultHeight = 900;
    private const double MinimumWidth = 1200;
    private const double MinimumHeight = 800;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    private nint _windowHandle;
    private bool _clampingSize;

    public MainWindow()
    {
        InitializeComponent();
        Title = "J-Center-Lite";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

        var handle = WindowNative.GetWindowHandle(this);
        AppServices.MainWindowHandle = handle;
        var windowId = Win32Interop.GetWindowIdFromWindow(handle);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _windowHandle = handle;
        ApplyInitialSize(handle);
        _appWindow.Changed += OnAppWindowChanged;
        _appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        _appWindow.Closing += OnClosing;
        _tray = new TrayIconService(ShowFromTray, ExitFromTray);

        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(DisplayPage));
        AppServices.Shell.StatusChanged += (_, status) => DispatcherQueue.TryEnqueue(() => UpdateStatus(status));
        UpdateStatus(AppServices.Shell.Status);
    }

    private static double GetScale(nint handle)
    {
        var dpi = GetDpiForWindow(handle);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }

    /// <summary>Sizes the window in DIPs, clamps it to the display work area and centers it.</summary>
    private void ApplyInitialSize(nint handle)
    {
        try
        {
            var scale = GetScale(handle);
            var workArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
            var margin = (int)Math.Round(48 * scale);

            var width = (int)Math.Round(DefaultWidth * scale);
            var height = (int)Math.Round(DefaultHeight * scale);
            width = Math.Min(width, Math.Max(640, workArea.Width - margin));
            height = Math.Min(height, Math.Max(480, workArea.Height - margin));

            var x = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
            var y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);
            _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
        catch
        {
            _appWindow.Resize(new SizeInt32(1440, 900));
        }

        UpdateTitleBarInset();
    }

    /// <summary>Keeps the window from being resized below a usable layout width and height.</summary>
    private void EnforceMinimumSize()
    {
        if (_clampingSize || _windowHandle == 0)
        {
            return;
        }

        var scale = GetScale(_windowHandle);
        var minWidth = (int)Math.Round(MinimumWidth * scale);
        var minHeight = (int)Math.Round(MinimumHeight * scale);
        var size = _appWindow.Size;
        if (size.Width >= minWidth && size.Height >= minHeight)
        {
            return;
        }

        _clampingSize = true;
        try
        {
            _appWindow.Resize(new SizeInt32(Math.Max(size.Width, minWidth), Math.Max(size.Height, minHeight)));
        }
        catch
        {
            // Resizing is best effort; never let layout enforcement crash the window.
        }
        finally
        {
            _clampingSize = false;
        }
    }

    /// <summary>Keeps the custom title bar text clear of the system caption buttons.</summary>
    private void UpdateTitleBarInset()
    {
        var rightInset = _appWindow.TitleBar.RightInset;
        AppTitleBar.Padding = new Thickness(0, 0, Math.Max(148, rightInset), 0);
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            EnforceMinimumSize();
        }

        if (args.DidSizeChange || args.DidPresenterChange)
        {
            UpdateTitleBarInset();
        }
    }

    private void ShowFromTray()
    {
        _appWindow.Show();
        Activate();
    }

    private void ExitFromTray()
    {
        _exiting = true;
        _tray.Dispose();
        Close();
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_exiting && AppServices.Settings.Current.CloseToTray)
        {
            args.Cancel = true;
            sender.Hide();
        }
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var page = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "display" => typeof(DisplayPage),
            "editor" => typeof(ThemeEditorPage),
            "settings" => typeof(SettingsPage),
            _ => typeof(DashboardPage),
        };

        if (ContentFrame.CurrentSourcePageType != page)
        {
            ContentFrame.Navigate(page);
        }
    }

    private void UpdateStatus(Core.Models.DeviceStatus status)
    {
        StatusDot.Fill = new SolidColorBrush(status.DeviceConnected ? Windows.UI.Color.FromArgb(255, 52, 211, 153) : Windows.UI.Color.FromArgb(255, 248, 113, 113));
        StatusText.Text = status.DeviceConnected ? $"已连接 · {status.DeviceModel}" : status.AdbAvailable ? "设备未连接" : "缺少 adb.exe";
    }
}
