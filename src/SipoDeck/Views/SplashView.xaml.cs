using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SipoDeck.Views;

/// <summary>
/// Açılış ekranı. Video kaynağı tek yerden değişir: <see cref="IntroVideoPath"/>.
/// Dosya yoksa veya oynatılamazsa XAML ile çizilmiş yer tutucu gösterilir.
/// </summary>
public partial class SplashView : System.Windows.Controls.UserControl
{
    /// <summary>Açılış videosunun tek yolu (uygulama dizininde, Assets/Video/intro.mp4).</summary>
    public static readonly string IntroVideoPath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Video", "intro.mp4");

    public SplashView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (File.Exists(IntroVideoPath))
        {
            try
            {
                IntroVideo.Source = new Uri(IntroVideoPath, UriKind.Absolute);
                IntroVideo.Play();
            }
            catch
            {
                ShowPlaceholder();
            }
        }

        if (SystemParameters.ClientAreaAnimation)
        {
            ((Storyboard)Resources["IntroStoryboard"]).Begin(this);
        }
        else
        {
            // Azaltılmış hareket: son kare doğrudan gösterilir.
            VideoLayer.Opacity = 1;
            Logo.Opacity = 1;
            Headline.Opacity = 1;
            Description.Opacity = 1;
            Actions.Opacity = 1;
            DontShowAgain.Opacity = 1;
        }

        StartButton.Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Ağaçtan kalkınca video çözücüyü bırak.
        IntroVideo.Stop();
        IntroVideo.Close();
        IntroVideo.Source = null;
    }

    private void OnMediaOpened(object sender, RoutedEventArgs e)
    {
        IntroVideo.Visibility = Visibility.Visible;
        Placeholder.Visibility = Visibility.Collapsed;
    }

    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        IntroVideo.Position = TimeSpan.Zero;
        IntroVideo.Play();
    }

    private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e) => ShowPlaceholder();

    private void ShowPlaceholder()
    {
        IntroVideo.Visibility = Visibility.Collapsed;
        Placeholder.Visibility = Visibility.Visible;
    }
}
