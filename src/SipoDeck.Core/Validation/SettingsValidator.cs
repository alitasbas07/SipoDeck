using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Validation;

/// <summary>
/// Ayarları doğrular. Yalnızca seçili bağlantı tipinin alanları denetlenir.
/// Bağlantı hiç doldurulmamışsa ("henüz yapılandırılmamış") geçerli sayılır:
/// Wi-Fi için Host boş ve Port 0, Serial için port adı boş ve baud rate varsayılan değerdeyse.
/// Kısmen doldurulmuş bağlantı hata verir.
/// </summary>
public static class SettingsValidator
{
    public static ValidationResult Validate(AppSettings settings, IReadOnlySet<string> knownProfileIds)
    {
        var errors = new List<ValidationError>();
        var connection = settings.Connection;

        if (connection.ConnectionType == ConnectionType.WiFi)
        {
            var unconfigured = string.IsNullOrWhiteSpace(connection.Host) && connection.Port == 0;
            if (!unconfigured)
            {
                if (string.IsNullOrWhiteSpace(connection.Host))
                    errors.Add(new("Connection.Host", "Adres boş olamaz."));
                if (connection.Port < 1 || connection.Port > 65535)
                    errors.Add(new("Connection.Port", "Port 1 ile 65535 arasında olmalıdır."));
            }
        }
        else if (connection.ConnectionType == ConnectionType.Serial)
        {
            var unconfigured = string.IsNullOrWhiteSpace(connection.SerialPortName)
                               && connection.BaudRate == SettingsDefaults.BaudRate;
            if (!unconfigured)
            {
                if (string.IsNullOrWhiteSpace(connection.SerialPortName))
                    errors.Add(new("Connection.SerialPortName", "Seri port adı boş olamaz."));
                if (connection.BaudRate <= 0)
                    errors.Add(new("Connection.BaudRate", "Baud rate sıfırdan büyük olmalıdır."));
            }
        }

        if (settings.Input.FnKey < 0)
            errors.Add(new("Input.FnKey", "Tuş numarası negatif olamaz."));

        if (settings.Input.LongPressThresholdMilliseconds <= 0)
            errors.Add(new("Input.LongPressThresholdMilliseconds", "Uzun basma süresi sıfırdan büyük olmalıdır."));

        foreach (var (key, profileId) in settings.Input.ProfileSwitchMap)
        {
            if (key < 0)
                errors.Add(new($"Input.ProfileSwitchMap[{key}]", "Tuş numarası negatif olamaz."));
            if (string.IsNullOrWhiteSpace(profileId) || !knownProfileIds.Contains(profileId))
                errors.Add(new($"Input.ProfileSwitchMap[{key}]", "Profil bulunamadı."));
        }

        return ValidationResult.Failure(errors);
    }
}
