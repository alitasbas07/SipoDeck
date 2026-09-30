namespace SipoDeck.Runtime.Tests.Helpers;

/// <summary>Testin ömrü boyunca var olan, sonunda silinen geçici klasör (gerçek %LOCALAPPDATA% kullanılmaz).</summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SipoDeckRuntimeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string SettingsFile => System.IO.Path.Combine(Path, "settings.json");

    public string ProfilesFile => System.IO.Path.Combine(Path, "profiles.json");

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
