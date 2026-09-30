using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime;
using SipoDeck.Runtime.Management;

namespace SipoDeck.ViewModels;

/// <summary>
/// Ayarlar sayfası. Form, Runtime'dan okunan ayarların taslak kopyasıdır; yalnızca Kaydet ile
/// Runtime yönetim API'sine gider. Bu ekranda kullanıcı kendi girdiği Host/Port/COM'u görür ve düzenler;
/// bu değerler loglanmaz.
/// </summary>
public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    private const string NumberError = "Geçerli bir sayı girin.";
    private const string ProfileKeyPrefix = "Input.ProfileSwitchMap[";
    private static readonly TimeSpan SavedMessageDuration = TimeSpan.FromSeconds(4);

    // Doğrulama alan yolu → hata özelliği adı.
    private static readonly Dictionary<string, string> ErrorProperties = new()
    {
        ["Connection.Host"] = nameof(HostError),
        ["Connection.Port"] = nameof(PortError),
        ["Connection.SerialPortName"] = nameof(SerialPortNameError),
        ["Connection.BaudRate"] = nameof(BaudRateError),
        ["Input.FnKey"] = nameof(FnKeyError),
        ["Input.LongPressThresholdMilliseconds"] = nameof(LongPressError)
    };

    private readonly AppRuntime _runtime;
    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<string, string> _errors = new();
    private AppSettings _baseline = new();
    private string _baselineKey = string.Empty;
    private bool _disposed;
    private int _savedMessageVersion;

    private int _connectionTypeIndex;
    private string _host = string.Empty;
    private string _portText = string.Empty;
    private string _serialPortName = string.Empty;
    private string _baudRateText = string.Empty;
    private bool _runAtStartup;
    private bool _runInSystemTray;
    private bool _autoReconnect;
    private string _fnKeyText = string.Empty;
    private string _longPressText = string.Empty;

    private bool _isSaving;
    private bool _isConnecting;
    private string? _generalError;
    private string? _savedMessage;
    private string? _connectMessage;

    public SettingsViewModel(AppRuntime runtime, Dispatcher dispatcher, StatusViewModel status)
    {
        _runtime = runtime;
        _dispatcher = dispatcher;
        Status = status;

        SaveCommand = new AsyncRelayCommand(() => SaveAsync(), () => HasChanges && !IsSaving);
        CancelCommand = new RelayCommand(Discard, () => HasChanges && !IsSaving);
        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        ConnectNowCommand = new AsyncRelayCommand(ConnectNowAsync, () => !IsConnecting);
        AddRowCommand = new RelayCommand(AddRow);

        FnMappings.CollectionChanged += OnRowsChanged;

        RefreshProfileOptions();
        Load(_runtime.GetSettingsSnapshot());

        runtime.SettingsChanged += OnRuntimeSettingsChanged;
        runtime.ProfilesChanged += OnRuntimeProfilesChanged;
    }

    public StatusViewModel Status { get; }

    public ObservableCollection<string> PortNames { get; } = new();

    public ObservableCollection<ProfileOption> ProfileOptions { get; } = new();

    public ObservableCollection<FnMappingRowViewModel> FnMappings { get; } = new();

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand RefreshPortsCommand { get; }

    public IAsyncRelayCommand ConnectNowCommand { get; }

    public IRelayCommand AddRowCommand { get; }

    // ---- Bağlantı ----

    /// <summary>0 = Wi-Fi, 1 = USB Serial (ComboBox.SelectedIndex).</summary>
    public int ConnectionTypeIndex
    {
        get => _connectionTypeIndex;
        set
        {
            if (value < 0 || value > 1)
                return;

            if (SetFormField(ref _connectionTypeIndex, value))
            {
                OnPropertyChanged(nameof(IsWiFi));
                OnPropertyChanged(nameof(IsSerial));
            }
        }
    }

    public bool IsWiFi => _connectionTypeIndex == 0;

    public bool IsSerial => _connectionTypeIndex == 1;

    public string Host
    {
        get => _host;
        set => SetFormField(ref _host, value ?? string.Empty, "Connection.Host");
    }

    public string PortText
    {
        get => _portText;
        set => SetFormField(ref _portText, value ?? string.Empty, "Connection.Port");
    }

    public string? SerialPortName
    {
        get => _serialPortName;
        set
        {
            // Liste yenilenirken ComboBox null yazabilir; seçim bu yolla silinmez.
            if (value is not null)
                SetFormField(ref _serialPortName, value, "Connection.SerialPortName");
        }
    }

    public string BaudRateText
    {
        get => _baudRateText;
        set => SetFormField(ref _baudRateText, value ?? string.Empty, "Connection.BaudRate");
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set
        {
            if (SetProperty(ref _isConnecting, value))
                ConnectNowCommand.NotifyCanExecuteChanged();
        }
    }

    public string? ConnectMessage
    {
        get => _connectMessage;
        private set => SetProperty(ref _connectMessage, value);
    }

    // ---- Uygulama ----

    public bool RunAtStartup
    {
        get => _runAtStartup;
        set => SetFormField(ref _runAtStartup, value);
    }

    public bool RunInSystemTray
    {
        get => _runInSystemTray;
        set => SetFormField(ref _runInSystemTray, value);
    }

    public bool AutoReconnect
    {
        get => _autoReconnect;
        set => SetFormField(ref _autoReconnect, value);
    }

    // ---- Girdi ----

    public string FnKeyText
    {
        get => _fnKeyText;
        set => SetFormField(ref _fnKeyText, value ?? string.Empty, "Input.FnKey");
    }

    public string LongPressText
    {
        get => _longPressText;
        set => SetFormField(ref _longPressText, value ?? string.Empty, "Input.LongPressThresholdMilliseconds");
    }

    public bool HasNoFnMappings => FnMappings.Count == 0;

    // ---- Durum / hatalar ----

    public bool HasChanges => BuildKey() != _baselineKey;

    /// <summary>Kaydedilmemiş taslak varken "Şimdi bağlan" kayıtlı ayarları kullanır; bunu belirtmek için.</summary>
    public bool ConnectUsesSavedNote => HasChanges;

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
                RefreshCommands();
        }
    }

    public string? HostError => GetError("Connection.Host");

    public string? PortError => GetError("Connection.Port");

    public string? SerialPortNameError => GetError("Connection.SerialPortName");

    public string? BaudRateError => GetError("Connection.BaudRate");

    public string? FnKeyError => GetError("Input.FnKey");

    public string? LongPressError => GetError("Input.LongPressThresholdMilliseconds");

    /// <summary>Alana bağlanamayan hata veya beklenmedik kayıt hatası.</summary>
    public string? GeneralError
    {
        get => _generalError;
        private set => SetProperty(ref _generalError, value);
    }

    /// <summary>Başarılı kayıttan sonra kısa süre gösterilen onay.</summary>
    public string? SavedMessage
    {
        get => _savedMessage;
        private set => SetProperty(ref _savedMessage, value);
    }

    /// <summary>Taslağı Runtime'daki ayarlara döndürür (İptal / Vazgeç).</summary>
    public void Discard()
    {
        Load(_runtime.GetSettingsSnapshot());
        SavedMessage = null;
    }

    /// <summary>Taslağı doğrulayıp kaydeder. Başarılıysa true; hata varsa alanların yanında gösterilir.</summary>
    public async Task<bool> SaveAsync()
    {
        if (IsSaving)
            return false;

        ClearErrors();
        SavedMessage = null;

        if (!TryBuildDraft(out var draft))
            return false;

        IsSaving = true;
        try
        {
            var result = await _runtime.UpdateSettingsAsync(draft);
            if (result.IsSuccess)
            {
                Load(_runtime.GetSettingsSnapshot());
                ShowSavedMessage();
                return true;
            }

            ApplyResultErrors(result);
            return false;
        }
        catch (Exception)
        {
            GeneralError = "Ayarlar kaydedilemedi.";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _runtime.SettingsChanged -= OnRuntimeSettingsChanged;
        _runtime.ProfilesChanged -= OnRuntimeProfilesChanged;
    }

    // ---- Yükleme ----

    private void Load(AppSettings settings)
    {
        _baseline = settings;

        _connectionTypeIndex = settings.Connection.ConnectionType == ConnectionType.Serial ? 1 : 0;
        _host = settings.Connection.Host;
        _portText = settings.Connection.Port.ToString(CultureInfo.InvariantCulture);
        _serialPortName = settings.Connection.SerialPortName;
        _baudRateText = settings.Connection.BaudRate.ToString(CultureInfo.InvariantCulture);
        _runAtStartup = settings.Application.RunAtStartup;
        _runInSystemTray = settings.Application.RunInSystemTray;
        _autoReconnect = settings.Application.AutoReconnect;
        _fnKeyText = settings.Input.FnKey.ToString(CultureInfo.InvariantCulture);
        _longPressText = settings.Input.LongPressThresholdMilliseconds.ToString(CultureInfo.InvariantCulture);

        FnMappings.CollectionChanged -= OnRowsChanged;
        foreach (var row in FnMappings)
            row.PropertyChanged -= OnRowPropertyChanged;
        FnMappings.Clear();
        foreach (var (key, profileId) in settings.Input.ProfileSwitchMap.OrderBy(p => p.Key))
            AttachRow(new FnMappingRowViewModel(key.ToString(CultureInfo.InvariantCulture), profileId, RemoveRow));
        FnMappings.CollectionChanged += OnRowsChanged;

        _errors.Clear();
        GeneralError = null;
        RefreshPorts();

        _baselineKey = BuildKey();
        OnPropertyChanged(string.Empty);
        RefreshCommands();
    }

    // ---- Seri port ----

    private void RefreshPorts()
    {
        IReadOnlyList<string> found;
        try
        {
            found = _runtime.GetAvailablePortNames();
        }
        catch
        {
            found = Array.Empty<string>();
        }

        var wanted = found.ToList();
        if (!string.IsNullOrWhiteSpace(_serialPortName) && !wanted.Contains(_serialPortName))
            wanted.Add(_serialPortName);

        // Yerinde güncelleme: ComboBox seçimi sıfırlanmaz.
        for (var i = PortNames.Count - 1; i >= 0; i--)
        {
            if (!wanted.Contains(PortNames[i]))
                PortNames.RemoveAt(i);
        }

        foreach (var name in wanted)
        {
            if (!PortNames.Contains(name))
                PortNames.Add(name);
        }
    }

    // ---- FN tablosu ----

    private void AddRow() =>
        AttachRow(new FnMappingRowViewModel(string.Empty, ProfileOptions.FirstOrDefault()?.Id, RemoveRow));

    private void AttachRow(FnMappingRowViewModel row)
    {
        row.PropertyChanged += OnRowPropertyChanged;
        FnMappings.Add(row);
    }

    private void RemoveRow(FnMappingRowViewModel row)
    {
        row.PropertyChanged -= OnRowPropertyChanged;
        FnMappings.Remove(row);
    }

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoFnMappings));
        OnPropertyChanged(nameof(HasChanges));
        RefreshCommands();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FnMappingRowViewModel.KeyText) or nameof(FnMappingRowViewModel.ProfileId))
        {
            OnPropertyChanged(nameof(HasChanges));
            RefreshCommands();
        }
    }

    private void RefreshProfileOptions()
    {
        List<ProfileOption> wanted;
        try
        {
            wanted = _runtime.GetProfilesSnapshot().Profiles
                .Select(p => new ProfileOption(p.Id, string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name))
                .ToList();
        }
        catch
        {
            wanted = new List<ProfileOption>();
        }

        if (wanted.SequenceEqual(ProfileOptions))
            return;

        ProfileOptions.Clear();
        foreach (var option in wanted)
            ProfileOptions.Add(option);

        foreach (var row in FnMappings)
            row.RefreshProfile();
    }

    // ---- Taslak → AppSettings ----

    private bool TryBuildDraft(out AppSettings draft)
    {
        draft = _baseline.Clone();
        var ok = true;

        draft.Connection.ConnectionType = IsSerial ? ConnectionType.Serial : ConnectionType.WiFi;
        draft.Connection.Host = _host.Trim();
        draft.Connection.SerialPortName = _serialPortName;

        // Gizli bağlantı türünün sayı alanı okunamazsa eski değer korunur; görünür alanda hata gösterilir.
        if (TryParse(_portText, out var port))
            draft.Connection.Port = port;
        else if (IsWiFi)
            ok = SetError("Connection.Port", NumberError);

        if (TryParse(_baudRateText, out var baud))
            draft.Connection.BaudRate = baud;
        else if (IsSerial)
            ok = SetError("Connection.BaudRate", NumberError);

        draft.Application.RunAtStartup = _runAtStartup;
        draft.Application.RunInSystemTray = _runInSystemTray;
        draft.Application.AutoReconnect = _autoReconnect;

        if (TryParse(_fnKeyText, out var fnKey))
            draft.Input.FnKey = fnKey;
        else
            ok = SetError("Input.FnKey", NumberError);

        if (TryParse(_longPressText, out var longPress))
            draft.Input.LongPressThresholdMilliseconds = longPress;
        else
            ok = SetError("Input.LongPressThresholdMilliseconds", NumberError);

        var map = new Dictionary<int, string>();
        foreach (var row in FnMappings)
        {
            row.Error = null;

            if (!TryParse(row.KeyText, out var key))
            {
                row.Error = "Geçerli bir tuş numarası girin.";
                ok = false;
            }
            else if (string.IsNullOrWhiteSpace(row.ProfileId))
            {
                row.Error = "Bir profil seçin.";
                ok = false;
            }
            else if (!map.TryAdd(key, row.ProfileId))
            {
                row.Error = "Bu tuş zaten eşleştirilmiş.";
                ok = false;
            }
        }

        draft.Input.ProfileSwitchMap = map;
        return ok;
    }

    private void ApplyResultErrors(ManagementResult result)
    {
        if (result.FailureMessage is not null)
        {
            GeneralError = result.FailureMessage;
            return;
        }

        var general = new List<string>();
        foreach (var error in result.Errors)
        {
            if (ErrorProperties.ContainsKey(error.Field))
            {
                SetError(error.Field, error.Message);
                continue;
            }

            var row = FindRow(error.Field);
            if (row is not null)
                row.Error = error.Message;
            else
                general.Add(error.Message);
        }

        GeneralError = general.Count == 0 ? null : string.Join(" ", general);
    }

    // "Input.ProfileSwitchMap[3]" → tuş numarası 3 olan satır.
    private FnMappingRowViewModel? FindRow(string field)
    {
        if (!field.StartsWith(ProfileKeyPrefix, StringComparison.Ordinal) || !field.EndsWith(']'))
            return null;

        return TryParse(field[ProfileKeyPrefix.Length..^1], out var key)
            ? FnMappings.FirstOrDefault(r => TryParse(r.KeyText, out var k) && k == key)
            : null;
    }

    // ---- Değişiklik takibi ve hatalar ----

    private bool SetFormField<T>(ref T field, T value, string? errorField = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
            return false;

        if (errorField is not null)
            ClearError(errorField);

        SavedMessage = null;
        OnPropertyChanged(nameof(HasChanges));
        RefreshCommands();
        return true;
    }

    private string BuildKey() => string.Join('\u001f', new object?[]
    {
        _connectionTypeIndex, _host, _portText, _serialPortName, _baudRateText,
        _runAtStartup, _runInSystemTray, _autoReconnect, _fnKeyText, _longPressText,
        string.Join('\u001e', FnMappings.Select(r => r.KeyText + "\u001d" + r.ProfileId))
    });

    private void RefreshCommands()
    {
        SaveCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ConnectUsesSavedNote));
    }

    private string? GetError(string field) => _errors.TryGetValue(field, out var message) ? message : null;

    private bool SetError(string field, string message)
    {
        _errors[field] = message;
        RaiseErrorChanged(field);
        return false;
    }

    private void ClearError(string field)
    {
        if (_errors.Remove(field))
            RaiseErrorChanged(field);
    }

    private void ClearErrors()
    {
        var fields = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var field in fields)
            RaiseErrorChanged(field);

        GeneralError = null;
        foreach (var row in FnMappings)
            row.Error = null;
    }

    private void RaiseErrorChanged(string field)
    {
        if (ErrorProperties.TryGetValue(field, out var property))
            OnPropertyChanged(property);
    }

    private static bool TryParse(string text, out int value)
        => int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    // ---- Bağlantı ----

    private async Task ConnectNowAsync()
    {
        IsConnecting = true;
        ConnectMessage = null;
        try
        {
            await _runtime.ReconnectAsync();
        }
        catch (Exception)
        {
            ConnectMessage = "Bağlantı başlatılamadı.";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async void ShowSavedMessage()
    {
        var version = ++_savedMessageVersion;
        SavedMessage = "Ayarlar kaydedildi.";

        await Task.Delay(SavedMessageDuration);

        if (!_disposed && version == _savedMessageVersion)
            SavedMessage = null;
    }

    // ---- Runtime olayları (arka plan thread'inden gelebilir) ----

    private void OnRuntimeSettingsChanged(object? sender, EventArgs e) => Post(() =>
    {
        // Kaydedilmemiş taslak varken kullanıcının girdisi ezilmez.
        if (!HasChanges && !IsSaving)
            Load(_runtime.GetSettingsSnapshot());
    });

    private void OnRuntimeProfilesChanged(object? sender, EventArgs e) => Post(RefreshProfileOptions);

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
