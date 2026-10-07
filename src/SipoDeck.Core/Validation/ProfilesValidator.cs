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

    // Eylem boşsa (atanmamış) geçerlidir; dolu ise katalog tanımına göre alanları denetlenir.
    // Zincirler için adım sınırı, iç içe derinlik ve döngü denetlenir.
    private static void ValidateAction(ActionConfig? action, string path, List<ValidationError> errors)
    {
        if (action is null)
            return;

        var totalSteps = 0;
        ValidateAction(action, path, errors, new List<ActionConfig>(), ref totalSteps);

        if (totalSteps > ActionCatalog.MaxChainSteps)
            errors.Add(new(path, $"Zincirler toplamda en fazla {ActionCatalog.MaxChainSteps} adım içerebilir."));
    }

    private static void ValidateAction(ActionConfig action, string path, List<ValidationError> errors,
        List<ActionConfig> ancestors, ref int totalSteps)
    {
        totalSteps++;

        foreach (var (property, message) in ActionCatalog.Default.ValidateParameters(action))
            errors.Add(new(string.IsNullOrEmpty(property) ? path : $"{path}.{property}", message));

        if (action is not ActionChainConfig chain)
            return;

        if (ancestors.Any(a => ReferenceEquals(a, chain)))
        {
            errors.Add(new(path, "Zincir kendisini içeremez (döngü)."));
            return;
        }

        if (ancestors.Count + 1 > ActionCatalog.MaxChainDepth)
        {
            errors.Add(new(path, $"Zincirler en fazla {ActionCatalog.MaxChainDepth} seviye iç içe olabilir."));
            return;
        }

        ancestors.Add(chain);
        for (var i = 0; i < chain.Actions.Count; i++)
        {
            if (chain.Actions[i] is null)
                errors.Add(new($"{path}.Actions[{i}]", "Zincirdeki eylem boş olamaz."));
            else
                ValidateAction(chain.Actions[i], $"{path}.Actions[{i}]", errors, ancestors, ref totalSteps);
        }
        ancestors.RemoveAt(ancestors.Count - 1);
    }
}
