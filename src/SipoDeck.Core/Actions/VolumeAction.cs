using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Ses kontrolü komutları.
/// </summary>
public enum VolumeCommand
{
    Up,
    Down,
    Mute
}

/// <summary>
/// Bir ses kontrolü eyleminin temel yapısıdır.
/// Gerçek davranış <see cref="ActionServices.Platform"/> üzerinden platform katmanında uygulanır.
/// </summary>
public sealed class VolumeAction : IAction
{
    public VolumeAction(VolumeCommand command) => Command = command;

    public VolumeCommand Command { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => ActionServices.Platform.SetVolumeAsync(Command, cancellationToken);
}
