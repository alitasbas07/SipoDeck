using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// Wi-Fi üzerinden iletişim için transport temel yapısıdır.
/// Gerçek soket bağlantısı ve veri gönderimi Task 010 kapsamında uygulanacaktır.
/// </summary>
public sealed class WiFiTransport : TransportBase
{
    public WiFiTransport(WiFiTransportSettings settings)
        : base($"Wi-Fi ({settings.Host}:{settings.Port})")
        => Settings = settings;

    public WiFiTransportSettings Settings { get; }

    public override Task ConnectAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Gerçek Wi-Fi bağlantısı Task 010 kapsamında uygulanacaktır.");

    public override Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Gerçek Wi-Fi bağlantısı Task 010 kapsamında uygulanacaktır.");
}
