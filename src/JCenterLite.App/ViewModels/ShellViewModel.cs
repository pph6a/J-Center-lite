using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using JCenterLite.Core.Models;
using JCenterLite.Core.Services;

namespace JCenterLite.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly AdbCli _adb;
    private readonly JCenterDeviceClient _device;
    private readonly HardwareMonitorService _hardware;
    private int _activeForwardPort;

    public ShellViewModel(SettingsService settings, AdbCli adb, JCenterDeviceClient device, HardwareMonitorService hardware)
    {
        _settings = settings;
        _adb = adb;
        _device = device;
        _hardware = hardware;
    }

    public ShellViewModel()
    {
        _settings = new SettingsService();
        _adb = new AdbCli(() => string.Empty);
        _device = new JCenterDeviceClient();
        _hardware = new HardwareMonitorService();
    }

    [ObservableProperty]
    private DeviceStatus _status = new();

    [ObservableProperty]
    private bool _busy;

    [ObservableProperty]
    private bool _needsTakeover;

    [ObservableProperty]
    private string _takeoverHint = string.Empty;

    public event EventHandler<DeviceStatus>? StatusChanged;

    public async Task ConnectAsync()
    {
        if (Busy)
        {
            return;
        }

        Busy = true;
        try
        {
            var status = await _adb.ProbeAsync().ConfigureAwait(true);
            if (!status.DeviceConnected)
            {
                NeedsTakeover = false;
                Publish(status);
                return;
            }

            var preferredPort = _settings.Current.LocalForwardPort;
            var port = _activeForwardPort;
            if (port == 0)
            {
                port = await _adb.IsForwardedAsync(preferredPort, 22222).ConfigureAwait(true)
                    ? preferredPort
                    : FindFreePort(preferredPort);
            }
            var forward = await _adb.ForwardAsync(port, 22222).ConfigureAwait(true);
            if (forward.ExitCode != 0)
            {
                port = FindFreePort(_settings.Current.LocalForwardPort + 1);
                forward = await _adb.ForwardAsync(port, 22222).ConfigureAwait(true);
            }

            if (forward.ExitCode != 0)
            {
                Publish(status with { DeviceConnected = false, Message = $"adb forward 失败：{forward.Error.Trim()}" });
                return;
            }

            _activeForwardPort = port;
            _device.DisposeClient();
            try
            {
                await _device.ConnectAsync("127.0.0.1", port).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Publish(status with { DeviceConnected = false, Message = $"TCP 连接失败：{ex.Message}" });
                return;
            }

            var version = await _device.QueryVersionAsync(TimeSpan.FromMilliseconds(1200)).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(version))
            {
                var original = FindOriginalProcess();
                NeedsTakeover = original is not null;
                TakeoverHint = original is null
                    ? "已建立 TCP，但副屏没有回包。请确认副屏仍在运行，且 adb 转发端口未被占用。"
                    : $"检测到原 J-Center（PID {original.Id}）正在占用副屏连接。副屏只接受一个控制连接；需要接管设备，或先手动退出原 J-Center。";
                Publish(status with { DeviceConnected = false, TerminalVersion = string.Empty, Message = NeedsTakeover ? "原 J-Center 正在占用副屏连接" : "副屏无响应" });
                return;
            }

            NeedsTakeover = false;
            TakeoverHint = string.Empty;
            Publish(status with { DeviceConnected = true, TerminalVersion = version, Message = "Connected" });
        }
        catch (Exception ex)
        {
            Publish(Status with { DeviceConnected = false, Message = $"连接失败：{ex.Message}" });
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// The display firmware accepts a single control socket. When the vendor
    /// client is still running it owns that socket, so the only way to use the
    /// device from J-Center-Lite is to stop the vendor process first.
    /// </summary>
    public async Task TakeOverAsync()
    {
        var processes = Process.GetProcessesByName("WFanManager");
        foreach (var process in processes)
        {
            try
            {
                process.Kill(entireProcessTree: false);
                process.WaitForExit(3000);
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        await Task.Delay(800).ConfigureAwait(true);
        _device.DisposeClient();
        await ConnectAsync().ConfigureAwait(true);
    }

    public async Task DisconnectAsync()
    {
        _device.DisposeClient();
        if (_activeForwardPort != 0)
        {
            await _adb.RemoveForwardAsync(_activeForwardPort).ConfigureAwait(true);
            _activeForwardPort = 0;
        }

        Publish(Status with { DeviceConnected = false, Message = "Disconnected" });
    }

    private static Process? FindOriginalProcess()
    {
        try
        {
            return Process.GetProcessesByName("WFanManager").FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static int FindFreePort(int preferred)
    {
        if (preferred is >= 1024 and <= 65535 && CanBind(preferred))
        {
            return preferred;
        }

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool CanBind(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Publish(DeviceStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }
}
