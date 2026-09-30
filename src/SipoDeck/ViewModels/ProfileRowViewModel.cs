using CommunityToolkit.Mvvm.ComponentModel;
using SipoDeck.Core.Profiles;

namespace SipoDeck.ViewModels;

/// <summary>Profil listesindeki tek satır (salt okunur özet; düzenleme taslağı ProfilesViewModel'dedir).</summary>
public sealed class ProfileRowViewModel : ObservableObject
{
    private string _name = string.Empty;
    private bool _isEnabled;
    private bool _isActive;
    private int _assignmentCount;

    public ProfileRowViewModel(ProfileData data, bool isActive)
    {
        Id = data.Id;
        Update(data, isActive);
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            if (SetProperty(ref _isEnabled, value))
                OnPropertyChanged(nameof(SummaryText));
        }
    }

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public int AssignmentCount
    {
        get => _assignmentCount;
        private set
        {
            if (SetProperty(ref _assignmentCount, value))
                OnPropertyChanged(nameof(SummaryText));
        }
    }

    public string SummaryText =>
        $"{(IsEnabled ? "Etkin" : "Pasif")} · {(AssignmentCount == 0 ? "Atama yok" : $"{AssignmentCount} atama")}";

    public void Update(ProfileData data, bool isActive)
    {
        Name = string.IsNullOrWhiteSpace(data.Name) ? data.Id : data.Name;
        IsEnabled = data.IsEnabled;
        IsActive = isActive;
        AssignmentCount = CountAssignments(data);
    }

    /// <summary>Eylemi (kısa veya uzun basma) dolu tuşlar + kombinasyonlar.</summary>
    public static int CountAssignments(ProfileData data) =>
        data.Keys.Values.Count(k => k is not null && (k.Action is not null || k.LongPressAction is not null))
        + data.Combinations.Count;
}
