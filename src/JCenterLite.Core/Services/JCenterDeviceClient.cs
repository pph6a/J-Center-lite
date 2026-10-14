using System.Net.Sockets;
using System.Text;
using JCenterLite.Core.Protocol;

namespace JCenterLite.Core.Services;

public sealed class JCenterDeviceClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected => _client?.Connected == true;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client?.Connected == true)
            {
                return;
            }

            _client?.Dispose();
            _client = new TcpClient { NoDelay = true };
            await _client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            _stream = _client.GetStream();
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<byte[]> SendAsync(byte[] frame, int expectedResponseLength = 8, CancellationToken cancellationToken = default)
        => SendAsync(frame, expectedResponseLength, TimeSpan.FromMilliseconds(750), cancellationToken);

    public async Task<byte[]> SendAsync(byte[] frame, int expectedResponseLength, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client?.Connected != true || _stream is null)
            {
                return Array.Empty<byte>();
            }

            await _stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            var buffer = new byte[Math.Max(1, expectedResponseLength)];
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            try
            {
                var read = await _stream.ReadAsync(buffer, timeoutCts.Token).ConfigureAwait(false);
                return buffer.AsSpan(0, read).ToArray();
            }
            catch (OperationCanceledException)
            {
                return Array.Empty<byte>();
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            DisposeClient();
            return Array.Empty<byte>();
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<string> QueryVersionAsync(CancellationToken cancellationToken = default)
        => QueryVersionAsync(TimeSpan.FromMilliseconds(750), cancellationToken);

    public async Task<string> QueryVersionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(JCenterCommandBuilder.QueryTerminalVersion(), 8, timeout, cancellationToken).ConfigureAwait(false);
        return Encoding.ASCII.GetString(response).Trim('\0', ' ', '\r', '\n');
    }

    public void DisposeClient()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        DisposeClient();
        _gate.Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
