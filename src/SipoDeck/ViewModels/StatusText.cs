using SipoDeck.Core.Transport;
using SipoDeck.Runtime;

namespace SipoDeck.ViewModels;

/// <summary>Durum enum'larının Türkçe metin eşlemesi (design-system §9).</summary>
internal static class StatusText
{
    public static string ForRuntime(RuntimeState state) => state switch
    {
        RuntimeState.Running => "Çalışıyor",
        RuntimeState.Starting => "Başlatılıyor",
        RuntimeState.Stopping => "Durduruluyor",
        RuntimeState.Stopped => "Durduruldu",
        RuntimeState.Faulted => "Hata",
        _ => "Bilinmiyor"
    };

    public static string ForConnection(ConnectionState state) => state switch
    {
        ConnectionState.Connected => "Bağlı",
        ConnectionState.Connecting => "Bağlanıyor",
        ConnectionState.Disconnected => "Bağlantı yok",
        ConnectionState.Error => "Bağlantı hatası",
        _ => "Bilinmiyor"
    };

    public static string ForConnectionType(ConnectionType type) => type switch
    {
        ConnectionType.WiFi => "Wi-Fi",
        ConnectionType.Serial => "USB",
        _ => "Bilinmiyor"
    };
}
