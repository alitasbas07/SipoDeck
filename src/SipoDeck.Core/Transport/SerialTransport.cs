using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// USB Serial üzerinden cihazla ham veri alışverişi yapar. Port adı ve baud rate
/// ayarlardan gelir. Okuma arka planda yapılır; port kapanır veya cihaz çıkarılırsa
/// durum <see cref="ConnectionState.Disconnected"/> olur.
/// </summary>
public sealed class SerialTransport : TransportBase
{
    private SerialPort? _port;

    public SerialTransport(SerialTransportSettings settings)
        : base($"Serial ({settings.PortName} @ {settings.BaudRate})")
        => Settings = settings;

    public SerialTransportSettings Settings { get; }

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Settings.PortName))
            throw new InvalidOperationException("Serial port adı ayarlanmamış.");
        if (Settings.BaudRate <= 0)
            throw new InvalidOperationException("Geçersiz baud rate.");

        ClosePort();
        SetState(ConnectionState.Connecting);

        var port = new SerialPort(Settings.PortName, Settings.BaudRate);
        try
        {
            await Task.Run(port.Open, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            port.Dispose();
            SetState(ConnectionState.Disconnected);
            throw;
        }

        _port = port;
        SetState(ConnectionState.Connected);
        _ = Task.Run(() => ReadLoopAsync(port));
    }

    public override async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        var port = _port ?? throw new InvalidOperationException("Serial bağlantısı yok.");
        await port.BaseStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }

    public override Task DisconnectAsync()
    {
        ClosePort();
        return base.DisconnectAsync();
    }

    private async Task ReadLoopAsync(SerialPort port)
    {
        var buffer = new byte[1024];
        try
        {
            while (true)
            {
                var read = await port.BaseStream.ReadAsync(buffer).ConfigureAwait(false);
                if (read <= 0)
                    break;

                RaiseDataReceived(buffer[..read]);
            }
        }
        catch
        {
            // Port kapatıldı veya cihaz çıkarıldı; aşağıda bağlantı kopmuş sayılır.
        }

        // Port hâlâ bu okuma döngüsüne aitse bağlantı kopmuştur.
        if (Interlocked.CompareExchange(ref _port, null, port) == port)
        {
            port.Dispose();
            SetState(ConnectionState.Disconnected);
        }
    }

    private void ClosePort()
    {
        var port = Interlocked.Exchange(ref _port, null);
        if (port is null)
            return;

        try
        {
            port.Close();
        }
        catch
        {
            // Kapatma sırasında oluşan hatalar yok sayılır; kaynak yine de serbest bırakılır.
        }

        port.Dispose();
    }
}
