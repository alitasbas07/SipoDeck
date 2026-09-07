namespace SipoDeck.Core.Settings;

/// <summary>
/// Girdi ayarları: FN tuşu ve uzun basma süresi. Değerler yapılandırılabilir.
/// </summary>
public sealed class InputSettings
{
    public int FnKey { get; set; } = SettingsDefaults.FnKey;

    public int LongPressThresholdMilliseconds { get; set; } = SettingsDefaults.LongPressThresholdMilliseconds;
}
