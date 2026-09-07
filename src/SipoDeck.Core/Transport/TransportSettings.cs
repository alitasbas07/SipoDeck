namespace SipoDeck.Core.Transport;

/// <summary>
/// Wi-Fi bağlantısı için yapılandırılabilir ayarlar. Değerler koda sabitlenmez.
/// </summary>
public sealed class WiFiTransportSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
}

/// <summary>
/// USB Serial bağlantısı için yapılandırılabilir ayarlar. Değerler koda sabitlenmez.
/// </summary>
public sealed class SerialTransportSettings
{
    public string PortName { get; set; } = string.Empty;

    public int BaudRate { get; set; } = 115200;
}
