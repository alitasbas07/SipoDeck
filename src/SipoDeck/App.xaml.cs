using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using SipoDeck.Core.Devices;
using SipoDeck.Core.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;

namespace SipoDeck;

/// <summary>
/// Interaction logic for App.xaml
/// Uygulama yaşam döngüsünü yönetir: ana pencere kapatıldığında sistem tepsisine
/// küçültülür, tepsiden geri açılabilir ve yalnızca "Tamamen Kapat" ile sonlanır.
/// Başlangıçta ayarlardaki bağlantıyla cihaz iletişimini başlatır ve cihaz olaylarını
/// Input Engine'e aktarır.
/// </summary>
public partial class App
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(2);

    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    private InputEngine? _inputEngine;
    private ITransport? _transport;
    private DeviceConnection? _deviceConnection;
    private ReconnectService? _reconnectService;

    public ApplicationState State { get; private set; } = ApplicationState.Starting;

    public AppSettings Settings { get; private set; } = new();

    public ProfileManager Profiles { get; } = new();

    public DeviceManager Devices { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Settings = new SettingsStore().Load();
        WindowsStartup.Apply(Settings.Application.RunAtStartup);

        LoadProfiles();
        StartDeviceCommunication();

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;

        _trayIcon = new TrayIcon(onShow: ShowMainWindow, onExit: ExitApplication);

        _mainWindow.Show();
        State = ApplicationState.Running;
    }

    private void LoadProfiles()
    {
        var data = new ProfileStore().Load();
        foreach (var profileData in data.Profiles)
            Profiles.Add(profileData.ToProfile());

        if (data.ActiveProfileId is not null)
            Profiles.SwitchTo(data.ActiveProfileId);
    }

    private void StartDeviceCommunication()
    {
        _transport = CreateTransport(Settings.Connection);
        if (_transport is null)
        {
            Debug.WriteLine("[SipoDeck] Bağlantı ayarları eksik; cihaz iletişimi başlatılmadı.");
            return;
        }

        _inputEngine = new InputEngine(
            Profiles,
            TimeSpan.FromMilliseconds(Settings.Input.LongPressThresholdMilliseconds),
            Settings.Input.FnKey,
            Settings.Input.ProfileSwitchMap);

        _deviceConnection = new DeviceConnection(_transport, Settings.Connection.ConnectionType, Devices);
        _deviceConnection.EventReceived += (_, deviceEvent) => _inputEngine.Process(deviceEvent);
        _deviceConnection.DeviceIdentified += (_, device) =>
            Debug.WriteLine($"[SipoDeck] Cihaz tanındı: {device.Name} ({device.Id}), firmware {device.FirmwareVersion}");
        _deviceConnection.MessageRejected += (_, reason) =>
            Debug.WriteLine($"[SipoDeck] Mesaj reddedildi: {reason}");
        _deviceConnection.StateChanged += (_, state) =>
            Debug.WriteLine($"[SipoDeck] {_transport.Name}: {state}");

        if (Settings.Application.AutoReconnect)
        {
            _reconnectService = new ReconnectService(_transport, ReconnectDelay);
            _reconnectService.Start();
        }
        else
        {
            _ = ConnectOnceAsync(_transport);
        }
    }

    private static async Task ConnectOnceAsync(ITransport transport)
    {
        try
        {
            await transport.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SipoDeck] Bağlantı kurulamadı: {ex.Message}");
        }
    }

    /// <summary>
    /// Ayarlardaki bağlantı türüne göre transport oluşturur. Adres/port veya seri port
    /// girilmemişse null döner; değerler koda sabitlenmez.
    /// </summary>
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

    private void StopDeviceCommunication()
    {
        _reconnectService?.Dispose();
        _deviceConnection?.Dispose();

        if (_transport is not null)
        {
            var transport = _transport;
            Task.Run(transport.DisconnectAsync).Wait(DisconnectTimeout);
        }

        _inputEngine?.Dispose();
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
            return;

        // Ana pencerenin kapatılması uygulamayı sonlandırmaz; tepsiye küçültür.
        e.Cancel = true;
        _mainWindow?.Hide();
        State = ApplicationState.Background;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
            return;

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        State = ApplicationState.Running;
    }

    private void ExitApplication()
    {
        _isExiting = true;
        State = ApplicationState.Closing;

        // Kaynaklar serbest bırakılır; aktif cihaz bağlantısı kapatılır.
        StopDeviceCommunication();
        _trayIcon?.Dispose();
        _mainWindow?.Close();

        Shutdown();
    }
}
