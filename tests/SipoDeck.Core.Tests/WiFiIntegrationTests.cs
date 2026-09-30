using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using SipoDeck.Core.Tests.Helpers;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests;

public class WiFiIntegrationTests
{
    private static WiFiTransport CreateTransport(int port)
        => new(new WiFiTransportSettings { Host = "127.0.0.1", Port = port });

    [Fact]
    public async Task ConnectAsync_ToLocalServer_BecomesConnected()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var accept = server.AcceptAsync();

        await transport.ConnectAsync();
        using var serverSocket = await accept;

        Assert.Equal(ConnectionState.Connected, transport.State);
        Assert.True(transport.IsConnected);
        await transport.DisconnectAsync();
        Assert.Equal(ConnectionState.Disconnected, transport.State);
    }

    [Fact]
    public async Task SendAsync_DeliversDataToServer()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var accept = server.AcceptAsync();
        await transport.ConnectAsync();
        using var serverSocket = await accept;

        await transport.SendAsync(System.Text.Encoding.UTF8.GetBytes("ping\n"));

        Assert.Equal("ping\n", await LocalWebSocketServer.ReceiveTextAsync(serverSocket));
        await transport.DisconnectAsync();
    }

    [Fact]
    public async Task ServerMessage_IsRaisedAsDataFollowedByLineTerminator()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var buffer = new List<byte>();
        transport.DataReceived += (_, data) =>
        {
            lock (buffer)
            {
                buffer.AddRange(data);
                var text = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
                if (text.EndsWith('\n'))
                    received.TrySetResult(text);
            }
        };
        var accept = server.AcceptAsync();
        await transport.ConnectAsync();
        using var serverSocket = await accept;

        await LocalWebSocketServer.SendTextAsync(serverSocket, "{\"a\":1}");

        Assert.Equal("{\"a\":1}\n", await received.Task.WaitAsync(TimeSpan.FromSeconds(15)));
        await transport.DisconnectAsync();
    }

    [Fact]
    public async Task ServerClosesConnection_StateBecomesDisconnected()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var states = new StateWaiter(transport);
        var accept = server.AcceptAsync();
        await transport.ConnectAsync();
        using var serverSocket = await accept;

        await serverSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        await states.WaitForAsync(ConnectionState.Disconnected);

        Assert.Equal(ConnectionState.Disconnected, transport.State);
        Assert.False(transport.IsConnected);
    }

    [Fact]
    public async Task ConnectionDropsAbruptly_StateBecomesDisconnected()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var states = new StateWaiter(transport);
        var accept = server.AcceptAsync();
        await transport.ConnectAsync();
        using var serverSocket = await accept;

        serverSocket.Abort();
        await states.WaitForAsync(ConnectionState.Disconnected);

        Assert.Equal(ConnectionState.Disconnected, transport.State);
    }

    [Fact]
    public async Task ReconnectService_ReestablishesDroppedConnection()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var states = new StateWaiter(transport);
        using var reconnect = new ReconnectService(transport, TimeSpan.FromMilliseconds(20));

        var firstAccept = server.AcceptAsync();
        reconnect.Start();
        using var first = await firstAccept;
        await states.WaitForAsync(ConnectionState.Connected);

        var secondAccept = server.AcceptAsync();
        first.Abort();
        await states.WaitForAsync(ConnectionState.Disconnected);
        using var second = await secondAccept;
        await states.WaitForAsync(ConnectionState.Connected);

        Assert.True(transport.IsConnected);
        reconnect.Stop();
        await transport.DisconnectAsync();
    }

    [Fact]
    public async Task ReconnectService_Stop_PreventsFurtherAttempts()
    {
        using var server = new LocalWebSocketServer();
        var transport = CreateTransport(server.Port);
        var states = new StateWaiter(transport);
        var reconnect = new ReconnectService(transport, TimeSpan.FromMilliseconds(20));

        var accept = server.AcceptAsync();
        reconnect.Start();
        using var socket = await accept;
        await states.WaitForAsync(ConnectionState.Connected);

        reconnect.Stop();
        Assert.False(reconnect.IsRunning);
        await transport.DisconnectAsync();
        await states.WaitForAsync(ConnectionState.Disconnected);

        // Durdurulduktan sonra yeniden bağlanmamalı: sunucu yeni bağlantı görmez.
        var none = server.AcceptAsync();
        await Task.Delay(200);
        Assert.False(none.IsCompleted);
        Assert.False(transport.IsConnected);
    }

    [Fact]
    public async Task ConnectAsync_ToClosedPort_ThrowsAndStaysDisconnected()
    {
        var transport = CreateTransport(LocalWebSocketServer.GetFreePort());

        await Assert.ThrowsAnyAsync<Exception>(() => transport.ConnectAsync());

        Assert.Equal(ConnectionState.Disconnected, transport.State);
    }

    [Fact]
    public async Task ConnectAsync_ToUnresponsiveServer_EndsByCancellationTimeout()
    {
        // Bağlantıyı kabul eden (backlog) ama HTTP/WebSocket el sıkışmasına hiç yanıt vermeyen yerel soket.
        using var silent = new TcpListener(IPAddress.Loopback, 0);
        silent.Start();
        var port = ((IPEndPoint)silent.LocalEndpoint).Port;
        var transport = CreateTransport(port);
        var states = new List<ConnectionState>();
        transport.StateChanged += (_, s) => states.Add(s);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.ConnectAsync(cts.Token));

        Assert.Equal(ConnectionState.Disconnected, transport.State);
        Assert.Equal(new[] { ConnectionState.Connecting, ConnectionState.Disconnected }, states);
    }

    [Theory]
    [InlineData("", 80)]
    [InlineData("127.0.0.1", 0)]
    [InlineData("127.0.0.1", 70000)]
    public async Task ConnectAsync_WithInvalidSettings_Throws(string host, int port)
    {
        var transport = new WiFiTransport(new WiFiTransportSettings { Host = host, Port = port });

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ConnectAsync());

        Assert.Equal(ConnectionState.Disconnected, transport.State);
    }

    [Fact]
    public async Task SendAsync_WhenNotConnected_Throws()
    {
        var transport = CreateTransport(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendAsync(new byte[] { 1 }));
    }
}
