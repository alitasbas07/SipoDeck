using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Devices;

/// <summary>
/// Bir ESP32 cihazını temsil eder. Cihaz bir <see cref="ITransport"/> soyutlaması
/// üzerinden haberleşir; doğrudan Wi-Fi veya Serial uygulamasına bağlı değildir.
/// </summary>
public sealed class Device : IDevice
{
    public Device(string id, string name, ConnectionType connectionType, ITransport? transport = null)
    {
        Id = id;
        Name = name;
        ConnectionType = connectionType;
        Transport = transport;
    }

    /// <summary>Benzersiz cihaz kimliği. Değişmez.</summary>
    public string Id { get; }

    /// <summary>Kullanıcı tarafından değiştirilebilen cihaz adı.</summary>
    public string Name { get; set; }

    public string FirmwareVersion { get; set; } = string.Empty;

    public int ProtocolVersion { get; set; }

    public ConnectionType ConnectionType { get; set; }

    /// <summary>Cihazın haberleştiği transport. Bağlanana kadar null olabilir.</summary>
    public ITransport? Transport { get; set; }

    /// <summary>Cihazın bağlantı durumu; bağlı transportun durumunu yansıtır.</summary>
    public ConnectionState State => Transport?.State ?? ConnectionState.Disconnected;
}
