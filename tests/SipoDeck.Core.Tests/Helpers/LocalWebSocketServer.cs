using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace SipoDeck.Core.Tests.Helpers;

/// <summary>Boş bir localhost portunda çalışan, WebSocket bağlantılarını sırayla kabul eden HttpListener sunucusu.</summary>
public sealed class LocalWebSocketServer : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(15);

    private readonly HttpListener _listener = new();

    public LocalWebSocketServer()
    {
        // Boş port seçimi ile dinleme başlatma arasında çakışma olursa yeni port denenir.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var port = GetFreePort();
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                _listener.Start();
                Port = port;
                return;
            }
            catch (HttpListenerException)
            {
            }
        }

        throw new InvalidOperationException("Boş port bulunamadı.");
    }

    public int Port { get; }

    public async Task<WebSocket> AcceptAsync()
    {
        var context = await _listener.GetContextAsync().WaitAsync(Limit);
        var wsContext = await context.AcceptWebSocketAsync(null).WaitAsync(Limit);
        return wsContext.WebSocket;
    }

    public static async Task<string> ReceiveTextAsync(WebSocket socket)
    {
        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None).WaitAsync(Limit);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    public static Task SendTextAsync(WebSocket socket, string text)
        => socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    public static int GetFreePort()
    {
        var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }

    public void Dispose() => _listener.Close();
}
