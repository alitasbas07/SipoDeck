namespace SipoDeck.Core.Storage;

/// <summary>
/// Uygulama verilerinin saklandığı klasör ve dosya yollarını tek merkezde tanımlar.
/// Veriler makineye özel olarak %LOCALAPPDATA%\SipoDeck altında tutulur.
/// </summary>
public static class AppDataPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SipoDeck");

    public static string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public static string ProfilesFile => Path.Combine(RootDirectory, "profiles.json");

    public static void EnsureRootExists() => Directory.CreateDirectory(RootDirectory);
}
