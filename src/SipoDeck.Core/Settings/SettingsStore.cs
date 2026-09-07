using System.Text.Json;
using System.Text.Json.Serialization;
using SipoDeck.Core.Storage;

namespace SipoDeck.Core.Settings;

/// <summary>
/// Uygulama ayarlarını JSON olarak yükler ve saklar. Ayarlar uygulama yeniden
/// başlatıldığında korunur. Bozuk veya eksik dosyada varsayılan ayarlar döner.
/// Hassas bilgiler loglanmaz.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
        => _filePath = filePath ?? AppDataPaths.SettingsFile;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AppSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        AppDataPaths.EnsureRootExists();
        var json = JsonSerializer.Serialize(settings, Options);
        File.WriteAllText(_filePath, json);
    }
}
