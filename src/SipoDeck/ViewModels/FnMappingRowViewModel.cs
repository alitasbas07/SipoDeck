using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SipoDeck.ViewModels;

/// <summary>Profil seçim listesindeki öğe.</summary>
public sealed record ProfileOption(string Id, string Name);

/// <summary>FN + tuş → profil eşleştirme tablosunun tek satırı (taslak).</summary>
public sealed class FnMappingRowViewModel : ObservableObject
{
    private string _keyText;
    private string? _profileId;
    private string? _error;

    public FnMappingRowViewModel(string keyText, string? profileId, Action<FnMappingRowViewModel> remove)
    {
        _keyText = keyText;
        _profileId = profileId;
        RemoveCommand = new RelayCommand(() => remove(this));
    }

    public string KeyText
    {
        get => _keyText;
        set
        {
            if (SetProperty(ref _keyText, value ?? string.Empty))
                Error = null;
        }
    }

    public string? ProfileId
    {
        get => _profileId;
        set
        {
            // Seçenek listesi yenilenirken ComboBox null yazabilir; seçim bu yolla silinmez.
            if (value is not null && SetProperty(ref _profileId, value))
                Error = null;
        }
    }

    public string? Error
    {
        get => _error;
        set => SetProperty(ref _error, value);
    }

    public IRelayCommand RemoveCommand { get; }

    /// <summary>Seçenek listesi değiştiğinde ComboBox seçimini yeniden okutur.</summary>
    public void RefreshProfile() => OnPropertyChanged(nameof(ProfileId));
}
