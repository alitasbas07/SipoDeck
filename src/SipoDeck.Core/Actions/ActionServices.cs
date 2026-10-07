namespace SipoDeck.Core.Actions;

/// <summary>
/// İşletim sistemine bağlı eylem davranışlarının (ses, medya, bildirim, klavye) platform uygulaması.
/// Core platformdan bağımsız kalır; Windows uygulaması SipoDeck.Platform.Windows'ta bulunur.
/// </summary>
public interface IPlatformActions
{
    Task SetVolumeAsync(VolumeCommand command, CancellationToken cancellationToken = default);

    Task SendMediaAsync(MediaCommand command, CancellationToken cancellationToken = default);

    Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>Bir klavye kısayolunu (bkz. <see cref="KeyShortcut"/>) odaktaki uygulamaya gönderir.</summary>
    Task SendShortcutAsync(KeyShortcut shortcut, CancellationToken cancellationToken = default);

    /// <summary>Metni odaktaki uygulamaya yazar.</summary>
    Task TypeTextAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>Aktif profili değiştirebilen bileşen (Runtime tarafından sağlanır).</summary>
public interface IProfileSwitcher
{
    Task SwitchProfileAsync(string profileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Eylemlerin çalışma anında ihtiyaç duyduğu servislerin tutulduğu yer. Uygulama başlangıçta doldurur;
/// doldurulmamışsa platform eylemleri hiçbir şey yapmaz (testler ve platform dışı kullanım için güvenlidir).
/// </summary>
public static class ActionServices
{
    private static volatile IPlatformActions _platform = NoPlatformActions.Instance;
    private static volatile IProfileSwitcher? _profileSwitcher;

    public static IPlatformActions Platform
    {
        get => _platform;
        set => _platform = value ?? NoPlatformActions.Instance;
    }

    public static IProfileSwitcher? ProfileSwitcher
    {
        get => _profileSwitcher;
        set => _profileSwitcher = value;
    }

    private sealed class NoPlatformActions : IPlatformActions
    {
        public static readonly NoPlatformActions Instance = new();
        public Task SetVolumeAsync(VolumeCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendMediaAsync(MediaCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendShortcutAsync(KeyShortcut shortcut, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task TypeTextAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
