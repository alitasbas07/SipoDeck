using System.Text.Json.Serialization;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Masaüstü uygulamasının cihazdan alabileceği tanım bilgilerini taşır.
/// </summary>
public sealed class DeviceInfo
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("firmware_version")]
    public string FirmwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("protocol_version")]
    public int ProtocolVersion { get; set; }
}
