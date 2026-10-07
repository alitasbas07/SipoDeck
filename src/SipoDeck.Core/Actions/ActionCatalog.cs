namespace SipoDeck.Core.Actions;

public enum ActionParameterKind
{
    Text,
    LongText,
    Number,
    Choice,
    FilePath,
    KeyShortcut,
    /// <summary>Profil kimliği; arayüz mevcut profillerden seçtirir.</summary>
    ProfileId
}

/// <summary>Bir eylem parametresinin tanımı. Değerler <see cref="Get"/> / <see cref="Set"/> ile okunur ve yazılır.</summary>
public sealed class ActionParameterDefinition
{
    public ActionParameterDefinition(string name, string label, ActionParameterKind kind,
        Func<ActionConfig, object?> get, Action<ActionConfig, object?> set)
    {
        Name = name;
        Label = label;
        Kind = kind;
        Get = get;
        Set = set;
    }

    /// <summary>Config üzerindeki özellik adı; doğrulama hata yollarında kullanılır.</summary>
    public string Name { get; }

    public string Label { get; }

    public ActionParameterKind Kind { get; }

    public bool Required { get; init; }

    public object? DefaultValue { get; init; }

    /// <summary>Seçim türü için seçenekler (değer, görünen ad).</summary>
    public IReadOnlyList<(object Value, string Label)> Choices { get; init; } = Array.Empty<(object, string)>();

    /// <summary>Boş olmayan değer için ek kural; hata iletisi veya null döner.</summary>
    public Func<object?, string?>? Validate { get; init; }

    public string? RequiredMessage { get; init; }

    public Func<ActionConfig, object?> Get { get; }

    public Action<ActionConfig, object?> Set { get; }
}

/// <summary>Bir eylem türünün katalog tanımı.</summary>
public sealed class ActionDefinition
{
    public ActionDefinition(string typeId, string displayName, string description, string category,
        Type configType, Func<ActionConfig> create, IReadOnlyList<ActionParameterDefinition>? parameters = null)
    {
        TypeId = typeId;
        DisplayName = displayName;
        Description = description;
        Category = category;
        ConfigType = configType;
        Create = create;
        Parameters = parameters ?? Array.Empty<ActionParameterDefinition>();
    }

    /// <summary>JSON "type" değeri ile aynıdır.</summary>
    public string TypeId { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string Category { get; }

    public Type ConfigType { get; }

    public Func<ActionConfig> Create { get; }

    public IReadOnlyList<ActionParameterDefinition> Parameters { get; }

    /// <summary>Zincir gibi başka eylemler içeren tür; adımları makro editörüyle düzenlenir.</summary>
    public bool IsContainer { get; init; }

    /// <summary>Türe özgü, tek parametreye bölünemeyen kural; (özellik adı, ileti) döner.</summary>
    public Func<ActionConfig, (string Property, string Message)?>? ValidateExtra { get; init; }

    /// <summary>Kullanıcıya gösterilecek uyarı (örn. odaktaki uygulamaya girdi gönderir).</summary>
    public string? Warning { get; init; }
}

/// <summary>
/// Yerleşik eylem türlerinin kataloğu. Arayüz eylem seçicisini ve parametre formlarını buradan üretir;
/// doğrulama da aynı tanımları kullanır.
/// </summary>
public sealed class ActionCatalog
{
    public const int MaxChainSteps = 50;
    public const int MaxChainDepth = 5;

    public static ActionCatalog Default { get; } = CreateDefault();

    private readonly List<ActionDefinition> _definitions = new();

    public IReadOnlyList<ActionDefinition> Definitions => _definitions;

    public void Register(ActionDefinition definition)
    {
        if (_definitions.Any(d => d.TypeId == definition.TypeId))
            throw new InvalidOperationException($"'{definition.TypeId}' kimlikli eylem zaten kayıtlı.");
        _definitions.Add(definition);
    }

    public ActionDefinition? Find(string typeId) => _definitions.FirstOrDefault(d => d.TypeId == typeId);

    public ActionDefinition? Find(ActionConfig config) => _definitions.FirstOrDefault(d => d.ConfigType == config.GetType());

