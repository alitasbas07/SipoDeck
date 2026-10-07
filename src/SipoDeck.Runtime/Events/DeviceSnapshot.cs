using SipoDeck.Core.Transport;

namespace SipoDeck.Runtime.Events;

/// <summary>
/// Bir cihazın olay payload'ı olarak dışarı verilebilecek anlık bilgisidir.
/// Transport nesnesini veya bağlantı adresini (IP/port/COM port) içermez.
/// </summary>
public sealed class DeviceSnapshot
{
    public DeviceSnapshot(string id, string name, string firmwareVersion, int protocolVersion, ConnectionType connectionType, int keyCount, int? rows, int? cols)
    {
        Id = id;
        Name = name;
        FirmwareVersion = firmwareVersion;
        ProtocolVersion = protocolVersion;
        ConnectionType = connectionType;
        KeyCount = keyCount;
        Rows = rows;
        Cols = cols;
    }

    public string Id { get; }

    public string Name { get; }

    public string FirmwareVersion { get; }

    public int ProtocolVersion { get; }

    public ConnectionType ConnectionType { get; }

    public int KeyCount { get; }
    public int? Rows { get; }
    public int? Cols { get; }
}
