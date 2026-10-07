using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Runtime;
using SipoDeck.Runtime.Management;
using SipoDeck.Runtime.Events;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Settings;
using System.Linq;

namespace SipoDeck.ViewModels;

public sealed partial class KeyViewModel : ObservableObject
{
    public int Key { get; }
    public bool CanAssign => !IsFnKey;
    public string Label => $"Tuş {Key}";

    [ObservableProperty]
    private ActionConfig? _action;

    [ObservableProperty]
    private ActionConfig? _longPressAction;

    [ObservableProperty]
    private bool _isFnKey;

    public KeyViewModel(int key, KeyConfig? config, bool isFnKey, ActionCatalog catalog, Func<IReadOnlyList<ProfileOption>> profiles)
    {
        Key = key;
        ActionEditor = new ActionEditorViewModel(catalog, profiles);
        LongPressEditor = new ActionEditorViewModel(catalog, profiles);
        IsFnKey = isFnKey;
        Action = config?.Action;
        LongPressAction = config?.LongPressAction;
        ActionEditor.Load(Action);
        LongPressEditor.Load(LongPressAction);
        ActionEditor.Changed += (_, _) => Action = ActionEditor.Config;
        LongPressEditor.Changed += (_, _) => LongPressAction = LongPressEditor.Config;
    }

    public ActionEditorViewModel ActionEditor { get; }
    public ActionEditorViewModel LongPressEditor { get; }

    /// <summary>Atamayı (normal + uzun basma) verilen yapılandırmalarla değiştirir ve editörleri günceller.</summary>
    public void SetActions(ActionConfig? action, ActionConfig? longPress)
    {
        Action = action;
        LongPressAction = longPress;
        ActionEditor.Load(action);
        LongPressEditor.Load(longPress);
    }

    public KeyConfig ToConfig() => new() { Action = Action, LongPressAction = LongPressAction };
}

public sealed partial class CombinationViewModel : ObservableObject
{
    public ObservableCollection<int> Keys { get; } = new();

    [ObservableProperty]
    private ActionConfig? _action;

    public ActionEditorViewModel ActionEditor { get; }

    public string KeysString
    {
        get => string.Join(", ", Keys);
        set
        {
            Keys.Clear();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parts = value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p.Trim(), out int val))
                    {
                        Keys.Add(val);
                    }
                }
            }
            OnPropertyChanged(nameof(KeysString));
        }
    }

    public CombinationViewModel(CombinationConfig config, ActionCatalog catalog, Func<IReadOnlyList<ProfileOption>> profiles)
    {
        ActionEditor = new ActionEditorViewModel(catalog, profiles);
        foreach (var k in config.Keys) Keys.Add(k);
        Action = config.Action;
        ActionEditor.Load(Action);
        ActionEditor.Changed += (_, _) => Action = ActionEditor.Config;
        Keys.CollectionChanged += (s, e) => OnPropertyChanged(nameof(KeysString));
    }

    public CombinationConfig ToConfig() => new() { Keys = Keys.ToList(), Action = Action };
}
