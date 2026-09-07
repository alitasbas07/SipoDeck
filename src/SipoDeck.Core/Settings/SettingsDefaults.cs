using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Settings;

/// <summary>
/// Ayarların varsayılan değerleri tek bir merkezde tutulur; koda dağıtılmaz.
/// Bağlantı adresi/portu gibi makineye özel değerlerin varsayılanı yoktur (kullanıcı girer).
/// </summary>
public static class SettingsDefaults
{
    public const ConnectionType ConnectionType = Transport.ConnectionType.WiFi;
    public const int BaudRate = 115200;

    public const int FnKey = 0;
    public const int LongPressThresholdMilliseconds = 500;

    public const bool RunAtStartup = false;
    public const bool RunInSystemTray = true;
    public const bool AutoReconnect = true;
}
