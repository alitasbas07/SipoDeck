using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Profiles;

/// <summary>
/// Tuşlara eylem atayan bir profili temsil eder.
/// Aynı fiziksel tuş farklı profillerde farklı eylemlere bağlanabilir.
/// </summary>
public sealed class Profile
{
    public Profile(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Tekli tuş eşleştirmeleri (tuş numarası → eylem).</summary>
    public Dictionary<int, KeyBinding> Keys { get; } = new();

    /// <summary>Kombinasyon eşleştirmeleri (tuş kümesi → eylem).</summary>
    public Dictionary<KeyCombination, IAction> Combinations { get; } = new();
}
