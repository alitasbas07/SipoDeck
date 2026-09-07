using System.ComponentModel;
using System.Windows;
using SipoDeck.Core.Settings;

namespace SipoDeck;

/// <summary>
/// Interaction logic for App.xaml
/// Uygulama yaşam döngüsünü yönetir: ana pencere kapatıldığında sistem tepsisine
/// küçültülür, tepsiden geri açılabilir ve yalnızca "Tamamen Kapat" ile sonlanır.
/// </summary>
public partial class App
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    public ApplicationState State { get; private set; } = ApplicationState.Starting;

    public AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Settings = new SettingsStore().Load();
        WindowsStartup.Apply(Settings.Application.RunAtStartup);

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;

        _trayIcon = new TrayIcon(onShow: ShowMainWindow, onExit: ExitApplication);

        _mainWindow.Show();
        State = ApplicationState.Running;
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

        // Kaynaklar serbest bırakılır. Aktif cihaz bağlantıları Task 010'da burada kapatılacaktır.
        _trayIcon?.Dispose();
        _mainWindow?.Close();

        Shutdown();
    }
}
