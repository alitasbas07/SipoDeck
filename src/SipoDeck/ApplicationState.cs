namespace SipoDeck;

/// <summary>
/// Uygulamanın temel yaşam döngüsü durumları. Cihaz/bağlantı durumundan ayrıdır.
/// </summary>
public enum ApplicationState
{
    Starting,
    Running,
    Background,
    Closing
}
