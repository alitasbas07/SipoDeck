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
/// Gerçek medya kontrolü ileriki bir task'ta / Windows katmanında uygulanacaktır.
/// </summary>
public sealed class MediaAction : IAction
{
    public MediaAction(MediaCommand command) => Command = command;

    public MediaCommand Command { get; }

    public void Execute()
    {
        // Temel yapı: gerçek medya kontrolü henüz uygulanmadı.
    }
}
