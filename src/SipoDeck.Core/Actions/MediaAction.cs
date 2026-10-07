using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Medya kontrolü komutları.
/// </summary>
public enum MediaCommand
{
    PlayPause,
    Next,
    Previous,
    Stop
}

/// <summary>
/// Bir medya kontrolü eyleminin temel yapısıdır.
/// Gerçek davranış <see cref="ActionServices.Platform"/> üzerinden platform katmanında uygulanır.
/// </summary>
public sealed class MediaAction : IAction
{
    public MediaAction(MediaCommand command) => Command = command;

    public MediaCommand Command { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => ActionServices.Platform.SendMediaAsync(Command, cancellationToken);
}
