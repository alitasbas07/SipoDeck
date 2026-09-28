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

    /// <summary>
    /// JSON metnini güvenli şekilde çözümler. Geçersiz JSON, eksik/bilinmeyen "type"
    /// veya desteklenmeyen protokol sürümü istisna fırlatmaz; false ve hata açıklaması döner.
    /// </summary>
    public static bool TryDeserialize(string json, out DeviceMessage? message, out string? error)
    {
        message = null;

        try
        {
            message = Deserialize(json);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            error = $"Geçersiz mesaj: {ex.Message}";
            return false;
        }

        if (message is null)
        {
            error = "Boş mesaj.";
            return false;
        }

        if (message.Version != ProtocolVersion.Current)
        {
            error = $"Desteklenmeyen protokol sürümü: {message.Version}.";
            message = null;
            return false;
        }

        error = null;
        return true;
    }
}
