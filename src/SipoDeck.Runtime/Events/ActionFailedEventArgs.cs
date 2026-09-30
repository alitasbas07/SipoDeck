namespace SipoDeck.Runtime.Events;

/// <summary>
/// Kuyruktan çalıştırılan bir eylem hata fırlattığında yayınlanır.
/// Kuyruk veya Runtime durmaz; sıradaki eylemle devam edilir.
/// </summary>
public sealed class ActionFailedEventArgs : EventArgs
{
    public ActionFailedEventArgs(string actionType, Exception exception)
    {
        ActionType = actionType;
        Exception = exception;
    }

    /// <summary>Hata veren eylemin tür adı (ör. "ActionChain", "RunProgramAction").</summary>
    public string ActionType { get; }

    public Exception Exception { get; }
}
