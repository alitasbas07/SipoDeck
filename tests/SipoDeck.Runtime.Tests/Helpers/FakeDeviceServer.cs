using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace SipoDeck.Runtime.Tests.Helpers;

/// <summary>
/// Yalnızca 127.0.0.1 üzerinde boş bir portta dinleyen yerel WebSocket "cihaz" sunucusu.
/// Bağlantı sayar ve bağlı son istemciye tuş mesajı gönderebilir.
/// </summary>
public sealed class FakeDeviceServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<WebSocket> _sockets = new();
    private readonly SemaphoreSlim _connected = new(0);
    private readonly SemaphoreSlim _disconnected = new(0);
    private int _connectionCount;

    private FakeDeviceServer(int port)
    {
        Port = port;
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    }

    public int Port { get; }

    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    public static FakeDeviceServer Start()
    {
        for (var attempt = 0; ; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var server = new FakeDeviceServer(port);
            try
            {
                server._listener.Start();
            }
            catch (HttpListenerException) when (attempt < 5)
            {
                continue;
            }

            _ = server.AcceptLoopAsync();
            return server;
        }
    }

    public Task WaitForConnectionAsync() => WaitAsync(_connected);

    public Task WaitForDisconnectAsync() => WaitAsync(_disconnected);

    public Task SendButtonAsync(int button, bool pressed)
        => SendTextAsync($"{{\"type\":\"button\",\"device_id\":\"d1\",\"button\":{button},\"state\":\"{(pressed ? "pressed" : "released")}\"}}");

    public async Task SendTextAsync(string text)
    {
        WebSocket socket;
        lock (_sockets)
            socket = _sockets[^1];

        await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task WaitAsync(SemaphoreSlim semaphore)
    {
        if (!await semaphore.WaitAsync(TaskExtensions.DefaultTimeout))
            throw new TimeoutException("Beklenen bağlantı olayı gelmedi.");
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (true)
            {
                var context = await _listener.GetContextAsync();
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(null);
                lock (_sockets)
                    _sockets.Add(wsContext.WebSocket);
                Interlocked.Increment(ref _connectionCount);
                _connected.Release();
                _ = ReceiveLoopAsync(wsContext.WebSocket);
            }
        }
        catch
        {
            // Dinleyici kapatıldı.
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket)
    {
        var buffer = new byte[1024];
        try
        {
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;
                }
            }
        }
        catch
        {
            // Bağlantı koptu.
        }

        _disconnected.Release();
    }

    public ValueTask DisposeAsync()
    {
        try { _listener.Abort(); } catch { }

        lock (_sockets)
        {
            foreach (var socket in _sockets)
                socket.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
