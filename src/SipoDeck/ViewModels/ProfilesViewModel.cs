using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Runtime;
using SipoDeck.Runtime.Management;

namespace SipoDeck.ViewModels;

/// <summary>
/// Profiller sayfası: sol liste + sağ detay. Ad ve etkinlik düzenlemesi taslakta tutulur, yalnızca Kaydet ile
/// Runtime'a gider. Oluştur / kopyala / sil / aktif yap doğrudan Runtime çağrısıdır; taslak kirliyken
/// önce Kaydet / Vazgeç / İptal uyarısı çıkar. Tuş atamaları bu sayfada düzenlenmez ve değiştirilmez.
/// </summary>
public sealed class ProfilesViewModel : ObservableObject, IUnsavedChangesGuard, IDisposable
{
    private static readonly TimeSpan SavedMessageDuration = TimeSpan.FromSeconds(4);
    private static readonly Regex ProfileFieldPattern = new(@"^Profiles\[(\d+)\]\.?(.*)$", RegexOptions.Compiled);

    private readonly AppRuntime _runtime;
    private readonly Dispatcher _dispatcher;
    private List<ProfileData> _profiles = new();
    private string? _activeId;
    private ProfileData? _baseline;
    private ProfileRowViewModel? _selected;
    private string? _preferredId;
    private bool _ownChange;
    private bool _disposed;
    private int _savedMessageVersion;
    private Func<Task>? _pending;

    private string _draftName = string.Empty;
    private bool _draftIsEnabled;
    private bool _isBusy;
    private bool _isDeleteConfirmVisible;
    private string? _deleteBlockedNote;
    private string? _nameError;
    private string? _generalError;
    private string? _infoMessage;
    private string? _savedMessage;

    public ProfilesViewModel(AppRuntime runtime, Dispatcher dispatcher)
    {
        _runtime = runtime;
        _dispatcher = dispatcher;

        SaveCommand = new AsyncRelayCommand(() => SaveAsync(), () => HasChanges && !IsBusy);
        CancelCommand = new RelayCommand(Discard, () => HasChanges && !IsBusy);
        CreateCommand = new AsyncRelayCommand(() => Guarded(CreateAsync), () => !IsBusy);
        DuplicateCommand = new AsyncRelayCommand(() => Guarded(DuplicateAsync), () => _selected is not null && !IsBusy);
        SetActiveCommand = new AsyncRelayCommand(() => Guarded(SetActiveAsync), () => CanSetActive);
        DeleteCommand = new AsyncRelayCommand(() => Guarded(BeginDelete), () => CanDelete);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync, () => IsDeleteConfirmVisible && !IsBusy && _deleteBlockedNote is null);
        CancelDeleteCommand = new RelayCommand(() => IsDeleteConfirmVisible = false);

        UnsavedSaveCommand = new AsyncRelayCommand(ResolveUnsavedSaveAsync);
        UnsavedDiscardCommand = new AsyncRelayCommand(ResolveUnsavedDiscardAsync);
        UnsavedCancelCommand = new RelayCommand(CancelPending);

        Refresh();

