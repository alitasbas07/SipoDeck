using CommunityToolkit.Mvvm.ComponentModel;
using SipoDeck.Core.Actions;

namespace SipoDeck.ViewModels;

/// <summary>
/// Tek bir eylem yapılandırmasının temel alanlarını düzenler. Zincir eylemi yalnızca özet olarak gösterilir (düzenleyici Task 019).
/// </summary>
public sealed partial class ActionEditorViewModel : ObservableObject
{
    public const string NoneType = "(Yok)";
    public const string ChainType = "Zincir";

    private static readonly string[] Types = { NoneType, "Program çalıştır", "URL aç", "Bildirim", "Ses", "Medya", ChainType };

    private ActionConfig? _config;
    private bool _loading;

    public IReadOnlyList<string> TypeOptions => Types;
    public IReadOnlyList<VolumeCommand> VolumeCommands { get; } = Enum.GetValues<VolumeCommand>();
    public IReadOnlyList<MediaCommand> MediaCommands { get; } = Enum.GetValues<MediaCommand>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProgram), nameof(IsUrl), nameof(IsNotification), nameof(IsVolume), nameof(IsMedia), nameof(IsChain))]
    private string _selectedType = NoneType;

    [ObservableProperty]
    private string _text1 = string.Empty;

    [ObservableProperty]
    private string _text2 = string.Empty;

    [ObservableProperty]
    private string _text3 = string.Empty;

    [ObservableProperty]
    private VolumeCommand _volumeCommand;

    [ObservableProperty]
    private MediaCommand _mediaCommand;

    public bool IsProgram => SelectedType == Types[1];
    public bool IsUrl => SelectedType == Types[2];
    public bool IsNotification => SelectedType == Types[3];
    public bool IsVolume => SelectedType == Types[4];
    public bool IsMedia => SelectedType == Types[5];
    public bool IsChain => SelectedType == ChainType;

    public string ChainSummary => _config is ActionChainConfig c ? $"{c.Actions.Count} adımlı zincir (düzenleme Task 019'da)" : string.Empty;

    /// <summary>Kullanıcı bir alanı değiştirdiğinde tetiklenir; yeni yapılandırma <see cref="Config"/> üzerindedir.</summary>
    public event EventHandler? Changed;

    public ActionConfig? Config => _config;

    public void Load(ActionConfig? config)
    {
        _loading = true;
        _config = config;
        Text1 = Text2 = Text3 = string.Empty;
        switch (config)
        {
            case RunProgramActionConfig p: SelectedType = Types[1]; Text1 = p.FilePath; Text2 = p.Arguments ?? string.Empty; Text3 = p.WorkingDirectory ?? string.Empty; break;
            case OpenUrlActionConfig u: SelectedType = Types[2]; Text1 = u.Url; break;
            case NotificationActionConfig n: SelectedType = Types[3]; Text1 = n.Title; Text2 = n.Message; break;
            case VolumeActionConfig v: SelectedType = Types[4]; VolumeCommand = v.Command; break;
            case MediaActionConfig m: SelectedType = Types[5]; MediaCommand = m.Command; break;
            case ActionChainConfig: SelectedType = ChainType; break;
            default: SelectedType = NoneType; break;
        }
        OnPropertyChanged(nameof(ChainSummary));
        _loading = false;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_loading || SelectedType == ChainType
            || e.PropertyName is nameof(Config) or nameof(ChainSummary) || e.PropertyName?.StartsWith("Is") == true)
            return;

        _config = Build();
        OnPropertyChanged(nameof(Config));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private ActionConfig? Build()
    {
        string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
        if (SelectedType == Types[1]) return new RunProgramActionConfig { FilePath = Text1, Arguments = NullIfEmpty(Text2), WorkingDirectory = NullIfEmpty(Text3) };
        if (SelectedType == Types[2]) return new OpenUrlActionConfig { Url = Text1 };
        if (SelectedType == Types[3]) return new NotificationActionConfig { Title = Text1, Message = Text2 };
        if (SelectedType == Types[4]) return new VolumeActionConfig { Command = VolumeCommand };
        if (SelectedType == Types[5]) return new MediaActionConfig { Command = MediaCommand };
        return null;
    }
}
