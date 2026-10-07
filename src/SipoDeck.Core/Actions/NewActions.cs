using System.Diagnostics;

namespace SipoDeck.Core.Actions;

/// <summary>Klavye kısayolunu odaktaki uygulamaya gönderir.</summary>
public sealed class KeyboardShortcutAction : IAction
{
    public KeyboardShortcutAction(string shortcut) => Shortcut = shortcut;

    public string Shortcut { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!KeyShortcut.TryParse(Shortcut, out var parsed, out var error))
            throw new InvalidOperationException(error);
        return ActionServices.Platform.SendShortcutAsync(parsed, cancellationToken);
    }
}

/// <summary>Metni odaktaki uygulamaya yazar.</summary>
public sealed class TypeTextAction : IAction
{
    public TypeTextAction(string text) => Text = text;

    public string Text { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => ActionServices.Platform.TypeTextAsync(Text, cancellationToken);
}

/// <summary>Kullanıcının girdiği dosya veya klasörü ilişkili uygulamayla / Gezgin'de açar.</summary>
public sealed class OpenPathAction : IAction
{
    public OpenPathAction(string path) => Path = path;

    public string Path { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Process.Start(new ProcessStartInfo(Path) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}

/// <summary>Aktif profili değiştirir.</summary>
public sealed class SwitchProfileAction : IAction
{
    public SwitchProfileAction(string profileId) => ProfileId = profileId;

    public string ProfileId { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var switcher = ActionServices.ProfileSwitcher
            ?? throw new InvalidOperationException("Profil değiştirme servisi hazır değil.");
        return switcher.SwitchProfileAsync(ProfileId, cancellationToken);
    }
}
