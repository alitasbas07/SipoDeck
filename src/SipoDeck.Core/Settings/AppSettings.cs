using System.Text.Json;
using System.Text.Json.Serialization;

namespace SipoDeck.Core.Settings;

/// <summary>
/// Uygulamanın merkezi ayar modeli. Profil verilerini içermez; profiller ayrı saklanır.
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ConnectionSettings Connection { get; set; } = new();

    public InputSettings Input { get; set; } = new();

    public ApplicationSettings Application { get; set; } = new();

    /// <summary>Ayarların bağımsız derin kopyasını döndürür (JSON gidiş-dönüşü).</summary>
    public AppSettings Clone()
        => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this, CloneOptions), CloneOptions) ?? new AppSettings();
}
