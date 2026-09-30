using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;

namespace SipoDeck.Core.Validation;

/// <summary>
/// Profil verisini doğrular. Alan yolları "Profiles[i].Keys[k].Action.Url",
/// "Profiles[i].Combinations[j].Keys", zincir içi eylemler için "...Actions[n]" biçimindedir.
/// </summary>
public static class ProfilesValidator
{
    public static ValidationResult Validate(ProfilesData data)
    {
        var errors = new List<ValidationError>();

        if (data.Profiles.Count == 0)
            errors.Add(new("Profiles", "En az bir profil olmalıdır."));

        var seenIds = new HashSet<string>();
        for (var i = 0; i < data.Profiles.Count; i++)
        {
            var profile = data.Profiles[i];
            var path = $"Profiles[{i}]";

            if (string.IsNullOrWhiteSpace(profile.Name))
                errors.Add(new($"{path}.Name", "Profil adı boş olamaz."));

            if (string.IsNullOrWhiteSpace(profile.Id))
                errors.Add(new($"{path}.Id", "Profil kimliği boş olamaz."));
            else if (!seenIds.Add(profile.Id))
                errors.Add(new($"{path}.Id", "Profil kimliği benzersiz olmalıdır."));

            foreach (var (key, config) in profile.Keys)
            {
                var keyPath = $"{path}.Keys[{key}]";
                if (key < 0)
                    errors.Add(new(keyPath, "Tuş numarası negatif olamaz."));
                if (config is null)
                    continue;
                ValidateAction(config.Action, $"{keyPath}.Action", errors);
                ValidateAction(config.LongPressAction, $"{keyPath}.LongPressAction", errors);
            }

            for (var j = 0; j < profile.Combinations.Count; j++)
            {
                var combination = profile.Combinations[j];
                var combinationPath = $"{path}.Combinations[{j}]";

                if (combination.Keys.Any(k => k < 0))
                    errors.Add(new($"{combinationPath}.Keys", "Tuş numarası negatif olamaz."));
                if (combination.Keys.Distinct().Count() < 2)
                    errors.Add(new($"{combinationPath}.Keys", "Kombinasyon en az iki farklı tuş içermelidir."));

                if (combination.Action is null)
                    errors.Add(new($"{combinationPath}.Action", "Kombinasyon için eylem gereklidir."));
                else
                    ValidateAction(combination.Action, $"{combinationPath}.Action", errors);
            }
        }

        if (!string.IsNullOrEmpty(data.ActiveProfileId) && !seenIds.Contains(data.ActiveProfileId))
            errors.Add(new("ActiveProfileId", "Aktif profil mevcut bir profili göstermelidir."));

        return ValidationResult.Failure(errors);
    }

    // Eylem boşsa (atanmamış) geçerlidir; dolu ise türüne göre alanları denetlenir.
    private static void ValidateAction(ActionConfig? action, string path, List<ValidationError> errors)
    {
        switch (action)
        {
            case null:
                break;
            case RunProgramActionConfig run when string.IsNullOrWhiteSpace(run.FilePath):
                errors.Add(new($"{path}.FilePath", "Program yolu boş olamaz."));
                break;
            case OpenUrlActionConfig url when string.IsNullOrWhiteSpace(url.Url):
                errors.Add(new($"{path}.Url", "Adres boş olamaz."));
                break;
            case WaitActionConfig wait when wait.Milliseconds < 0:
                errors.Add(new($"{path}.Milliseconds", "Bekleme süresi negatif olamaz."));
                break;
            case NotificationActionConfig n when string.IsNullOrWhiteSpace(n.Title) && string.IsNullOrWhiteSpace(n.Message):
                errors.Add(new($"{path}.Message", "Bildirim başlığı veya mesajı girilmelidir."));
                break;
            case VolumeActionConfig v when !Enum.IsDefined(v.Command):
                errors.Add(new($"{path}.Command", "Geçersiz ses komutu."));
                break;
            case MediaActionConfig m when !Enum.IsDefined(m.Command):
                errors.Add(new($"{path}.Command", "Geçersiz medya komutu."));
                break;
            case ActionChainConfig chain:
                for (var i = 0; i < chain.Actions.Count; i++)
                {
                    if (chain.Actions[i] is null)
                        errors.Add(new($"{path}.Actions[{i}]", "Zincirdeki eylem boş olamaz."));
                    else
                        ValidateAction(chain.Actions[i], $"{path}.Actions[{i}]", errors);
                }
                break;
        }
    }
}
