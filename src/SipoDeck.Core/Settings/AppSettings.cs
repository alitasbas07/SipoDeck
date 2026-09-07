namespace SipoDeck.Core.Settings;

/// <summary>
/// Uygulamanın merkezi ayar modeli. Profil verilerini içermez; profiller ayrı saklanır.
/// </summary>
public sealed class AppSettings
{
    public ConnectionSettings Connection { get; set; } = new();

    public InputSettings Input { get; set; } = new();

    public ApplicationSettings Application { get; set; } = new();
}
