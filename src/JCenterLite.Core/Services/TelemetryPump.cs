using JCenterLite.Core.Protocol;

namespace JCenterLite.Core.Services;

/// <summary>Periodically pushes telemetry to the display, mirroring the original 100 ms timer.</summary>
public sealed class TelemetryPump : IAsyncDisposable
{
    private readonly HardwareMonitorService _hardware;
    private readonly JCenterDeviceClient _device;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TelemetryPump(HardwareMonitorService hardware, JCenterDeviceClient device)
    {
        _hardware = hardware;
        _device = device;
    }

    public bool IsRunning => _loop is { IsCompleted: false };

    public event EventHandler<FanTelemetry>? TelemetrySent;

    public void Start(TimeSpan interval)
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(interval, _cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_device.IsConnected)
            {
                continue;
            }

            var telemetry = _hardware.BuildTelemetry();
            var frame = JCenterTelemetryCodec.Build(telemetry);
            await _device.SendAsync(frame, 1, cancellationToken).ConfigureAwait(false);
            TelemetrySent?.Invoke(this, telemetry);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
