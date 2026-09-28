using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// Wi-Fi üzerinden cihazla WebSocket (ws://Host:Port/) ile ham veri alışverişi yapar.
/// Cihaz WebSocket sunucusudur; masaüstü istemci olarak bağlanır. Her WebSocket mesajı
/// bir satır olarak iletilir. Ping/pong ile yanıt vermeyen bağlantı kopmuş sayılır.
/// </summary>
public sealed class WiFiTransport : TransportBase
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(5);

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveCts;

    public WiFiTransport(WiFiTransportSettings settings)
        : base($"Wi-Fi ({settings.Host}:{settings.Port})")
        => Settings = settings;

    public WiFiTransportSettings Settings { get; }

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Settings.Host))
            throw new InvalidOperationException("Cihaz adresi ayarlanmamış.");
        if (Settings.Port is <= 0 or > 65535)
            throw new InvalidOperationException("Geçersiz port.");

        await CloseSocketAsync().ConfigureAwait(false);
        SetState(ConnectionState.Connecting);

        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = KeepAliveInterval;
        socket.Options.KeepAliveTimeout = KeepAliveTimeout;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectTimeout);
            var uri = new UriBuilder("ws", Settings.Host, Settings.Port).Uri;
            await socket.ConnectAsync(uri, timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            socket.Dispose();
            SetState(ConnectionState.Disconnected);
            throw;
        }

        var receiveCts = new CancellationTokenSource();
        _receiveCts = receiveCts;
        _socket = socket;
        SetState(ConnectionState.Connected);
        _ = Task.Run(() => ReceiveLoopAsync(socket, receiveCts.Token));
    }

    public override async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        var socket = _socket ?? throw new InvalidOperationException("Wi-Fi bağlantısı yok.");
        await socket.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task DisconnectAsync()
    {
        await CloseSocketAsync().ConfigureAwait(false);
        await base.DisconnectAsync().ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Cihazın kapatma isteği yanıtlanır; aksi hâlde karşı taraf yanıt bekler.
                    using var timeout = new CancellationTokenSource(ConnectTimeout);
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token)
                        .ConfigureAwait(false);
                    break;
                }

                if (result.Count > 0)
                    RaiseDataReceived(buffer[..result.Count]);

                // WebSocket mesaj sınırı satır sınırıdır; mesaj '\n' ile bitmese de satır tamamlanır.
                if (result.EndOfMessage && (result.Count == 0 || buffer[result.Count - 1] != (byte)'\n'))
                    RaiseDataReceived([(byte)'\n']);
            }
        }
        catch
        {
            // Bağlantı koptu, ping yanıtı gelmedi veya kapatma istendi.
        }

        // Soket hâlâ bu döngüye aitse bağlantı kendiliğinden kopmuştur.
        if (Interlocked.CompareExchange(ref _socket, null, socket) == socket)
        {
            Interlocked.Exchange(ref _receiveCts, null)?.Dispose();
            socket.Dispose();
            SetState(ConnectionState.Disconnected);
        }
    }

    private async Task CloseSocketAsync()
    {
        var socket = Interlocked.Exchange(ref _socket, null);
        var receiveCts = Interlocked.Exchange(ref _receiveCts, null);
        if (socket is null)
            return;

        try
        {
            if (socket.State == WebSocketState.Open)
            {
                using var timeout = new CancellationTokenSource(ConnectTimeout);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
            // Kapatma el sıkışması başarısız olsa da kaynak serbest bırakılır.
        }

        receiveCts?.Cancel();
        receiveCts?.Dispose();
        socket.Dispose();
    }
}
