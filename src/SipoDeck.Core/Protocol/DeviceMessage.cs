using System.Text.Json.Serialization;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Cihazdan gelen tüm protokol mesajlarının ortak taban modelidir.
/// Mesaj türü JSON içindeki "type" alanı ile ayrılır; yeni olay türleri
/// bu taban sınıftan türetilerek eklenir.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ButtonEventMessage), "button")]
[JsonDerivedType(typeof(HelloMessage), "hello")]
public abstract class DeviceMessage
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = ProtocolVersion.Current;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
