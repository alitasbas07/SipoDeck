using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using SipoDeck.Core.Settings;
using SipoDeck.Runtime;

namespace SipoDeck;

/// <summary>
/// Interaction logic for App.xaml
/// Uygulama yaşam döngüsünü yönetir: ana pencere kapatıldığında sistem tepsisine
/// küçültülür, tepsiden geri açılabilir ve yalnızca "Tamamen Kapat" ile sonlanır.
/// Ayar/profil yükleme, cihaz bağlantısı, Input Engine ve eylem kuyruğu <see cref="AppRuntime"/>
/// tarafından yönetilir; bu sınıf yalnızca WPF/tray yaşam döngüsünü ve Runtime bağlantısını yönetir.
/// </summary>
public partial class App
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    public ApplicationState State { get; private set; } = ApplicationState.Starting;

    public AppRuntime Runtime { get; } = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Windows başlangıç ayarı Runtime'ın sorumluluğu değildir (Task 014'te olay tabanlı
        // hale getirilecektir); şimdilik burada tek seferlik okunur.
        var startupSettings = new SettingsStore().Load();
        WindowsStartup.Apply(startupSettings.Application.RunAtStartup);

        SubscribeToRuntimeEvents();

        try
        {
            await Runtime.StartAsync();
        }
        catch (Exception ex)
        {
            // Runtime Faulted durumuna geçer ve olayla bildirir; uygulama yine de açılır.
            Debug.WriteLine($"[SipoDeck] Runtime başlatılamadı: {ex.Message}");
        }

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;

        _trayIcon = new TrayIcon(onShow: ShowMainWindow, onExit: ExitApplication);

        _mainWindow.Show();
        State = ApplicationState.Running;
    }

    private void SubscribeToRuntimeEvents()
    {
        Runtime.StateChanged += (_, state) => Debug.WriteLine($"[SipoDeck] Runtime: {state}");
        Runtime.DeviceConnectionStateChanged += (_, args) => Debug.WriteLine($"[SipoDeck] {args.ConnectionType}: {args.State}");
        Runtime.DeviceIdentified += (_, device) =>
            Debug.WriteLine($"[SipoDeck] Cihaz tanındı: {device.Name} ({device.Id}), firmware {device.FirmwareVersion}");
        Runtime.MessageRejected += (_, reason) => Debug.WriteLine($"[SipoDeck] Mesaj reddedildi: {reason}");
        Runtime.ActionFailed += (_, args) => Debug.WriteLine($"[SipoDeck] Eylem başarısız ({args.ActionType}): {args.Exception.Message}");
        Runtime.Faulted += (_, args) => Debug.WriteLine($"[SipoDeck] Runtime kritik hata: {args.Exception.Message}");
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

    private async void ExitApplication()
    {
        _isExiting = true;
        State = ApplicationState.Closing;

        try
        {
            await Runtime.StopAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SipoDeck] Runtime durdurulurken hata: {ex.Message}");
        }

        _trayIcon?.Dispose();
        _mainWindow?.Close();

        Shutdown();
    }
}
