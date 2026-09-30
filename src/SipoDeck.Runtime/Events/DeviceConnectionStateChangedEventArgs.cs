using SipoDeck.Core.Transport;

namespace SipoDeck.Runtime.Events;

/// <summary>
/// Cihaz bağlantı durumu değiştiğinde yayınlanır. Bağlantı adresi (IP/port/COM port) içermez.
/// </summary>
public sealed class DeviceConnectionStateChangedEventArgs : EventArgs
{
    public DeviceConnectionStateChangedEventArgs(ConnectionType connectionType, ConnectionState state)
    {
        ConnectionType = connectionType;
        State = state;
    }

    public ConnectionType ConnectionType { get; }

    public ConnectionState State { get; }
}
