using System.Text.Json.Serialization;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Bir tuşun basılma veya bırakılma olayını taşıyan protokol mesajıdır.
/// </summary>
public sealed class ButtonEventMessage : DeviceMessage
{
    [JsonPropertyName("button")]
    public int Button { get; set; }

    [JsonPropertyName("state")]
    public ButtonState State { get; set; }
}
