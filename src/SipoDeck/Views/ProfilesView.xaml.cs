using System.Windows.Controls;
using System.Windows.Threading;
using SipoDeck.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace SipoDeck.Views;

public partial class ProfilesView : UserControl
{
    public ProfilesView()
    {
        InitializeComponent();
    }

    // Seçim onaya bağlandıysa (kaydedilmemiş taslak) ViewModel seçimi değiştirmez; WPF ise listeyi tıklanan satırda
    // bırakabilir. Seçim değişimi bittikten sonra satırların seçili görünümü ViewModel'deki gerçek seçimle eşitlenir.
    private void OnProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (DataContext is not ProfilesViewModel { SelectedProfile: { } selected })
                return;

            var mismatched = false;
            foreach (var item in ProfileList.Items)
            {
                if (ProfileList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container
                    && container.IsSelected != ReferenceEquals(item, selected))
                {
                    mismatched = true;
                    break;
                }
            }

            if (!mismatched)
                return;

            foreach (var item in ProfileList.Items)
            {
                if (ProfileList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
                    container.IsSelected = ReferenceEquals(item, selected);
            }
        });
    }
}
