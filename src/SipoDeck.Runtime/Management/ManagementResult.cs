using SipoDeck.Core.Validation;

namespace SipoDeck.Runtime.Management;

/// <summary>
/// Bir yönetim çağrısının (ayar/profil kaydı) sonucu. Üç durumdan biridir: başarılı,
/// doğrulama hatası (<see cref="Errors"/>) veya beklenmedik hata (<see cref="FailureMessage"/>).
/// Mesajlar bağlantı bilgisi (IP/port/COM) veya dosya yolu içermez.
/// </summary>
public sealed class ManagementResult
{
    private ManagementResult(IReadOnlyList<ValidationError> errors, string? failureMessage)
    {
        Errors = errors;
        FailureMessage = failureMessage;
    }

    public static ManagementResult Success { get; } = new(Array.Empty<ValidationError>(), null);

    public bool IsSuccess => Errors.Count == 0 && FailureMessage is null;

    /// <summary>Alan bazlı doğrulama hataları; hata yoksa boş.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Doğrulama dışı beklenmedik hata (ör. diske yazılamadı) için genel mesaj; yoksa null.</summary>
    public string? FailureMessage { get; }

    public static ManagementResult Invalid(IEnumerable<ValidationError> errors)
    {
        var list = errors.ToArray();
        return list.Length == 0 ? Success : new ManagementResult(list, null);
    }

    public static ManagementResult Failed(string message) => new(Array.Empty<ValidationError>(), message);
}
