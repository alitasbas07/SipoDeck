using SipoDeck.Core.Actions;
using SipoDeck.Core.Devices;
using SipoDeck.Core.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime.Events;

namespace SipoDeck.Runtime;

/// <summary>
/// SipoDeck'in çalışma motoru. Ayar/profil yüklemeyi, cihaz bağlantısını, Input Engine'i ve
/// eylem kuyruğunu WPF'ten bağımsız olarak koordine eder. Cihaz bağlı olmasa veya bağlantı
/// ayarları eksik olsa bile <see cref="RuntimeState.Running"/> durumuna geçer.
/// </summary>
public sealed class AppRuntime
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);

    private readonly string? _settingsFilePath;
    private readonly string? _profilesFilePath;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly ProfileManager _profiles = new();
    private readonly DeviceManager _devices = new();

    private volatile RuntimeState _state = RuntimeState.Stopped;
    private volatile ConnectionState _deviceConnectionState = ConnectionState.Disconnected;
    private ConnectionType _connectionType;

    private ActionQueue? _actionQueue;
    private InputEngine? _inputEngine;
    private ITransport? _transport;
    private DeviceConnection? _deviceConnection;
    private ReconnectService? _reconnectService;
    private Task? _singleConnectAttempt;

    /// <param name="settingsFilePath">Ayar dosyası yolu; verilmezse varsayılan %LOCALAPPDATA% konumu kullanılır.</param>
    /// <param name="profilesFilePath">Profil dosyası yolu; verilmezse varsayılan %LOCALAPPDATA% konumu kullanılır.</param>
    public AppRuntime(string? settingsFilePath = null, string? profilesFilePath = null)
    {
        _settingsFilePath = settingsFilePath;
        _profilesFilePath = profilesFilePath;
    }

    public RuntimeState State => _state;

    /// <summary>Cihaz bağlantı durumu. Runtime durumundan ayrı izlenir.</summary>
    public ConnectionState DeviceConnectionState => _deviceConnectionState;

    public event EventHandler<RuntimeState>? StateChanged;

    public event EventHandler<DeviceConnectionStateChangedEventArgs>? DeviceConnectionStateChanged;

    public event EventHandler<DeviceSnapshot>? DeviceIdentified;

    public event EventHandler<string>? MessageRejected;

    public event EventHandler<ActionFailedEventArgs>? ActionFailed;

    /// <summary>Devam etmeyi engelleyen beklenmedik bir başlangıç hatasında tetiklenir.</summary>
    public event EventHandler<RuntimeFaultedEventArgs>? Faulted;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is RuntimeState.Starting or RuntimeState.Running)
                return;

            SetState(RuntimeState.Starting);

            try
            {
                LoadProfiles();

                var settings = new SettingsStore(_settingsFilePath).Load();

                _actionQueue = new ActionQueue();
                _actionQueue.ActionFailed += OnActionFailed;

                _inputEngine = new InputEngine(
                    _profiles,
                    _actionQueue,
                    TimeSpan.FromMilliseconds(settings.Input.LongPressThresholdMilliseconds),
                    settings.Input.FnKey,
                    settings.Input.ProfileSwitchMap);

                var transport = CreateTransport(settings.Connection);
                if (transport is not null)
                {
                    _transport = transport;
                    _connectionType = settings.Connection.ConnectionType;

                    _deviceConnection = new DeviceConnection(transport, _connectionType, _devices);
                    _deviceConnection.EventReceived += OnDeviceEventReceived;
                    _deviceConnection.DeviceIdentified += OnDeviceIdentified;
                    _deviceConnection.MessageRejected += OnMessageRejected;
                    _deviceConnection.StateChanged += OnDeviceConnectionStateChanged;

                    if (settings.Application.AutoReconnect)
                    {
                        _reconnectService = new ReconnectService(transport, ReconnectDelay);
                        _reconnectService.Start();
                    }
                    else
                    {
                        _singleConnectAttempt = ConnectOnceAsync(transport);
                    }
                }

                SetState(RuntimeState.Running);
            }
            catch (Exception ex)
            {
                await DisposeComponentsAsync().ConfigureAwait(false);
                SetState(RuntimeState.Faulted);
                Faulted?.Invoke(this, new RuntimeFaultedEventArgs(ex));
                throw;
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is RuntimeState.Stopped or RuntimeState.Stopping)
                return;

            SetState(RuntimeState.Stopping);
            await DisposeComponentsAsync().ConfigureAwait(false);
            SetState(RuntimeState.Stopped);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Çalışan kuyruğa doğrudan bir eylem gönderir. Runtime <see cref="RuntimeState.Running"/>
    /// değilse eylem sessizce yok sayılır. Test/doğrulama ve gelecekteki istemciler içindir;
    /// olağan akışta eylemler Input Engine üzerinden kuyruğa girer.
    /// </summary>
    public void Dispatch(IAction action) => _actionQueue?.Dispatch(action);

    private void LoadProfiles()
    {
        var data = new ProfileStore(_profilesFilePath).Load();
        foreach (var profileData in data.Profiles)
            _profiles.Add(profileData.ToProfile());

        if (data.ActiveProfileId is not null)
            _profiles.SwitchTo(data.ActiveProfileId);
    }

    private static ITransport? CreateTransport(ConnectionSettings connection) => connection.ConnectionType switch
    {
        ConnectionType.WiFi when !string.IsNullOrWhiteSpace(connection.Host) && connection.Port > 0
            => new WiFiTransport(new WiFiTransportSettings { Host = connection.Host, Port = connection.Port }),
        ConnectionType.Serial when !string.IsNullOrWhiteSpace(connection.SerialPortName)
            => new SerialTransport(new SerialTransportSettings
            {
                PortName = connection.SerialPortName,
                BaudRate = connection.BaudRate
            }),
        _ => null
    };

    private static async Task ConnectOnceAsync(ITransport transport)
    {
        try
        {
            await transport.ConnectAsync().ConfigureAwait(false);
        }
        catch
        {
            // Tek seferlik bağlantı denemesi başarısız oldu; DeviceConnectionStateChanged
            // zaten Disconnected bildirir. Otomatik yeniden bağlanma kapalı olduğu için
            // bir sonraki deneme kullanıcı eylemine (Task 014) bırakılır.
        }
    }

    private void OnDeviceEventReceived(object? sender, IDeviceEvent deviceEvent) => _inputEngine?.Process(deviceEvent);

    private void OnDeviceIdentified(object? sender, Device device)
        => DeviceIdentified?.Invoke(this, new DeviceSnapshot(device.Id, device.Name, device.FirmwareVersion, device.ProtocolVersion, device.ConnectionType));

    private void OnMessageRejected(object? sender, string reason) => MessageRejected?.Invoke(this, reason);

    private void OnDeviceConnectionStateChanged(object? sender, ConnectionState state)
    {
        _deviceConnectionState = state;
        DeviceConnectionStateChanged?.Invoke(this, new DeviceConnectionStateChangedEventArgs(_connectionType, state));
    }

    private void OnActionFailed(object? sender, ActionFailedEventArgs e) => ActionFailed?.Invoke(this, e);

    private async Task DisposeComponentsAsync()
    {
        _reconnectService?.Dispose();
        _reconnectService = null;

        if (_deviceConnection is not null)
        {
            _deviceConnection.EventReceived -= OnDeviceEventReceived;
            _deviceConnection.DeviceIdentified -= OnDeviceIdentified;
            _deviceConnection.MessageRejected -= OnMessageRejected;
            _deviceConnection.StateChanged -= OnDeviceConnectionStateChanged;
            _deviceConnection.Dispose();
            _deviceConnection = null;
        }

        if (_singleConnectAttempt is not null)
        {
            try
            {
                await _singleConnectAttempt.WaitAsync(ShutdownTimeout).ConfigureAwait(false);
            }
            catch
            {
                // Kapanış sırasında tek seferlik bağlantı denemesinin sonucu artık önemli değil.
            }

            _singleConnectAttempt = null;
        }

        if (_transport is not null)
        {
            try
            {
                await _transport.DisconnectAsync().WaitAsync(ShutdownTimeout).ConfigureAwait(false);
            }
            catch
            {
                // Bağlantı kapatma sırasında hata olsa bile kapanış akışı kesilmemeli.
            }

            _transport = null;
        }

        _inputEngine?.Dispose();
        _inputEngine = null;

        if (_actionQueue is not null)
        {
            _actionQueue.ActionFailed -= OnActionFailed;
            await _actionQueue.DisposeAsync().ConfigureAwait(false);
            _actionQueue = null;
        }

        _deviceConnectionState = ConnectionState.Disconnected;
    }

    private void SetState(RuntimeState state)
    {
        if (_state == state)
            return;

        _state = state;
        StateChanged?.Invoke(this, state);
    }
}
