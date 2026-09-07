using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// USB Serial üzerinden iletişim için transport temel yapısıdır.
/// Gerçek seri port bağlantısı ve veri gönderimi Task 010 kapsamında uygulanacaktır.
/// </summary>
public sealed class SerialTransport : TransportBase
{
    public SerialTransport(SerialTransportSettings settings)
        : base($"Serial ({settings.PortName} @ {settings.BaudRate})")
        => Settings = settings;

    public SerialTransportSettings Settings { get; }

    public override Task ConnectAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Gerçek Serial bağlantısı Task 010 kapsamında uygulanacaktır.");

    public override Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Gerçek Serial bağlantısı Task 010 kapsamında uygulanacaktır.");
}
