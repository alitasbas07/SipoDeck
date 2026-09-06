using System.Text.Json;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Protokol mesajlarının JSON serileştirme ve çözümlemesini tek noktadan sağlar.
/// Cihazdan gelen mesajlarda "type" alanının konumundan bağımsız çalışır.
/// </summary>
public static class ProtocolSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        AllowOutOfOrderMetadataProperties = true
    };

    /// <summary>
    /// Bir protokol mesajını JSON metnine dönüştürür.
    /// </summary>
    public static string Serialize(DeviceMessage message)
        => JsonSerializer.Serialize(message, Options);

    /// <summary>
    /// JSON metnini ilgili protokol mesajı türüne çözümler.
    /// </summary>
    public static DeviceMessage? Deserialize(string json)
        => JsonSerializer.Deserialize<DeviceMessage>(json, Options);
}
