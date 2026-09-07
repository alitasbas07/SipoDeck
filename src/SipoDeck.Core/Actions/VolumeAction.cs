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
/// Gerçek ses kontrolü ileriki bir task'ta / Windows katmanında uygulanacaktır.
/// </summary>
public sealed class VolumeAction : IAction
{
    public VolumeAction(VolumeCommand command) => Command = command;

    public VolumeCommand Command { get; }

    public void Execute()
    {
        // Temel yapı: gerçek ses kontrolü henüz uygulanmadı.
    }
}
