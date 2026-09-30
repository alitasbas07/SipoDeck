namespace SipoDeck.Core.Validation;

/// <summary>Doğrulama sonucu: hata yoksa geçerlidir.</summary>
public sealed class ValidationResult
{
    private ValidationResult(IReadOnlyList<ValidationError> errors) => Errors = errors;

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ValidationError> Errors { get; }

    public static ValidationResult Success { get; } = new(Array.Empty<ValidationError>());

    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        var list = errors.ToArray();
        return list.Length == 0 ? Success : new ValidationResult(list);
    }
}
