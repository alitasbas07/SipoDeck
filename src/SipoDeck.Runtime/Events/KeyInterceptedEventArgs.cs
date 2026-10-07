namespace SipoDeck.Runtime.Events;

public sealed class KeyInterceptedEventArgs : EventArgs
{
    public int Key { get; }
    public bool IsPressed { get; }

    public KeyInterceptedEventArgs(int key, bool isPressed)
    {
        Key = key;
        IsPressed = isPressed;
    }
}