using System.Text.Json;
using System.Text.Json.Serialization;

namespace SipoDeck.Core.Profiles;

/// <summary>
/// Saklanan tüm profillerin kök yapısıdır. Aynı anda yalnızca bir profil aktif olabilir;
/// pasif profiller saklanmaya devam eder. Version alanı ileride veri göçü için kullanılır.
/// </summary>
public sealed class ProfilesData
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public int Version { get; set; } = 1;

    public string? ActiveProfileId { get; set; }

    public List<ProfileData> Profiles { get; set; } = new();

    /// <summary>Verinin bağımsız derin kopyasını döndürür (JSON gidiş-dönüşü; eylemler dahil).</summary>
    public ProfilesData Clone()
        => JsonSerializer.Deserialize<ProfilesData>(JsonSerializer.Serialize(this, CloneOptions), CloneOptions) ?? new ProfilesData();

    /// <summary>
    /// İlk çalıştırmada sunulacak, kullanıcı tarafından değiştirilebilen/silinebilen
    /// kullanılabilir bir varsayılan profil üretir.
    /// </summary>
    public static ProfilesData CreateDefault()
    {
        var defaultProfile = new ProfileData
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Varsayılan Profil",
            IsEnabled = true
        };

        return new ProfilesData
        {
            Version = 1,
            ActiveProfileId = defaultProfile.Id,
            Profiles = { defaultProfile }
        };
    }
}
