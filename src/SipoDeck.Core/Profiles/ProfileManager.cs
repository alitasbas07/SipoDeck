namespace SipoDeck.Core.Profiles;

/// <summary>
/// Profilleri tutar ve aynı anda yalnızca bir profilin aktif olmasını sağlar.
/// Input Engine'den bağımsızdır.
/// </summary>
public sealed class ProfileManager
{
    private readonly Dictionary<string, Profile> _profiles = new();

    public Profile? ActiveProfile { get; private set; }

    public IReadOnlyCollection<Profile> Profiles => _profiles.Values;

    /// <summary>
    /// Profili ekler. İlk eklenen etkin profil otomatik olarak aktif olur.
    /// </summary>
    public void Add(Profile profile)
    {
        _profiles[profile.Id] = profile;
        if (ActiveProfile is null && profile.IsEnabled)
            ActiveProfile = profile;
    }

    /// <summary>
    /// Verilen kimlikteki etkin profili aktif yapar. Başarılıysa true döner.
    /// </summary>
    public bool SwitchTo(string profileId)
    {
        if (_profiles.TryGetValue(profileId, out var profile) && profile.IsEnabled)
        {
            ActiveProfile = profile;
            return true;
        }

        return false;
    }
}
