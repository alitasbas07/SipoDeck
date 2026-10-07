using System.Text.Json.Serialization;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Cihazın bağlantı kurulduğunda kendini tanıttığı el sıkışma mesajıdır.
/// Masaüstü uygulaması da bağlandığında yalnızca "type" alanı dolu bir hello göndererek
/// cihazdan tanıtım bilgisini ister.
/// </summary>
public sealed class HelloMessage : DeviceMessage
{
    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("firmware_version")]
    public string FirmwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("protocol_version")]
    public int ProtocolVersion { get; set; }

    [JsonPropertyName("key_count")]
    public int KeyCount { get; set; }

    [JsonPropertyName("rows")]
    public int? Rows { get; set; }

    [JsonPropertyName("cols")]
    public int? Cols { get; set; }

    public DeviceInfo ToDeviceInfo() => new()
    {
        DeviceId = DeviceId,
        DeviceName = DeviceName,
        FirmwareVersion = FirmwareVersion,
        ProtocolVersion = ProtocolVersion,
        KeyCount = KeyCount,
        Rows = Rows,
        Cols = Cols
    };
}
