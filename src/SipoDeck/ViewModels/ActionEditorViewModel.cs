using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Actions;

namespace SipoDeck.ViewModels;

/// <summary>Eylem türü seçicisindeki bir satır. <see cref="TypeId"/> null ise "(Yok)" seçeneğidir.</summary>
public sealed record ActionTypeOption(string? TypeId, string Name, string Category)
{
    public string Display => TypeId is null ? Name : $"{Category} › {Name}";
}

/// <summary>
/// Katalog tabanlı eylem editörü: tür seçimi, türün parametre formu ve zincir türü için makro adım listesi.
/// Her değişiklikte yeni bir <see cref="Config"/> nesnesi üretilir ve <see cref="Changed"/> tetiklenir.
/// </summary>
public sealed partial class ActionEditorViewModel : ObservableObject
{
    private readonly ActionCatalog _catalog;
    private readonly Func<IReadOnlyList<ProfileOption>> _profiles;
    private readonly ActionEditorViewModel? _parent;
    private readonly bool _allowNone;
    private readonly ActionTypeOption _noneOption = new(null, "(Yok)", string.Empty);
    private ActionConfig? _config;
    private bool _loading;

    public ActionEditorViewModel(ActionCatalog catalog, Func<IReadOnlyList<ProfileOption>> profiles)
        : this(catalog, profiles, null, true)
    {
    }

    private ActionEditorViewModel(ActionCatalog catalog, Func<IReadOnlyList<ProfileOption>> profiles,
        ActionEditorViewModel? parent, bool allowNone)
    {
        _catalog = catalog;
        _profiles = profiles;
        _parent = parent;
        _allowNone = allowNone;
        Depth = parent is null ? 0 : parent.Depth + 1;

        var options = new List<ActionTypeOption>();
        if (allowNone) options.Add(_noneOption);
        foreach (var d in catalog.Definitions)
        {
            // Zincir, derinlik sınırına ulaşıldıysa seçilemez.
            if (d.IsContainer && Depth >= ActionCatalog.MaxChainDepth) continue;
            options.Add(new ActionTypeOption(d.TypeId, d.DisplayName, d.Category));
        }
        TypeOptions = options;
        StepTypeOptions = options.Where(o => o.TypeId is not null).ToList();
        _selectedType = allowNone ? _noneOption : options[0];
        NewStepType = options.FirstOrDefault(o => o.TypeId == "wait") ?? options.FirstOrDefault(o => o.TypeId is not null);

        AddStepCommand = new RelayCommand(() => AddStep(NewStepType?.TypeId));
        AddWaitCommand = new RelayCommand(() => AddStep("wait"));
        RemoveStepCommand = new RelayCommand<ActionEditorViewModel>(RemoveStep);
        DuplicateStepCommand = new RelayCommand<ActionEditorViewModel>(DuplicateStep);
        MoveStepUpCommand = new RelayCommand<ActionEditorViewModel>(s => MoveStep(s, -1));
        MoveStepDownCommand = new RelayCommand<ActionEditorViewModel>(s => MoveStep(s, 1));
    }

    /// <summary>Zincir içindeki iç içelik seviyesi; en üstteki editör 0'dır.</summary>
    public int Depth { get; }

    public IReadOnlyList<ActionTypeOption> TypeOptions { get; }

    /// <summary>"Adım ekle" seçicisindeki türler ("(Yok)" hariç).</summary>
    public IReadOnlyList<ActionTypeOption> StepTypeOptions { get; }

    public ObservableCollection<ActionParameterViewModel> Parameters { get; } = new();

