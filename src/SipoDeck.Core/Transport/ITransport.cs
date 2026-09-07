using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// Cihazlarla ham veri alışverişini sağlayan iletişim kanalını temsil eder.
/// Üst katman bağlantı yönteminin (Wi-Fi/Serial) detaylarını bilmez.
/// Protokolün JSON yapısını yorumlamak transportun sorumluluğu değildir.
/// </summary>
public interface ITransport
{
    string Name { get; }

    ConnectionState State { get; }

    bool IsConnected { get; }

    /// <summary>Cihazdan ham veri alındığında tetiklenir.</summary>
    event EventHandler<byte[]>? DataReceived;

    /// <summary>Bağlantı durumu değiştiğinde tetiklenir.</summary>
    event EventHandler<ConnectionState>? StateChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();

    Task SendAsync(byte[] data, CancellationToken cancellationToken = default);
}
