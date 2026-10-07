namespace SipoDeck.Plugin;

/// <summary>
/// Bir plugin'in giriş noktası. Plugin DLL'i bu arayüzü uygulayan tam olarak bir somut sınıf içermelidir
/// (genel, parametresiz kurucu). Yaşam döngüsü: <see cref="StartAsync"/> → eylemler → <see cref="StopAsync"/>.
/// Hatalar uygulamayı ve diğer plugin'leri durdurmaz; başlatma, durdurma ve eylem çalıştırma zaman sınırına tabidir.
/// </summary>
public interface IPlugin
{
    /// <summary>Plugin'i başlatır. İptal veya zaman aşımında plugin başlamamış sayılır.</summary>
    Task StartAsync(IPluginContext context, CancellationToken cancellationToken);

    /// <summary>Plugin'i durdurur; kaynakları bırakmalıdır. İptal edilebilir ve zaman sınırlıdır.</summary>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>Plugin'in eylem kataloğuna eklediği eylemler. <see cref="StartAsync"/> sonrasında okunur.</summary>
    IReadOnlyList<PluginActionDescriptor> GetActions();

    /// <summary>
    /// Bir eylemi çalıştırır. Eylemler Runtime'ın eylem kuyruğunda sırayla çalışır; iptal isteğine saygı gösterilmelidir.
    /// Hata fırlatılırsa kuyruk bunu eylem hatası olarak raporlar ve devam eder.
    /// </summary>
    Task ExecuteActionAsync(PluginActionRequest request, CancellationToken cancellationToken);
}

public enum PluginParameterKind
{
    Text,
    LongText,
    Number,
    Choice,
    FilePath,
    KeyShortcut
}

/// <summary>Plugin eyleminin bir parametresinin tanımı. Uygulama bu tanımdan form üretir ve değeri doğrular.</summary>
public sealed class PluginParameterDescriptor
{
    public PluginParameterDescriptor(string name, string label, PluginParameterKind kind)
    {
        Name = name;
        Label = label;
        Kind = kind;
    }

    /// <summary><see cref="PluginActionRequest.Parameters"/> içindeki anahtar.</summary>
    public string Name { get; }

    public string Label { get; }

    public PluginParameterKind Kind { get; }

    public bool Required { get; init; }

    public string? DefaultValue { get; init; }

    /// <summary><see cref="PluginParameterKind.Choice"/> için seçenekler (değer, görünen ad).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Choices { get; init; } = Array.Empty<KeyValuePair<string, string>>();
}

/// <summary>Plugin'in kataloğa eklediği bir eylem türü. Tür kimliği uygulamada "pluginId/actionId" olarak benzersizleşir.</summary>
public sealed class PluginActionDescriptor
{
    public PluginActionDescriptor(string actionId, string displayName)
    {
        ActionId = actionId;
        DisplayName = displayName;
    }

    /// <summary>Plugin içinde benzersiz, değişmeyen kimlik (küçük harf, rakam, '-' ve '.').</summary>
    public string ActionId { get; }

    public string DisplayName { get; }

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = "Plugin";

    public IReadOnlyList<PluginParameterDescriptor> Parameters { get; init; } = Array.Empty<PluginParameterDescriptor>();
}

/// <summary>Çalıştırılacak eylem ve kullanıcının girdiği parametre değerleri (metin olarak saklanır).</summary>
public sealed class PluginActionRequest
{
    public PluginActionRequest(string actionId, IReadOnlyDictionary<string, string> parameters)
    {
        ActionId = actionId;
        Parameters = parameters;
    }

    public string ActionId { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }
}
