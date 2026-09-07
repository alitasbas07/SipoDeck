namespace SipoDeck.Core.Transport;

/// <summary>
/// Bir transport bağlantısının temel durumları.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
