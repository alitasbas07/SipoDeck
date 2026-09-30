using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime;
using SipoDeck.Runtime.Events;

namespace SipoDeck.ViewModels;

/// <summary>
/// Runtime durumunun arayüz görünümü. Runtime olayları arka plan thread'lerinden gelir;
/// tüm güncellemeler Dispatcher üzerinden yapılır. IP/port/COM bilgisi tutulmaz.
/// </summary>
public sealed class StatusViewModel : ObservableObject, IDisposable
{
    private const int TrayTooltipMaxLength = 63;
    private const string NoProfileName = "—";
    private const string GenericFaultMessage = "Çalışma motorunda beklenmeyen bir sorun oluştu. Uygulamayı yeniden başlatmayı dene.";

    private readonly AppRuntime _runtime;
    private readonly Dispatcher _dispatcher;
    private bool _disposed;

    private RuntimeState _runtimeState;
    private ConnectionState _deviceConnectionState;
    private ConnectionType? _connectionType;
    private string? _deviceName;
    private string? _firmwareVersion;
    private string _activeProfileName;
    private bool _hasFault;
    private string? _faultMessage;

    public StatusViewModel(AppRuntime runtime, Dispatcher dispatcher)
    {
        _runtime = runtime;
        _dispatcher = dispatcher;

        _runtimeState = runtime.State;
        _deviceConnectionState = runtime.DeviceConnectionState;
        _hasFault = _runtimeState == RuntimeState.Faulted;
        _faultMessage = _hasFault ? GenericFaultMessage : null;
        _activeProfileName = ReadActiveProfileName();

        runtime.StateChanged += OnStateChanged;
        runtime.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;
        runtime.DeviceIdentified += OnDeviceIdentified;
        runtime.Faulted += OnFaulted;
        runtime.ActiveProfileChanged += OnActiveProfileChanged;
        runtime.ProfilesChanged += OnProfilesChanged;
        runtime.SettingsChanged += OnSettingsChanged;
    }

    public RuntimeState RuntimeState => _runtimeState;

    public string RuntimeStateText => StatusText.ForRuntime(_runtimeState);

    public ConnectionState DeviceConnectionState => _deviceConnectionState;

    public string ConnectionStateText => StatusText.ForConnection(_deviceConnectionState);

    /// <summary>"Wi-Fi" / "USB"; yalnızca bağlıyken ve tür biliniyorsa dolu, aksi halde null.</summary>
    public string? ConnectionTypeText =>
        _deviceConnectionState == ConnectionState.Connected && _connectionType is { } type
            ? StatusText.ForConnectionType(type)
            : null;

    public string? DeviceName => _deviceName;

    public string? FirmwareVersion => _firmwareVersion;

    public string DeviceSummaryText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_deviceName))
                return "Cihaz bağlı değil";

            if (string.IsNullOrWhiteSpace(_firmwareVersion))
                return _deviceName;

            var version = _firmwareVersion.StartsWith('v') || _firmwareVersion.StartsWith('V')
                ? _firmwareVersion
                : "v" + _firmwareVersion;
            return $"{_deviceName} · {version}";
        }
    }

    public string ActiveProfileName => _activeProfileName;

    /// <summary>Runtime'da başarılı eylem olayı bulunmadığından şimdilik her zaman null.</summary>
    public string? LastActionText => null;

    public bool HasFault => _hasFault;

    public string? FaultMessage => _faultMessage;

    /// <summary>NotifyIcon.Text sınırı (63 karakter) gözetilerek üretilir.</summary>
    public string TrayTooltipText
    {
        get
        {
            var text = $"SipoDeck · {ConnectionStateText} · {RuntimeStateText}";
            return text.Length <= TrayTooltipMaxLength ? text : text[..TrayTooltipMaxLength];
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _runtime.StateChanged -= OnStateChanged;
        _runtime.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;
        _runtime.DeviceIdentified -= OnDeviceIdentified;
        _runtime.Faulted -= OnFaulted;
        _runtime.ActiveProfileChanged -= OnActiveProfileChanged;
        _runtime.ProfilesChanged -= OnProfilesChanged;
        _runtime.SettingsChanged -= OnSettingsChanged;
    }

    private void OnStateChanged(object? sender, RuntimeState state) => Post(() =>
    {
        _runtimeState = state;
        _hasFault = state == RuntimeState.Faulted;
        _faultMessage = _hasFault ? GenericFaultMessage : null;

        OnPropertyChanged(nameof(RuntimeState));
        OnPropertyChanged(nameof(RuntimeStateText));
        OnPropertyChanged(nameof(HasFault));
        OnPropertyChanged(nameof(FaultMessage));
        OnPropertyChanged(nameof(TrayTooltipText));
    });

    private void OnDeviceConnectionStateChanged(object? sender, DeviceConnectionStateChangedEventArgs args) => Post(() =>
    {
        _deviceConnectionState = args.State;
        _connectionType = args.ConnectionType;

        OnPropertyChanged(nameof(DeviceConnectionState));
        OnPropertyChanged(nameof(ConnectionStateText));
        OnPropertyChanged(nameof(ConnectionTypeText));
        OnPropertyChanged(nameof(TrayTooltipText));
    });

    private void OnDeviceIdentified(object? sender, DeviceSnapshot device) => Post(() =>
    {
        _deviceName = string.IsNullOrWhiteSpace(device.Name) ? null : device.Name;
        _firmwareVersion = string.IsNullOrWhiteSpace(device.FirmwareVersion) ? null : device.FirmwareVersion;
        _connectionType = device.ConnectionType;

        OnPropertyChanged(nameof(DeviceName));
        OnPropertyChanged(nameof(FirmwareVersion));
        OnPropertyChanged(nameof(DeviceSummaryText));
        OnPropertyChanged(nameof(ConnectionTypeText));
    });

    // İstisna detayı/yol arayüze taşınmaz; yalnızca genel mesaj gösterilir.
    private void OnFaulted(object? sender, RuntimeFaultedEventArgs args) => Post(() =>
    {
        _hasFault = true;
        _faultMessage = GenericFaultMessage;

        OnPropertyChanged(nameof(HasFault));
        OnPropertyChanged(nameof(FaultMessage));
    });

    private void OnActiveProfileChanged(object? sender, string? profileId) => Post(RefreshActiveProfile);

    private void OnProfilesChanged(object? sender, EventArgs args) => Post(RefreshActiveProfile);

    private void OnSettingsChanged(object? sender, EventArgs args)
    {
        // Durum alanı ayarlardan etkilenmez; Windows başlangıç ayarını App işler.
    }

    private void RefreshActiveProfile()
        => SetProperty(ref _activeProfileName, ReadActiveProfileName(), nameof(ActiveProfileName));

    private string ReadActiveProfileName()
    {
        try
        {
            var data = _runtime.GetProfilesSnapshot();
            var profile = data.Profiles.FirstOrDefault(p => p.Id == data.ActiveProfileId);
            return string.IsNullOrWhiteSpace(profile?.Name) ? NoProfileName : profile.Name;
        }
        catch
        {
            return NoProfileName;
        }
    }

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
