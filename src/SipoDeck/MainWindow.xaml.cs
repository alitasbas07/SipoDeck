using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SipoDeck.ViewModels;

namespace SipoDeck;

/// <summary>
/// Ana pencere kabuğu. Code-behind yalnızca pencere komutları ve açılış → kabuk geçişi tetikleriyle sınırlıdır.
/// </summary>
public partial class MainWindow : Window
{
    // Açılışın ağaçtan kaldırılma gecikmesi (design-system §10.g: 340 ms).
    private static readonly TimeSpan SplashRemoveDelay = TimeSpan.FromMilliseconds(340);

    private ShellViewModel? _shell;

    public MainWindow()
    {
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_shell is not null)
            _shell.PropertyChanged -= OnShellPropertyChanged;

        _shell = e.NewValue as ShellViewModel;
        if (_shell is null)
            return;

        _shell.PropertyChanged += OnShellPropertyChanged;

        if (_shell.IsSplashVisible)
            return;

        // Açılış atlanıyorsa kabuk doğrudan görünür.
        RemoveSplash();
        ShowShell();
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.IsSplashVisible) || _shell is null || _shell.IsSplashVisible)
            return;

        ShowShell();

        if (!SystemParameters.ClientAreaAnimation)
        {
            RemoveSplash();
            return;
        }

        Splash.IsHitTestVisible = false;
        ((Storyboard)Resources["SplashExitStoryboard"]).Begin(this);

        var timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = SplashRemoveDelay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            RemoveSplash();
        };
        timer.Start();
    }

    private void ShowShell()
    {
        ShellRoot.Visibility = Visibility.Visible;

        if (SystemParameters.ClientAreaAnimation)
            ((Storyboard)Resources["ShellEnterStoryboard"]).Begin(this);
        else
            ShellRoot.Opacity = 1;
    }

    private void RemoveSplash()
    {
        // Ağaçtan kalkınca SplashView.Unloaded video çözücüyü bırakır.
        if (Splash.Parent is System.Windows.Controls.Panel panel)
            panel.Children.Remove(Splash);
    }

    private void OnSettingsShortcutClick(object sender, RoutedEventArgs e) =>
        _shell?.Navigate(ShellViewModel.SettingsPageKey);
}
