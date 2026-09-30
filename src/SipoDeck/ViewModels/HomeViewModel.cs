using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SipoDeck.ViewModels;

/// <summary>Ana Sayfa. Sistem bilgileri paneli bu task kapsamı dışındadır.</summary>
public sealed class HomeViewModel : ObservableObject
{
    public HomeViewModel(StatusViewModel status, Action<string> navigate)
    {
        Status = status;
        EditDeckCommand = new RelayCommand(() => navigate(ShellViewModel.DeckPageKey));
    }

    public StatusViewModel Status { get; }

    public string GreetingText => "Hoş geldin";

    public IRelayCommand EditDeckCommand { get; }
}
