using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Profiles;

/// <summary>
/// Bir tuşun saklanabilir yapılandırması: normal ve (isteğe bağlı) uzun basma eylemi.
/// </summary>
public sealed class KeyConfig
{
    public ActionConfig? Action { get; set; }
    public ActionConfig? LongPressAction { get; set; }

    public KeyBinding ToBinding() => new(Action?.ToAction(), LongPressAction?.ToAction());
}

/// <summary>
/// Bir kombinasyonun saklanabilir yapılandırması: tuş kümesi ve eylemi.
/// </summary>
public sealed class CombinationConfig
{
    public List<int> Keys { get; set; } = new();
    public ActionConfig? Action { get; set; }
}

/// <summary>
/// Bir profilin saklanabilir hâlidir. Çalışma zamanı <see cref="Profile"/> nesnesine
/// <see cref="ToProfile"/> ile dönüştürülür. Aynı tuş farklı profillerde farklı eylemler taşıyabilir.
/// </summary>
public sealed class ProfileData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<int, KeyConfig> Keys { get; set; } = new();
    public List<CombinationConfig> Combinations { get; set; } = new();

    public Profile ToProfile()
    {
        var profile = new Profile(Id, Name) { IsEnabled = IsEnabled };

        foreach (var (key, config) in Keys)
            profile.Keys[key] = config.ToBinding();

        foreach (var combination in Combinations)
        {
            if (combination.Action is not null && combination.Keys.Count >= 2)
                profile.Combinations[new KeyCombination(combination.Keys)] = combination.Action.ToAction();
        }

        return profile;
    }
}
