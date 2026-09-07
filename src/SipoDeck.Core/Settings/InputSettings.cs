namespace SipoDeck.Core.Settings;

/// <summary>
/// Girdi ayarları: FN tuşu ve uzun basma süresi. Değerler yapılandırılabilir.
/// </summary>
public sealed class InputSettings
{
    public int FnKey { get; set; } = SettingsDefaults.FnKey;

    public int LongPressThresholdMilliseconds { get; set; } = SettingsDefaults.LongPressThresholdMilliseconds;

    /// <summary>
    /// FN + tuş → profil değiştirme eşleştirmeleri (tuş numarası → profil kimliği).
    /// FN yapılandırması profil verilerinden ayrı, ayarlarda saklanır.
    /// </summary>
    public Dictionary<int, string> ProfileSwitchMap { get; set; } = new();
}
