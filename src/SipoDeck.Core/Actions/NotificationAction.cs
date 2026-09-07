namespace SipoDeck.Core.Actions;

/// <summary>
/// Bir bildirim gösterme eyleminin temel yapısıdır.
/// Gerçek işletim sistemi bildirimi (toast) ileriki bir task'ta / Windows katmanında uygulanacaktır.
/// </summary>
public sealed class NotificationAction : IAction
{
    public NotificationAction(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }

    public void Execute()
    {
        // Temel yapı: gerçek bildirim gösterimi henüz uygulanmadı.
    }
}
