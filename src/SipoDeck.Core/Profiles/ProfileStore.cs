using System.Text.Json;
using System.Text.Json.Serialization;
using SipoDeck.Core.Storage;

namespace SipoDeck.Core.Profiles;

/// <summary>
/// Profilleri JSON olarak yükler ve saklar (profiles.json). Ayarlardan ayrı tutulur.
/// Dosya yoksa veya bozuksa varsayılan profil döner. Veri uygulama yeniden başlatıldığında korunur.
/// </summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly bool _usesDefaultPath;

    public ProfileStore(string? filePath = null)
    {
        _usesDefaultPath = filePath is null;
        _filePath = filePath ?? AppDataPaths.ProfilesFile;
    }

    public ProfilesData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return ProfilesData.CreateDefault();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<ProfilesData>(json, Options) ?? ProfilesData.CreateDefault();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return ProfilesData.CreateDefault();
        }
    }

    public void Save(ProfilesData data)
    {
        if (_usesDefaultPath)
            AppDataPaths.EnsureRootExists();

        var json = JsonSerializer.Serialize(data, Options);
        AtomicFile.WriteAllText(_filePath, json);
    }
}