    /// <summary>Bu editör zincir türündeyken adımları taşır; her adım kendi editörüdür.</summary>
    public ObservableCollection<ActionEditorViewModel> Steps { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChain), nameof(Warning), nameof(StepTitle), nameof(HasParameters))]
    private ActionTypeOption _selectedType;

    [ObservableProperty]
    private ActionTypeOption? _newStepType;

    [ObservableProperty]
    private int _stepNumber;

    [ObservableProperty]
    private string? _validationSummary;

    [ObservableProperty]
    private string? _stepLimitMessage;

    public bool IsChain => _catalog.Find(SelectedType.TypeId ?? string.Empty)?.IsContainer == true;
    public bool HasParameters => Parameters.Count > 0;
    public string? Warning => _catalog.Find(SelectedType.TypeId ?? string.Empty)?.Warning;
    public string StepTitle => $"{StepNumber}. {SelectedType.Name}";

    public string ChainSummary => _config is ActionChainConfig chain
        ? $"Toplam adım: {CountSteps(chain)} / {ActionCatalog.MaxChainSteps - 1}  ·  Tahmini bekleme: {TotalWaitMilliseconds(chain) / 1000.0:0.##} sn"
        : string.Empty;

    public IRelayCommand AddStepCommand { get; }
    public IRelayCommand AddWaitCommand { get; }
    public IRelayCommand<ActionEditorViewModel> RemoveStepCommand { get; }
    public IRelayCommand<ActionEditorViewModel> DuplicateStepCommand { get; }
    public IRelayCommand<ActionEditorViewModel> MoveStepUpCommand { get; }
    public IRelayCommand<ActionEditorViewModel> MoveStepDownCommand { get; }

    /// <summary>Kullanıcı bir alanı değiştirdiğinde tetiklenir; yeni yapılandırma <see cref="Config"/> üzerindedir.</summary>
    public event EventHandler? Changed;

    public ActionConfig? Config => _config;

    public void Load(ActionConfig? config)
    {
        _loading = true;
        _config = config;
        var definition = config is null ? null : _catalog.Find(config);
        SelectedType = (definition is null ? null : TypeOptions.FirstOrDefault(o => o.TypeId == definition.TypeId))
            ?? (_allowNone ? _noneOption : TypeOptions[0]);
        RebuildParameters(definition);
        RebuildSteps(config as ActionChainConfig);
        StepLimitMessage = null;
        Revalidate();
        OnPropertyChanged(nameof(ChainSummary));
        _loading = false;
    }

    partial void OnSelectedTypeChanged(ActionTypeOption value)
    {
        if (_loading) return;

        var definition = value.TypeId is null ? null : _catalog.Find(value.TypeId);
        _config = definition?.Create();
        RebuildParameters(definition);
        RebuildSteps(null);
        StepLimitMessage = null;
        Revalidate();
        OnPropertyChanged(nameof(ChainSummary));
        RaiseChanged();
    }

    partial void OnStepNumberChanged(int value) => OnPropertyChanged(nameof(StepTitle));

    /// <summary>Bir parametre değiştiğinde çağrılır: yapılandırmanın kopyasını güncelleyip değişikliği bildirir.</summary>
    internal void SetParameter(ActionParameterViewModel parameter, object? value)
    {
        if (_loading || _config is null) return;

        var copy = Clone(_config);
        parameter.Definition.Set(copy, value);
        _config = copy;
        Revalidate();
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        OnPropertyChanged(nameof(Config));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildParameters(ActionDefinition? definition)
    {
        Parameters.Clear();
        if (definition is not null && _config is not null)
        {
            var profiles = definition.Parameters.Any(p => p.Kind == ActionParameterKind.ProfileId)
                ? _profiles()
                : Array.Empty<ProfileOption>();
            foreach (var p in definition.Parameters)
                Parameters.Add(new ActionParameterViewModel(this, p, p.Get(_config), profiles));
        }
        OnPropertyChanged(nameof(HasParameters));
    }

    private void Revalidate()
    {
        if (_config is null)
        {
            ValidationSummary = null;
            return;
        }

        var errors = _catalog.ValidateParameters(_config);
        foreach (var p in Parameters)
            p.SetCatalogError(errors.FirstOrDefault(e => e.Property == p.Name).Message);

        var general = errors.Where(e => Parameters.All(p => p.Name != e.Property)).Select(e => e.Message).ToList();
        if (_config is ActionChainConfig chain)
        {
            // Çekirdek doğrulama, en üstteki zincirin kendisini de adım sayar.
            if (_parent is null && CountSteps(chain) + 1 > ActionCatalog.MaxChainSteps)
                general.Add($"Zincirler toplamda en fazla {ActionCatalog.MaxChainSteps} adım içerebilir.");
        }
        ValidationSummary = general.Count == 0 ? null : string.Join(Environment.NewLine, general);
    }

    // ---- Makro (zincir) adımları ----

    private ActionEditorViewModel Root => _parent is null ? this : _parent.Root;

    private int RootTotalSteps => Root._config is ActionChainConfig c ? CountSteps(c) : 0;

    private void RebuildSteps(ActionChainConfig? chain)
    {
        foreach (var s in Steps) s.Changed -= OnStepChanged;
        Steps.Clear();
        if (chain is not null)
            foreach (var action in chain.Actions)
                Steps.Add(CreateStep(action));
        Renumber();
    }

    private ActionEditorViewModel CreateStep(ActionConfig config)
    {
        var step = new ActionEditorViewModel(_catalog, _profiles, this, allowNone: false);
        step.Load(config);
        step.Changed += OnStepChanged;
        return step;
    }

    private void Renumber()
    {
        for (var i = 0; i < Steps.Count; i++) Steps[i].StepNumber = i + 1;
    }

    private void OnStepChanged(object? sender, EventArgs e) => ChainChanged();

    private void ChainChanged()
    {
        _config = new ActionChainConfig
        {
            Actions = Steps.Select(s => s.Config).OfType<ActionConfig>().ToList()
        };
        Renumber();
        StepLimitMessage = null;
        Revalidate();
        OnPropertyChanged(nameof(ChainSummary));
        RaiseChanged();
    }

    private bool CanAddSteps(int count)
    {
        if (RootTotalSteps + 1 + count > ActionCatalog.MaxChainSteps)
        {
            StepLimitMessage = $"Adım sınırı aşılıyor: zincirler toplamda en fazla {ActionCatalog.MaxChainSteps} adım içerebilir.";
            return false;
        }
        return true;
    }

    private void AddStep(string? typeId)
    {
        var definition = typeId is null ? null : _catalog.Find(typeId);
        if (definition is null || !IsChain || !CanAddSteps(1)) return;

        var step = CreateStep(definition.Create());
        Steps.Add(step);
        ChainChanged();
    }

    private void RemoveStep(ActionEditorViewModel? step)
    {
        if (step is null || !Steps.Remove(step)) return;
        step.Changed -= OnStepChanged;
        ChainChanged();
    }

    private void DuplicateStep(ActionEditorViewModel? step)
    {
        if (step?.Config is null) return;
        var index = Steps.IndexOf(step);
        if (index < 0) return;

        var copy = Clone(step.Config);
        var size = copy is ActionChainConfig c ? 1 + CountSteps(c) : 1;
        if (!CanAddSteps(size)) return;

        Steps.Insert(index + 1, CreateStep(copy));
        ChainChanged();
    }

    private void MoveStep(ActionEditorViewModel? step, int offset)
    {
        if (step is null) return;
        var index = Steps.IndexOf(step);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= Steps.Count) return;

        Steps.Move(index, target);
        ChainChanged();
    }

    // ---- Yardımcılar ----

    private static readonly JsonSerializerOptions CloneOptions = new();

    /// <summary>JSON ile derin kopya.</summary>
    private static ActionConfig Clone(ActionConfig config) =>
        JsonSerializer.Deserialize<ActionConfig>(JsonSerializer.Serialize(config, CloneOptions), CloneOptions)!;

    /// <summary>Zincirdeki toplam adım sayısı (iç içe zincirlerin kendisi ve adımları dahil).</summary>
    private static int CountSteps(ActionChainConfig chain) =>
        chain.Actions.Sum(a => a is ActionChainConfig inner ? 1 + CountSteps(inner) : 1);

    private static long TotalWaitMilliseconds(ActionChainConfig chain) =>
        chain.Actions.Sum(a => a switch
        {
            WaitActionConfig w => (long)Math.Max(0, w.Milliseconds),
            ActionChainConfig inner => TotalWaitMilliseconds(inner),
            _ => 0L
        });
}
