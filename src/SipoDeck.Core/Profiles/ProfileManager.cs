namespace SipoDeck.Core.Profiles;

/// <summary>
/// Profilleri tutar ve aynı anda yalnızca bir profilin aktif olmasını sağlar.
/// Input Engine'den bağımsızdır. Thread-safe: okumalar ve yönetim çağrıları farklı
/// thread'lerden yapılabilir. <see cref="ActiveProfileChanged"/> olayı kilit dışında tetiklenir.
/// </summary>
public sealed class ProfileManager
{
    private readonly object _lock = new();
    private readonly List<Profile> _profiles = new();
    private Profile? _activeProfile;

    /// <summary>
    /// Aktif profil kimliği değiştiğinde tetiklenir (argüman: yeni aktif profil kimliği,
    /// aktif profil kalmadıysa null). Aynı kimlikli profile tekrar geçişte veya aktif profilin
    /// nesnesi aynı kimlikle değiştirildiğinde tetiklenmez. Olay, çağıran thread'de ve kilit dışında çalışır.
    /// </summary>
    public event EventHandler<string?>? ActiveProfileChanged;

    public Profile? ActiveProfile
    {
        get
        {
            lock (_lock)
                return _activeProfile;
        }
    }

    /// <summary>Profillerin o andaki kopya listesi (eklenme sırasıyla).</summary>
    public IReadOnlyCollection<Profile> Profiles
    {
        get
        {
            lock (_lock)
                return _profiles.ToArray();
        }
    }

    /// <summary>
    /// Profili ekler; aynı kimlikli profil varsa yerine koyar. Aktif profil yoksa ve profil etkinse aktif olur.
    /// </summary>
    public void Add(Profile profile)
    {
        Change(() =>
        {
            var index = IndexOf(profile.Id);
            if (index >= 0)
                _profiles[index] = profile;
            else
                _profiles.Add(profile);

            if (_activeProfile is null || _activeProfile.Id == profile.Id)
                _activeProfile = profile;
            FixActive(preferredId: null);
        });
    }

    /// <summary>
    /// Verilen kimlikteki etkin profili aktif yapar. Başarılıysa true döner.
    /// </summary>
    public bool SwitchTo(string profileId)
    {
        var success = false;
        Change(() =>
        {
            var index = IndexOf(profileId);
            if (index >= 0 && _profiles[index].IsEnabled)
            {
                _activeProfile = _profiles[index];
                success = true;
            }
        });
        return success;
    }

    /// <summary>
    /// Profili kaldırır; bulunamazsa false döner. Aktif profil kaldırılırsa başka bir etkin profil
    /// (yoksa null) aktif olur.
    /// </summary>
    public bool Remove(string profileId)
    {
        var removed = false;
        Change(() =>
        {
            var index = IndexOf(profileId);
            if (index < 0)
                return;

            _profiles.RemoveAt(index);
            removed = true;
            FixActive(preferredId: null);
        });
        return removed;
    }

    /// <summary>
    /// Aynı kimlikli mevcut profili günceller. Profil aktifse <see cref="ActiveProfile"/> yeni nesneyi gösterir;
    /// artık etkin değilse başka bir etkin profil aktif olur. Kimlik bulunamazsa <see cref="KeyNotFoundException"/> fırlatır.
    /// </summary>
    public void Replace(Profile profile)
    {
        Change(() =>
        {
            var index = IndexOf(profile.Id);
            if (index < 0)
                throw new KeyNotFoundException($"Profil bulunamadı: {profile.Id}");

            _profiles[index] = profile;
            if (_activeProfile?.Id == profile.Id)
                _activeProfile = profile;
            FixActive(preferredId: null);
        });
    }

    /// <summary>
    /// Tüm profilleri tek seferde yükler (toplu yükleme). Aktif profil, <paramref name="activeProfileId"/>
    /// etkin bir profili gösteriyorsa o, değilse ilk etkin profil olur (yoksa null). Tek olay tetiklenir.
    /// </summary>
    public void ReplaceAll(IEnumerable<Profile> profiles, string? activeProfileId = null)
    {
        var list = profiles.ToList();
        Change(() =>
        {
            _profiles.Clear();
            foreach (var profile in list)
            {
                var index = IndexOf(profile.Id);
                if (index >= 0)
                    _profiles[index] = profile;
                else
                    _profiles.Add(profile);
            }

            _activeProfile = null;
            FixActive(activeProfileId);
        });
    }

    /// <summary>Tüm profilleri kaldırır; aktif profil null olur.</summary>
    public void Clear() => ReplaceAll(Array.Empty<Profile>());

    private int IndexOf(string profileId) => _profiles.FindIndex(p => p.Id == profileId);

    // Kilit altında çağrılır: aktif profil geçerli ve etkin değilse tercih edilene, yoksa ilk etkin profile geçer.
    private void FixActive(string? preferredId)
    {
        if (_activeProfile is not null)
        {
            var current = _profiles.Find(p => p.Id == _activeProfile.Id);
            if (current is not null && current.IsEnabled)
            {
                _activeProfile = current;
                return;
            }
        }

        _activeProfile = _profiles.Find(p => p.IsEnabled && p.Id == preferredId)
                         ?? _profiles.Find(p => p.IsEnabled);
    }

    private void Change(Action mutate)
    {
        string? newId;
        bool changed;
        lock (_lock)
        {
            var oldId = _activeProfile?.Id;
            mutate();
            newId = _activeProfile?.Id;
            changed = oldId != newId;
        }

        if (changed)
            ActiveProfileChanged?.Invoke(this, newId);
    }
}
