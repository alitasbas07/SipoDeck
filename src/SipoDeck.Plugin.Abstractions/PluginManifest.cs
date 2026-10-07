using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SipoDeck.Plugin;

/// <summary>Plugin tanım dosyası (<c>plugin.json</c>) modeli.</summary>
public sealed class PluginManifest
{
    public const string FileName = "plugin.json";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("apiVersion")]
    public int ApiVersion { get; set; }

    /// <summary>Plugin klasörüne göre göreli giriş DLL'i.</summary>
    [JsonPropertyName("entry")]
    public string Entry { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
}

public sealed record PluginManifestError(string Field, string Message);

public sealed class PluginManifestParseResult
{
    private PluginManifestParseResult(PluginManifest? manifest, IReadOnlyList<PluginManifestError> errors)
    {
        Manifest = manifest;
        Errors = errors;
    }

    public PluginManifest? Manifest { get; }

    public IReadOnlyList<PluginManifestError> Errors { get; }

    public bool IsValid => Manifest is not null && Errors.Count == 0;

    internal static PluginManifestParseResult Success(PluginManifest manifest)
        => new(manifest, Array.Empty<PluginManifestError>());

    internal static PluginManifestParseResult Failure(IReadOnlyList<PluginManifestError> errors, PluginManifest? manifest = null)
        => new(manifest, errors);
}

/// <summary>Manifest çözümleme ve doğrulama (yükleyiciden bağımsız).</summary>
public static partial class PluginManifestParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Küçük harf, rakam, '-' ve '_' ile '.' ile ayrılmış bölümler: "ornek.plugin".
    [GeneratedRegex(@"^[a-z0-9][a-z0-9_-]*(\.[a-z0-9][a-z0-9_-]*)*$")]
    private static partial Regex IdPattern();

    // Anlamsal sürüm: 1.0.0 veya 1.0.0-beta.1
    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionPattern();

    public const int MaxIdLength = 64;

    public static PluginManifestParseResult Parse(string json, int supportedApiVersion = PluginApi.Current)
    {
        PluginManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PluginManifest>(json, Options);
        }
        catch (JsonException ex)
        {
            return PluginManifestParseResult.Failure(new[] { new PluginManifestError("", $"Manifest okunamadı: {ex.Message}") });
        }

        if (manifest is null)
            return PluginManifestParseResult.Failure(new[] { new PluginManifestError("", "Manifest boş.") });

        var errors = Validate(manifest, supportedApiVersion);
        return errors.Count == 0
            ? PluginManifestParseResult.Success(manifest)
            : PluginManifestParseResult.Failure(errors, manifest);
    }

    public static IReadOnlyList<PluginManifestError> Validate(PluginManifest manifest, int supportedApiVersion = PluginApi.Current)
    {
        var errors = new List<PluginManifestError>();

        if (string.IsNullOrWhiteSpace(manifest.Id))
            errors.Add(new("id", "Plugin kimliği boş olamaz."));
        else if (manifest.Id.Length > MaxIdLength || !IdPattern().IsMatch(manifest.Id))
            errors.Add(new("id", "Plugin kimliği küçük harf, rakam, '-', '_' ve '.' içermeli ve en fazla 64 karakter olmalıdır (örn. ornek.plugin)."));

        if (string.IsNullOrWhiteSpace(manifest.Name))
            errors.Add(new("name", "Plugin adı boş olamaz."));

        if (string.IsNullOrWhiteSpace(manifest.Version))
            errors.Add(new("version", "Sürüm boş olamaz."));
        else if (!VersionPattern().IsMatch(manifest.Version))
            errors.Add(new("version", "Sürüm 1.0.0 biçiminde olmalıdır."));

        if (manifest.ApiVersion != supportedApiVersion)
            errors.Add(new("apiVersion", $"Desteklenmeyen API sürümü: {manifest.ApiVersion}. Uygulama sürüm {supportedApiVersion} destekler."));

        ValidateEntry(manifest.Entry, errors);

        var seen = new HashSet<string>();
        foreach (var permission in manifest.Permissions ?? new List<string>())
        {
            if (permission is null || !PluginPermissions.IsKnown(permission))
                errors.Add(new("permissions", $"Bilinmeyen yetki: {permission}"));
            else if (!seen.Add(permission))
                errors.Add(new("permissions", $"Yetki birden fazla kez beyan edilmiş: {permission}"));
        }

        return errors;
    }

    private static void ValidateEntry(string entry, List<PluginManifestError> errors)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            errors.Add(new("entry", "Giriş dosyası boş olamaz."));
            return;
        }

        if (!entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            errors.Add(new("entry", "Giriş dosyası bir .dll olmalıdır."));

        // Plugin klasörü dışına çıkışı engelle: mutlak yol, sürücü, '..' yok.
        var parts = entry.Split('/', '\\');
        if (Path.IsPathRooted(entry) || entry.Contains(':') || parts.Any(p => p == ".." || p == "."))
            errors.Add(new("entry", "Giriş dosyası plugin klasörüne göreli olmalı ve klasör dışına çıkmamalıdır."));
    }

    /// <summary>
    /// Güncellemede yeni onay gerekip gerekmediğini belirler: yeni sürüm, daha önce onaylanmamış bir yetki istiyorsa true.
    /// </summary>
    public static bool RequiresReapproval(IEnumerable<string> approvedPermissions, PluginManifest updated)
        => updated.Permissions.Except(approvedPermissions).Any();
}
