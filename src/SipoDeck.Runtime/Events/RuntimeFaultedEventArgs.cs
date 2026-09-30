namespace SipoDeck.Runtime.Events;

/// <summary>
/// Runtime, devam etmeyi engelleyen beklenmedik bir başlangıç hatasıyla karşılaştığında yayınlanır.
/// Bu durumda Runtime durumu <see cref="RuntimeState.Faulted"/> olur.
/// </summary>
public sealed class RuntimeFaultedEventArgs : EventArgs
{
    public RuntimeFaultedEventArgs(Exception exception) => Exception = exception;

    public Exception Exception { get; }
}
