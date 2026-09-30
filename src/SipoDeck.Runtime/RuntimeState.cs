namespace SipoDeck.Runtime;

/// <summary>
/// Runtime'ın yaşam döngüsü durumları. Cihaz/bağlantı durumundan ayrı izlenir.
/// </summary>
public enum RuntimeState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Faulted
}
