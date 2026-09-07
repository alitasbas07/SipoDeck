namespace SipoDeck.Core.Profiles;

/// <summary>
/// Birlikte basılması gereken tuşlardan oluşan bir kombinasyonu temsil eder.
/// Tuş kümesine göre değer eşitliği sağlar; sözlük anahtarı olarak kullanılabilir.
/// </summary>
public sealed class KeyCombination : IEquatable<KeyCombination>
{
    private readonly int[] _keys;

    public KeyCombination(IEnumerable<int> keys)
    {
        _keys = keys.Distinct().OrderBy(k => k).ToArray();
        if (_keys.Length < 2)
            throw new ArgumentException("Bir kombinasyon en az iki tuş içermelidir.", nameof(keys));
    }

    public IReadOnlyList<int> Keys => _keys;

    public bool Contains(int key) => Array.IndexOf(_keys, key) >= 0;

    /// <summary>
    /// Kombinasyondaki tüm tuşların basılı tuşlar arasında olup olmadığını döndürür.
    /// </summary>
    public bool IsSatisfiedBy(IReadOnlySet<int> pressedKeys) => _keys.All(pressedKeys.Contains);

    public bool Equals(KeyCombination? other)
        => other is not null && _keys.AsSpan().SequenceEqual(other._keys);

    public override bool Equals(object? obj) => Equals(obj as KeyCombination);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var key in _keys)
            hash.Add(key);
        return hash.ToHashCode();
    }
}
