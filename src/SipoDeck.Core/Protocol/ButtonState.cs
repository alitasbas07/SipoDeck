using System.Text.Json.Serialization;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Bir tuşun anlık durumunu temsil eder.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ButtonState>))]
public enum ButtonState
{
    [JsonStringEnumMemberName("pressed")]
    Pressed,

    [JsonStringEnumMemberName("released")]
    Released
}
