using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Runtime;
using SipoDeck.Runtime.Management;
using SipoDeck.Runtime.Events;
using SipoDeck.Core.Transport;

namespace SipoDeck.ViewModels;

public sealed partial class KeysViewModel : ObservableObject, IUnsavedChangesGuard, IDisposable, IDeactivateAware
{
    private readonly AppRuntime _runtime;
    private readonly Dispatcher _dispatcher;

    private string _profileId = string.Empty;
    private ProfilesData? _snapshot;
    private ProfileData? _profileData;

    [ObservableProperty]
    private int _gridRows;
    [ObservableProperty]
    private int _gridCols;

    public ObservableCollection<KeyViewModel> Keys { get; } = new();
    public ObservableCollection<CombinationViewModel> Combinations { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeySelected))]
    private KeyViewModel? _selectedKey;

    public bool HasNoKeys => Keys.Count == 0;
    public string InterceptionButtonText => IsInterceptionModeActive ? "Seçim modunu iptal et" : "Fiziksel Tuşla Seç";
    public bool IsKeySelected => SelectedKey is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _isDirty;

    public bool HasChanges => IsDirty;
    public string UnsavedChangesMessage => "Tuş atamaları kaydedilmedi. Vazgeçmek istiyor musunuz?";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterceptionButtonText))]
    private bool _isInterceptionModeActive;

    [ObservableProperty]
    private string? _statusMessage;

    public KeysViewModel(AppRuntime runtime, Dispatcher dispatcher)
    {
        _runtime = runtime;
        _dispatcher = dispatcher;

        _runtime.KeyIntercepted += OnKeyIntercepted;
        _runtime.DeviceConnectionStateChanged += OnDeviceStateChanged;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasChanges);
        CancelCommand = new RelayCommand(Discard, () => HasChanges);
        ClearKeyCommand = new RelayCommand(ClearKey, () => SelectedKey is not null);
        CopyKeyCommand = new RelayCommand(CopyKey, () => SelectedKey is not null && CopyTarget is not null);
        ToggleInterceptionCommand = new RelayCommand(ToggleInterception);
        SelectKeyCommand = new RelayCommand<KeyViewModel>(k => SelectedKey = k);
        AddCombinationCommand = new RelayCommand(AddCombination);
        DeleteCombinationCommand = new RelayCommand<CombinationViewModel>(DeleteCombination);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ClearKeyCommand { get; }
    public IRelayCommand CopyKeyCommand { get; }

    [ObservableProperty]
    private KeyViewModel? _copyTarget;

    partial void OnCopyTargetChanged(KeyViewModel? value) => CopyKeyCommand.NotifyCanExecuteChanged();
    partial void OnSelectedKeyChanged(KeyViewModel? value)
    {
        ClearKeyCommand.NotifyCanExecuteChanged();
        CopyKeyCommand.NotifyCanExecuteChanged();
    }

    private void CopyKey()
    {
        if (SelectedKey is null || CopyTarget is null || CopyTarget == SelectedKey) return;
        if (CopyTarget.IsFnKey)
        {
            StatusMessage = "Hata: FN tuşuna eylem kopyalanamaz.";
            return;
        }
        CopyTarget.SetActions(SelectedKey.Action, SelectedKey.LongPressAction);
        StatusMessage = $"Tuş {SelectedKey.Key} ataması Tuş {CopyTarget.Key} üzerine kopyalandı.";
    }
    public IRelayCommand ToggleInterceptionCommand { get; }
    public IRelayCommand<KeyViewModel> SelectKeyCommand { get; }
    public IRelayCommand AddCombinationCommand { get; }
    public IRelayCommand<CombinationViewModel> DeleteCombinationCommand { get; }

    private void AddCombination()
    {
        var combo = new CombinationConfig { Action = null };
        var cvm = new CombinationViewModel(combo, _runtime.GetActionCatalog(), GetProfileOptions);
        cvm.PropertyChanged += (s, e) => IsDirty = true;
        cvm.Keys.CollectionChanged += (s, e) => IsDirty = true;
        Combinations.Add(cvm);
        IsDirty = true;
    }

    private void DeleteCombination(CombinationViewModel? combo)
    {
        if (combo != null && Combinations.Remove(combo))
        {
            IsDirty = true;
        }
    }

    public void LoadProfile(string profileId)
    {
        // Stop interception if active
        if (IsInterceptionModeActive) ToggleInterception();

        _profileId = profileId;
        IsDirty = false;
        StatusMessage = null;
        SelectedKey = null;
        
        var result = _runtime.GetProfilesSnapshot();
        if (result is not null)
        {
            _snapshot = result;
            _profileData = _snapshot.Profiles.FirstOrDefault(p => p.Id == profileId);
        }

        if (_profileData is null)
        {
            Keys.Clear();
            Combinations.Clear();
            OnPropertyChanged(nameof(HasNoKeys));
            return;
        }

        var device = _runtime.GetDeviceSnapshot();
        int keyCount = device?.KeyCount ?? 0;
        
        if (keyCount == 0)
        {
            // If device disconnected or no key info, fallback to existing keys in profile max
            keyCount = _profileData.Keys.Keys.Count > 0 ? _profileData.Keys.Keys.Max() : 0;
        }

        GridCols = device?.Cols ?? (keyCount > 0 ? (int)Math.Ceiling(Math.Sqrt(keyCount)) : 1);
        GridRows = device?.Rows ?? (keyCount > 0 ? (int)Math.Ceiling((double)keyCount / GridCols) : 1);

        int fnKey = -1;
        var appSettings = _runtime.GetSettingsSnapshot();
        fnKey = appSettings?.Input.FnKey ?? -1;

        Keys.Clear();
        for (int i = 1; i <= keyCount; i++)
        {
            _profileData.Keys.TryGetValue(i, out var config);
            var kvm = new KeyViewModel(i, config, i == fnKey, _runtime.GetActionCatalog(), GetProfileOptions);
            kvm.PropertyChanged += (s, e) => IsDirty = true;
            Keys.Add(kvm);
        }
        OnPropertyChanged(nameof(HasNoKeys));

        Combinations.Clear();
        foreach (var combo in _profileData.Combinations)
        {
            var cvm = new CombinationViewModel(combo, _runtime.GetActionCatalog(), GetProfileOptions);
            cvm.PropertyChanged += (s, e) => IsDirty = true;
            cvm.Keys.CollectionChanged += (s, e) => IsDirty = true;
            Combinations.Add(cvm);
        }
    }

    private IReadOnlyList<ProfileOption> GetProfileOptions() =>
        _runtime.GetProfilesSnapshot().Profiles.Select(p => new ProfileOption(p.Id, p.Name)).ToList();

    public async Task<bool> SaveAsync()
    {
        if (!HasChanges || _snapshot is null || _profileData is null)
            return true;

        if (IsInterceptionModeActive) ToggleInterception();

        // 1. Validate FN Key (Cannot have single/long-press assignment)
        var fnKeyVm = Keys.FirstOrDefault(k => k.IsFnKey);
        if (fnKeyVm != null && (fnKeyVm.Action != null || fnKeyVm.LongPressAction != null))
        {
            StatusMessage = "Hata: FN tuşuna normal veya uzun basma eylemi atanamaz.";
            return false;
        }

        // 2. Validate Combinations
        var combinationKeySets = new HashSet<string>();
        foreach (var combo in Combinations)
        {
            if (combo.Keys.Count < 2)
            {
                StatusMessage = "Hata: Kombinasyonlar en az 2 tuş içermelidir.";
                return false;
            }
            var sortedKeys = string.Join(",", combo.Keys.OrderBy(k => k));
            if (!combinationKeySets.Add(sortedKeys))
            {
                StatusMessage = "Hata: Aynı tuşlardan oluşan birden fazla kombinasyon olamaz.";
                return false;
            }
        }

        // Update current layout keys
        foreach (var k in Keys)
        {
            if (k.Action is null && k.LongPressAction is null)
                _profileData.Keys.Remove(k.Key);
            else
                _profileData.Keys[k.Key] = k.ToConfig();
        }

        // We DO NOT remove existing keys that are outside the current device layout, to preserve them (Requirement).
        // Combinations
        _profileData.Combinations = Combinations.Select(c => c.ToConfig()).ToList();

        var res = await _runtime.UpdateProfileAsync(_profileData);
        if (res.IsSuccess)
        {
            IsDirty = false;
            StatusMessage = "Kaydedildi.";
            SaveCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
            return true;
        }
        else
        {
            StatusMessage = (res.FailureMessage ?? res.Errors.FirstOrDefault()?.Message) ?? "Hata oluştu.";
            return false;
        }
    }

    public void Discard()
    {
        if (IsInterceptionModeActive) ToggleInterception();
        LoadProfile(_profileId);
    }

    private void ClearKey()
    {
        if (SelectedKey is not null)
        {
            SelectedKey.SetActions(null, null);
            IsDirty = true;
        }
    }

    private void ToggleInterception()
    {
        IsInterceptionModeActive = !IsInterceptionModeActive;
        _runtime.IsKeyInterceptionEnabled = IsInterceptionModeActive;
        
        if (IsInterceptionModeActive)
            StatusMessage = "Cihazdan tuşa basmanız bekleniyor...";
        else
            StatusMessage = null;
    }

    private void OnKeyIntercepted(object? sender, KeyInterceptedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (IsInterceptionModeActive)
            {
                var kvm = Keys.FirstOrDefault(k => k.Key == e.Key);
                if (kvm != null)
                    SelectedKey = kvm;
                
                // Kapat
                ToggleInterception();
                StatusMessage = $"Tuş {e.Key} seçildi.";
            }
        });
    }

    private void OnDeviceStateChanged(object? sender, DeviceConnectionStateChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (e.State != ConnectionState.Connected && IsInterceptionModeActive)
            {
                ToggleInterception();
                StatusMessage = "Cihaz bağlantısı koptuğu için seçim modu iptal edildi.";
            }
        });
    }

    public void OnDeactivated()
    {
        if (IsInterceptionModeActive)
        {
            ToggleInterception();
        }
    }

    public void Dispose()
    {
        if (IsInterceptionModeActive)
        {
            _runtime.IsKeyInterceptionEnabled = false;
        }
        _runtime.KeyIntercepted -= OnKeyIntercepted;
        _runtime.DeviceConnectionStateChanged -= OnDeviceStateChanged;
    }
}