        runtime.ProfilesChanged += OnRuntimeChanged;
        runtime.ActiveProfileChanged += OnRuntimeActiveChanged;
    }

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new();

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IAsyncRelayCommand CreateCommand { get; }

    public IAsyncRelayCommand DuplicateCommand { get; }

    public IAsyncRelayCommand SetActiveCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public IAsyncRelayCommand UnsavedSaveCommand { get; }

    public IAsyncRelayCommand UnsavedDiscardCommand { get; }

    public IRelayCommand UnsavedCancelCommand { get; }

    // ---- Seçim ----

    public ProfileRowViewModel? SelectedProfile
    {
        get => _selected;
        set
        {
            // Liste yeniden kurulurken ListBox null yazabilir; seçim bu yolla silinmez.
            if (value is null || ReferenceEquals(value, _selected))
                return;

            if (HasChanges)
            {
                // Seçim onaya bağlanır; detay onaya kadar eski profilde kalır (İptal listeyi eski seçime döndürür).
                Pending = () =>
                {
                    if (Profiles.Contains(value))
                        SelectCore(value);
                    return Task.CompletedTask;
                };
                return;
            }

            SelectCore(value);
        }
    }

    public bool HasSelection => _selected is not null;

    // ---- Taslak ----

    public string DraftName
    {
        get => _draftName;
        set
        {
            if (!SetProperty(ref _draftName, value ?? string.Empty))
                return;

            NameError = null;
            SavedMessage = null;
            RefreshState();
        }
    }

    public bool DraftIsEnabled
    {
        get => _draftIsEnabled;
        set
        {
            if (!SetProperty(ref _draftIsEnabled, value))
                return;

            SavedMessage = null;
            RefreshState();
        }
    }

    public bool HasChanges =>
        _baseline is not null && (_draftName != _baseline.Name || _draftIsEnabled != _baseline.IsEnabled);

    public string UnsavedChangesMessage =>
        "Profil düzenlemesinde kaydetmediğin değişiklikler var. Sayfadan ayrılmadan önce ne yapmak istersin?";

    // ---- Seçili profilin durumu (kayıtlı haline göre) ----

    public bool IsSelectedActive => _selected is not null && _selected.Id == _activeId;

    public bool CanSetActive => _baseline is { IsEnabled: true } && !IsSelectedActive && !IsBusy;

    /// <summary>Aktif yap düğmesi pasifken nedeni.</summary>
    public string? SetActiveHint =>
        _baseline is { IsEnabled: false } ? "Pasif profil aktif yapılamaz. Önce etkinleştirip kaydet." : null;

    public bool CanDelete => _selected is not null && Profiles.Count > 1 && !IsBusy;

    public string? DeleteHint => _selected is not null && Profiles.Count <= 1 ? "Son profil silinemez." : null;

    public string AssignmentSummary => _selected is null
        ? string.Empty
        : _selected.AssignmentCount == 0 ? "Bu profilde henüz tuş ataması yok." : $"{_selected.AssignmentCount} atama yapılmış.";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshState();
        }
    }

    // ---- Silme onayı ----

    public bool IsDeleteConfirmVisible
    {
        get => _isDeleteConfirmVisible;
        private set
        {
            if (SetProperty(ref _isDeleteConfirmVisible, value))
            {
                if (!value)
                    DeleteBlockedNote = null;
                ConfirmDeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Profil bir FN eşleştirmesinde kullanılıyorsa silmeyi engelleyen açıklama.</summary>
    public string? DeleteBlockedNote
    {
        get => _deleteBlockedNote;
        private set
        {
            if (SetProperty(ref _deleteBlockedNote, value))
                ConfirmDeleteCommand.NotifyCanExecuteChanged();
        }
    }

    public string DeleteConfirmText => _selected is null
        ? string.Empty
        : $"\"{_selected.Name}\" profili silinecek. Tuş atamaları da silinir; bu geri alınamaz.";

    // ---- Mesajlar ----

    public string? NameError
    {
        get => _nameError;
        private set => SetProperty(ref _nameError, value);
    }

    public string? GeneralError
    {
        get => _generalError;
        private set => SetProperty(ref _generalError, value);
    }

    /// <summary>Aktif profil değiştiğinde vb. kullanıcıya verilen kısa bilgi.</summary>
    public string? InfoMessage
    {
        get => _infoMessage;
        private set => SetProperty(ref _infoMessage, value);
    }

    public string? SavedMessage
    {
        get => _savedMessage;
        private set => SetProperty(ref _savedMessage, value);
    }

    // ---- Kaydedilmemiş değişiklik uyarısı (sayfa içi) ----

    private Func<Task>? Pending
    {
        get => _pending;
        set
        {
            if (ReferenceEquals(_pending, value))
                return;

            _pending = value;
            OnPropertyChanged(nameof(IsUnsavedPromptVisible));
            OnPropertyChanged(nameof(UnsavedMessage));
        }
    }

    public bool IsUnsavedPromptVisible => _pending is not null;

    public string UnsavedMessage => "Profilde kaydetmediğin değişiklikler var. Devam etmeden önce ne yapmak istersin?";

    // ---- IUnsavedChangesGuard ----

    /// <summary>Taslağı kayıtlı profile döndürür.</summary>
    public void Discard()
    {
        LoadDraft();
        ClearMessages();
        RefreshState();
    }

    /// <summary>Ad/etkinliği kaydeder; tuşlar ve kombinasyonlar kayıtlı haliyle aynen gönderilir.</summary>
    public async Task<bool> SaveAsync()
    {
        if (IsBusy || _selected is null || _baseline is null)
            return false;

        if (!HasChanges)
            return true;

        ClearMessages();

        var name = _draftName.Trim();
        if (name.Length == 0)
        {
            NameError = "Profil adı boş olamaz.";
            return false;
        }

        var draft = CloneProfile(_baseline);
        draft.Name = name;
        draft.IsEnabled = _draftIsEnabled;

        var before = _activeId;
        IsBusy = true;
        _ownChange = true;
        try
        {
            var result = await _runtime.UpdateProfileAsync(draft);
            if (result.IsSuccess)
            {
                _draftName = name;
                Refresh();
                InfoMessage = ActiveChangeNotice(before);
                ShowSavedMessage();
                return true;
            }

            ApplyResultErrors(result, draft.Id);
            return false;
        }
        catch (Exception)
        {
            GeneralError = "Profil kaydedilemedi.";
            return false;
        }
        finally
        {
            _ownChange = false;
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _runtime.ProfilesChanged -= OnRuntimeChanged;
        _runtime.ActiveProfileChanged -= OnRuntimeActiveChanged;
    }

    // ---- Komutlar ----

    // Taslak kirliyse işlem onaya bağlanır (Kaydet / Vazgeç / İptal); değilse hemen çalışır.
    private Task Guarded(Func<Task> action)
    {
        if (!HasChanges)
            return action();

        Pending = action;
        return Task.CompletedTask;
    }

    // İptal: bekleyen işlem düşer; liste seçimi (kullanıcı başka satıra tıklamışsa) gerçek seçime döner.
    private void CancelPending()
    {
        Pending = null;
        OnPropertyChanged(nameof(SelectedProfile));
    }

    private async Task ResolveUnsavedSaveAsync()
    {
        var action = _pending;
        if (action is null)
            return;

        // Hata varsa işlem iptal olur; hatalar alanların yanında kalır.
        Pending = null;
        if (await SaveAsync())
            await action();
        else
            OnPropertyChanged(nameof(SelectedProfile));
    }

    private async Task ResolveUnsavedDiscardAsync()
    {
        var action = _pending;
        Pending = null;
        Discard();
        if (action is not null)
            await action();
    }

    private async Task CreateAsync()
    {
        var profile = new ProfileData
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = UniqueName(n => n == 1 ? "Yeni Profil" : $"Yeni Profil {n}"),
            IsEnabled = true
        };

        if (await RunAsync(() => _runtime.CreateProfileAsync(profile), profile.Id))
            InfoMessage = $"\"{profile.Name}\" oluşturuldu.";
    }

    private async Task DuplicateAsync()
    {
        if (_baseline is null)
            return;

        // Derin kopya (tuş atamaları ve kombinasyonlar dahil); asıl profille bağı kalmaz.
        var copy = CloneProfile(_baseline);
        copy.Id = Guid.NewGuid().ToString("N");
        copy.Name = UniqueName(n => n == 1 ? $"{_baseline.Name} (kopya)" : $"{_baseline.Name} (kopya {n})");

        if (await RunAsync(() => _runtime.CreateProfileAsync(copy), copy.Id))
            InfoMessage = $"\"{copy.Name}\" oluşturuldu.";
    }

    private async Task SetActiveAsync()
    {
        if (_selected is null)
            return;

        var id = _selected.Id;
        if (await RunAsync(() => _runtime.SetActiveProfileAsync(id), null))
            InfoMessage = $"\"{_selected?.Name}\" aktif profil oldu.";
    }

    private Task BeginDelete()
    {
        ClearMessages();
        if (_selected is null || Profiles.Count <= 1)
            return Task.CompletedTask;

        // FN eşleştirmesinde kullanılan profil silinemez; önce uyarılır (Runtime da reddeder).
        string? note = null;
        try
        {
            var keys = _runtime.GetSettingsSnapshot().Input.ProfileSwitchMap
                .Where(p => p.Value == _selected.Id)
                .Select(p => p.Key.ToString(CultureInfo.InvariantCulture))
                .ToList();
            if (keys.Count > 0)
            {
                note = $"Bu profil FN + {string.Join(", ", keys)} eşleştirmesinde kullanılıyor. " +
                       "Silmek için önce Ayarlar'da eşleştirmeyi değiştir.";
            }
        }
        catch
        {
            // Kontrol yapılamazsa Runtime silme sırasında yine reddeder.
        }

        DeleteBlockedNote = note;
        IsDeleteConfirmVisible = true;
        return Task.CompletedTask;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_selected is null)
            return;

        var id = _selected.Id;
        var name = _selected.Name;
        var index = Profiles.IndexOf(_selected);
        var neighbor = Profiles.Where(p => p.Id != id).ElementAtOrDefault(Math.Min(index, Profiles.Count - 2));
        var before = _activeId;

        IsDeleteConfirmVisible = false;
        if (await RunAsync(() => _runtime.DeleteProfileAsync(id), neighbor?.Id))
            InfoMessage = $"\"{name}\" silindi." + ActiveChangeNotice(before, " ");
    }

    // Doğrudan Runtime çağrısı: başarılıysa liste yenilenir ve (varsa) hedef profil seçilir. Başarıda true.
    private async Task<bool> RunAsync(Func<Task<ManagementResult>> call, string? selectAfterId)
    {
        if (IsBusy)
            return false;

        ClearMessages();
        IsBusy = true;
        _ownChange = true;
        try
        {
            var result = await call();
            if (!result.IsSuccess)
            {
                GeneralError = ResultMessage(result);
                return false;
            }

            _preferredId = selectAfterId;
            Refresh();
            return true;
        }
        catch (Exception)
        {
            GeneralError = "İşlem tamamlanamadı.";
            return false;
        }
        finally
        {
            _ownChange = false;
            IsBusy = false;
        }
    }

    // ---- Yükleme ve yenileme ----

    // Runtime'ın güncel profillerini listeye uygular; kirli taslak ezilmez.
    private void Refresh()
    {
        ProfilesData snapshot;
        try
        {
            snapshot = _runtime.GetProfilesSnapshot();
        }
        catch
        {
            return;
        }

        _profiles = snapshot.Profiles;
        _activeId = snapshot.ActiveProfileId;
        var oldIndex = _selected is null ? -1 : Profiles.IndexOf(_selected);

        for (var i = Profiles.Count - 1; i >= 0; i--)
        {
            if (!_profiles.Any(p => p.Id == Profiles[i].Id))
                Profiles.RemoveAt(i);
        }

        for (var i = 0; i < _profiles.Count; i++)
        {
            var data = _profiles[i];
            var isActive = data.Id == _activeId;
            var existing = Profiles.FirstOrDefault(r => r.Id == data.Id);
            if (existing is null)
            {
                Profiles.Insert(Math.Min(i, Profiles.Count), new ProfileRowViewModel(data, isActive));
                continue;
            }

            existing.Update(data, isActive);
            var current = Profiles.IndexOf(existing);
            if (current != i && i < Profiles.Count)
                Profiles.Move(current, i);
        }

        var preferred = _preferredId is null ? null : Profiles.FirstOrDefault(r => r.Id == _preferredId);
        _preferredId = null;

        if (preferred is not null)
        {
            SelectCore(preferred);
        }
        else if (_selected is not null && Profiles.Contains(_selected))
        {
            _baseline = _profiles.FirstOrDefault(p => p.Id == _selected.Id);
            if (!HasChanges)
                LoadDraft();
        }
        else
        {
            var hadSelection = _selected is not null;
            var next = oldIndex >= 0 && Profiles.Count > 0
                ? Profiles[Math.Min(oldIndex, Profiles.Count - 1)]
                : Profiles.FirstOrDefault(r => r.IsActive) ?? Profiles.FirstOrDefault();

            SelectCore(next);
            if (hadSelection && !_ownChange)
                InfoMessage = "Seçili profil silindi; taslak atıldı.";
        }

        RefreshState();
    }

    // Seçimi korumadan değiştirir (koruma çağıranın işidir) ve taslağı yeni profilden doldurur.
    private void SelectCore(ProfileRowViewModel? row)
    {
        _selected = row;
        _baseline = row is null ? null : _profiles.FirstOrDefault(p => p.Id == row.Id);
        IsDeleteConfirmVisible = false;
        LoadDraft();
        NameError = null;
        GeneralError = null;
        SavedMessage = null;
        OnPropertyChanged(nameof(SelectedProfile));
        OnPropertyChanged(nameof(HasSelection));
        RefreshState();
    }

    private void LoadDraft()
    {
        _draftName = _baseline?.Name ?? string.Empty;
        _draftIsEnabled = _baseline?.IsEnabled ?? false;
        OnPropertyChanged(nameof(DraftName));
        OnPropertyChanged(nameof(DraftIsEnabled));
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(HasChanges));
        OnPropertyChanged(nameof(IsSelectedActive));
        OnPropertyChanged(nameof(CanSetActive));
        OnPropertyChanged(nameof(SetActiveHint));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(DeleteHint));
        OnPropertyChanged(nameof(AssignmentSummary));
        OnPropertyChanged(nameof(DeleteConfirmText));

        SaveCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        CreateCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
        SetActiveCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ConfirmDeleteCommand.NotifyCanExecuteChanged();
    }

    // ---- Hatalar ve mesajlar ----

    private void ClearMessages()
    {
        NameError = null;
        GeneralError = null;
        InfoMessage = null;
        SavedMessage = null;
    }

    private static string ResultMessage(ManagementResult result) =>
        result.FailureMessage
        ?? string.Join(" ", result.Errors.Select(e => e.Message).Distinct());

    // Runtime hata yollarındaki "Profiles[i]" TÜM listedeki indekstir; profil Id'sine çevrilip eşlenir.
    private void ApplyResultErrors(ManagementResult result, string profileId)
    {
        if (result.FailureMessage is not null)
        {
            GeneralError = result.FailureMessage;
            return;
        }

        List<ProfileData> all;
        try
        {
            all = _runtime.GetProfilesSnapshot().Profiles;
        }
        catch
        {
            all = _profiles;
        }

        var general = new List<string>();
        foreach (var error in result.Errors)
        {
            var match = ProfileFieldPattern.Match(error.Field);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var index) && index < all.Count)
            {
                var owner = all[index];
                if (owner.Id == profileId && match.Groups[2].Value == "Name")
                    NameError = error.Message;
                else if (owner.Id == profileId)
                    general.Add(error.Message);
                else
                    general.Add($"\"{owner.Name}\" profilinde hata: {error.Message}");
            }
            else
            {
                general.Add(error.Message);
            }
        }

        GeneralError = general.Count == 0 ? null : string.Join(" ", general.Distinct());
    }

    // Aktif profil kendiliğinden başka profile geçtiyse kısa bilgi.
    private string? ActiveChangeNotice(string? before, string prefix = "")
    {
        if (before is null || before == _activeId)
            return null;

        var next = Profiles.FirstOrDefault(r => r.Id == _activeId);
        return prefix + (next is null
            ? "Etkin profil kalmadı. Bir profili etkinleştirip aktif yap."
            : $"Aktif profil \"{next.Name}\" oldu.");
    }

    // make(1) ilk aday, make(2), make(3)... sonrakiler; listede olmayan ilk ad seçilir (büyük/küçük harf duyarsız).
    private string UniqueName(Func<int, string> make)
    {
        for (var i = 1; ; i++)
        {
            var candidate = make(i);
            if (!_profiles.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
    }

    private static ProfileData CloneProfile(ProfileData source)
        => new ProfilesData { Profiles = { source } }.Clone().Profiles[0];

    private async void ShowSavedMessage()
    {
        var version = ++_savedMessageVersion;
        SavedMessage = "Profil kaydedildi.";

        await Task.Delay(SavedMessageDuration);

        if (!_disposed && version == _savedMessageVersion)
            SavedMessage = null;
    }

    // ---- Runtime olayları (arka plan thread'inden gelebilir) ----

    private void OnRuntimeChanged(object? sender, EventArgs e) => Post(Refresh);

    private void OnRuntimeActiveChanged(object? sender, string? e) => Post(Refresh);

    private void Post(Action action)
    {
        if (_disposed)
            return;

        void Run()
        {
            if (!_disposed)
                action();
        }

        if (_dispatcher.CheckAccess())
            Run();
        else
            _dispatcher.BeginInvoke(Run);
    }
}
