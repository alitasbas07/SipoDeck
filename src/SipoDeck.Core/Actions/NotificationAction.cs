using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Bir bildirim gösterme eyleminin temel yapısıdır.
/// Gerçek bildirim <see cref="ActionServices.Platform"/> üzerinden platform katmanında gösterilir.
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

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => ActionServices.Platform.ShowNotificationAsync(Title, Message, cancellationToken);
}