    /// <summary>Tek bir eylemin (zincir içeriği hariç) parametrelerini doğrular. Hata yoksa boş liste döner.</summary>
    public IReadOnlyList<(string Property, string Message)> ValidateParameters(ActionConfig config)
    {
        var errors = new List<(string, string)>();
        var definition = Find(config);
        if (definition is null)
        {
            errors.Add(("", "Bilinmeyen eylem türü."));
            return errors;
        }

        foreach (var p in definition.Parameters)
        {
            var value = p.Get(config);
            var isEmpty = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
            if (isEmpty)
            {
                if (p.Required)
                    errors.Add((p.Name, p.RequiredMessage ?? $"{p.Label} boş olamaz."));
                continue;
            }

            var message = p.Validate?.Invoke(value);
            if (message is not null)
                errors.Add((p.Name, message));
        }

        if (definition.ValidateExtra?.Invoke(config) is { } extra)
            errors.Add(extra);

        return errors;
    }

    private static ActionCatalog CreateDefault()
    {
        var c = new ActionCatalog();

        c.Register(new ActionDefinition("runProgram", "Program çalıştır", "Bir programı başlatır.", "Uygulama",
            typeof(RunProgramActionConfig), () => new RunProgramActionConfig(), new[]
            {
                Text<RunProgramActionConfig>("FilePath", "Program yolu", ActionParameterKind.FilePath, a => a.FilePath, (a, v) => a.FilePath = v ?? string.Empty, true, "Program yolu boş olamaz."),
                Text<RunProgramActionConfig>("Arguments", "Argümanlar", ActionParameterKind.Text, a => a.Arguments, (a, v) => a.Arguments = v),
                Text<RunProgramActionConfig>("WorkingDirectory", "Çalışma klasörü", ActionParameterKind.FilePath, a => a.WorkingDirectory, (a, v) => a.WorkingDirectory = v)
            }));

        c.Register(new ActionDefinition("openUrl", "URL aç", "Adresi varsayılan tarayıcıda açar.", "Uygulama",
            typeof(OpenUrlActionConfig), () => new OpenUrlActionConfig(), new[]
            {
                Text<OpenUrlActionConfig>("Url", "Adres", ActionParameterKind.Text, a => a.Url, (a, v) => a.Url = v ?? string.Empty, true, "Adres boş olamaz.")
            }));

        c.Register(new ActionDefinition("openPath", "Dosya/klasör aç", "Bir dosya veya klasörü açar.", "Uygulama",
            typeof(OpenPathActionConfig), () => new OpenPathActionConfig(), new[]
            {
                Text<OpenPathActionConfig>("Path", "Yol", ActionParameterKind.FilePath, a => a.Path, (a, v) => a.Path = v ?? string.Empty, true, "Yol boş olamaz.")
            }));

        c.Register(new ActionDefinition("wait", "Bekle", "Sıradaki eyleme geçmeden önce bekler.", "Akış",
            typeof(WaitActionConfig), () => new WaitActionConfig { Milliseconds = 500 }, new[]
            {
                new ActionParameterDefinition("Milliseconds", "Süre (ms)", ActionParameterKind.Number,
                    a => ((WaitActionConfig)a).Milliseconds, (a, v) => ((WaitActionConfig)a).Milliseconds = ToInt(v))
                {
                    Required = true,
                    DefaultValue = 500,
                    Validate = v => ToInt(v) < 0 ? "Bekleme süresi negatif olamaz." : null
                }
            }));

        c.Register(new ActionDefinition("notification", "Bildirim", "Windows bildirimi gösterir.", "Sistem",
            typeof(NotificationActionConfig), () => new NotificationActionConfig { Title = "SipoDeck" }, new[]
            {
                Text<NotificationActionConfig>("Title", "Başlık", ActionParameterKind.Text, a => a.Title, (a, v) => a.Title = v ?? string.Empty),
                Text<NotificationActionConfig>("Message", "Mesaj", ActionParameterKind.LongText, a => a.Message, (a, v) => a.Message = v ?? string.Empty)
            })
        {
            ValidateExtra = a => a is NotificationActionConfig n && string.IsNullOrWhiteSpace(n.Title) && string.IsNullOrWhiteSpace(n.Message)
                ? ("Message", "Bildirim başlığı veya mesajı girilmelidir.")
                : null
        });

        c.Register(new ActionDefinition("volume", "Ses", "Sistem sesini artırır, azaltır veya kapatır.", "Medya",
            typeof(VolumeActionConfig), () => new VolumeActionConfig(), new[]
            {
                Choice<VolumeActionConfig, VolumeCommand>("Command", "Komut", a => a.Command, (a, v) => a.Command = v,
                    "Geçersiz ses komutu.", (VolumeCommand.Up, "Sesi artır"), (VolumeCommand.Down, "Sesi azalt"), (VolumeCommand.Mute, "Sessize al"))
            }));

        c.Register(new ActionDefinition("media", "Medya", "Medya oynatıcıyı kontrol eder.", "Medya",
            typeof(MediaActionConfig), () => new MediaActionConfig(), new[]
            {
                Choice<MediaActionConfig, MediaCommand>("Command", "Komut", a => a.Command, (a, v) => a.Command = v,
                    "Geçersiz medya komutu.", (MediaCommand.PlayPause, "Oynat/Duraklat"), (MediaCommand.Next, "Sonraki"),
                    (MediaCommand.Previous, "Önceki"), (MediaCommand.Stop, "Durdur"))
            }));

        c.Register(new ActionDefinition("keyboardShortcut", "Klavye kısayolu gönder", "Kısayolu odaktaki uygulamaya gönderir (örn. Ctrl+Shift+M).", "Sistem",
            typeof(KeyboardShortcutActionConfig), () => new KeyboardShortcutActionConfig(), new[]
            {
                new ActionParameterDefinition("Shortcut", "Kısayol", ActionParameterKind.KeyShortcut,
                    a => ((KeyboardShortcutActionConfig)a).Shortcut, (a, v) => ((KeyboardShortcutActionConfig)a).Shortcut = v as string ?? string.Empty)
                {
                    Required = true,
                    RequiredMessage = "Kısayol boş olamaz.",
                    Validate = v => KeyShortcut.TryParse(v as string, out _, out var error) ? null : error
                }
            })
        { Warning = "Bu eylem odaktaki uygulamaya klavye girdisi gönderir." });

        c.Register(new ActionDefinition("typeText", "Metin yaz", "Metni odaktaki uygulamaya yazar.", "Sistem",
            typeof(TypeTextActionConfig), () => new TypeTextActionConfig(), new[]
            {
                Text<TypeTextActionConfig>("Text", "Metin", ActionParameterKind.LongText, a => a.Text, (a, v) => a.Text = v ?? string.Empty, true, "Metin boş olamaz.")
            })
        { Warning = "Bu eylem odaktaki uygulamaya klavye girdisi gönderir." });

        c.Register(new ActionDefinition("switchProfile", "Profil değiştir", "Aktif profili değiştirir.", "Akış",
            typeof(SwitchProfileActionConfig), () => new SwitchProfileActionConfig(), new[]
            {
                Text<SwitchProfileActionConfig>("ProfileId", "Profil", ActionParameterKind.ProfileId, a => a.ProfileId, (a, v) => a.ProfileId = v ?? string.Empty, true, "Profil seçilmelidir.")
            }));

        c.Register(new ActionDefinition("chain", "Zincir (makro)", "Eylemleri sırayla çalıştırır.", "Akış",
            typeof(ActionChainConfig), () => new ActionChainConfig())
        { IsContainer = true });

        return c;
    }

    private static int ToInt(object? v) => v switch
    {
        int i => i,
        long l => (int)l,
        double d => (int)d,
        string s when int.TryParse(s, out var n) => n,
        _ => 0
    };

    private static ActionParameterDefinition Text<T>(string name, string label, ActionParameterKind kind,
        Func<T, string?> get, Action<T, string?> set, bool required = false, string? requiredMessage = null)
        where T : ActionConfig
        => new(name, label, kind, a => get((T)a), (a, v) => set((T)a, v as string))
        {
            Required = required,
            RequiredMessage = requiredMessage
        };

    private static ActionParameterDefinition Choice<T, TEnum>(string name, string label,
        Func<T, TEnum> get, Action<T, TEnum> set, string invalidMessage, params (TEnum Value, string Label)[] choices)
        where T : ActionConfig
        where TEnum : struct, Enum
        => new(name, label, ActionParameterKind.Choice, a => get((T)a), (a, v) => set((T)a, v is TEnum e ? e : default))
        {
            Required = true,
            DefaultValue = choices[0].Value,
            Choices = choices.Select(c => ((object)c.Value, c.Label)).ToArray(),
            Validate = v => v is TEnum e && Enum.IsDefined(e) ? null : invalidMessage
        };
}
