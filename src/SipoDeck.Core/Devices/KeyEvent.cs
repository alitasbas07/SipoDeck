using SipoDeck.Core.Protocol;

namespace SipoDeck.Core.Devices;

/// <summary>
/// Bir tuşun basılma/bırakılma olayını taşıyan domain girdi olayıdır.
/// Wire protokolünden bağımsızdır; protokol mesajları bu tipe dönüştürülür.
/// </summary>
public sealed class KeyEvent : IDeviceEvent
{
    public KeyEvent(string deviceId, int button, ButtonState state, DateTimeOffset timestamp)
    {
        DeviceId = deviceId;
        Button = button;
        State = state;
        Timestamp = timestamp;
    }

    public string DeviceId { get; }

    public DateTimeOffset Timestamp { get; }

    public int Button { get; }

    public ButtonState State { get; }
}
