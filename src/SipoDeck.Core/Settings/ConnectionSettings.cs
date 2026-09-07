using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Settings;

/// <summary>
/// Bağlantı ayarları. IP/port/serial bilgileri koda sabitlenmez; kullanıcı tarafından girilir.
/// </summary>
public sealed class ConnectionSettings
{
    public ConnectionType ConnectionType { get; set; } = SettingsDefaults.ConnectionType;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string SerialPortName { get; set; } = string.Empty;

    public int BaudRate { get; set; } = SettingsDefaults.BaudRate;
}
