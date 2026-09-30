namespace SipoDeck.ViewModels;

/// <summary>
/// Kaydedilmemiş taslağı olabilen sayfa. Kabuk, sayfa değişiminden önce bu sözleşmeyle
/// Kaydet / Vazgeç / İptal uyarısını yönetir.
/// </summary>
public interface IUnsavedChangesGuard
{
    bool HasChanges { get; }

    /// <summary>Uyarı penceresinde gösterilen açıklama.</summary>
    string UnsavedChangesMessage { get; }

    /// <summary>Taslağı kaydeder; başarılıysa true (hata varsa sayfada gösterilir).</summary>
    Task<bool> SaveAsync();

    /// <summary>Taslağı atar.</summary>
    void Discard();
}
