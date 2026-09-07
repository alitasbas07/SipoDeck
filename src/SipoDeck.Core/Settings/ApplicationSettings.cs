namespace SipoDeck.Core.Settings;

/// <summary>
/// Uygulama ayarları: Windows başlangıcında çalışma, sistem tepsisinde çalışma,
/// otomatik yeniden bağlanma.
/// </summary>
public sealed class ApplicationSettings
{
    public bool RunAtStartup { get; set; } = SettingsDefaults.RunAtStartup;

    public bool RunInSystemTray { get; set; } = SettingsDefaults.RunInSystemTray;

    public bool AutoReconnect { get; set; } = SettingsDefaults.AutoReconnect;
}
