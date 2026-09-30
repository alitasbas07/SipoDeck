using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Runtime;

namespace SipoDeck.ViewModels;

/// <summary>Ana pencere kabuğu: navigasyon, açılış ekranı ve durum alanı.</summary>
public sealed class ShellViewModel : ObservableObject, IDisposable
{
    // Navigasyon anahtarları (Navigate çağrılarında kullanılır).
    public const string HomePageKey = "Home";
    public const string DeckPageKey = "Deck";
    public const string ProfilesPageKey = "Profiles";
    public const string ActionsPageKey = "Actions";
    public const string DevicesPageKey = "Devices";
    public const string NotificationsPageKey = "Notifications";
    public const string SettingsPageKey = "Settings";

    private readonly Dictionary<string, NavigationItemViewModel> _itemsByKey = new();
    private NavigationItemViewModel _selectedNavigationItem;
    private bool _isSplashVisible = true;
    private bool _dontShowSplashAgain;
    private NavigationItemViewModel? _pendingNavigation;
    private NavigationItemViewModel _pageItem;

    public ShellViewModel(AppRuntime runtime, Dispatcher dispatcher)
    {
        Status = new StatusViewModel(runtime, dispatcher);
        Settings = new SettingsViewModel(runtime, dispatcher, Status);

        var items = new List<NavigationItemViewModel>
        {
            Add(HomePageKey, "Ana Sayfa", "IconHome", new HomeViewModel(Status, Navigate)),
            Add(DeckPageKey, "Deck", "IconDeck", new PlaceholderPageViewModel("Deck", "Tuşlara eylem atama yakında burada.")),
            Add(ProfilesPageKey, "Profiller", "IconProfiles", new PlaceholderPageViewModel("Profiller", "Profil oluşturma ve düzenleme yakında burada.")),
            Add(ActionsPageKey, "Eylemler", "IconActions", new PlaceholderPageViewModel("Eylemler", "Eylem kitaplığı yakında burada.")),
            Add(DevicesPageKey, "Cihazlar", "IconDevices", new PlaceholderPageViewModel("Cihazlar", "Cihaz eşleştirme ve yönetimi yakında burada.")),
            Add(NotificationsPageKey, "Bildirimler", "IconNotifications", new PlaceholderPageViewModel("Bildirimler", "İşlem bildirimlerinin geçmişi yakında burada.")),
            Add(SettingsPageKey, "Ayarlar", "IconSettings", Settings)
        };

        NavigationItems = items;
        _selectedNavigationItem = items[0];
        _pageItem = items[0];

        StartCommand = new RelayCommand(() => Proceed(HomePageKey));
        ConnectDeviceCommand = new RelayCommand(() => Proceed(DevicesPageKey));

        UnsavedSaveCommand = new AsyncRelayCommand(ResolveUnsavedSaveAsync);
        UnsavedDiscardCommand = new RelayCommand(() =>
        {
            Settings.Discard();
            CompletePendingNavigation();
        });
        UnsavedCancelCommand = new RelayCommand(CancelPendingNavigation);
    }

    public StatusViewModel Status { get; }

    public SettingsViewModel Settings { get; }

    /// <summary>Kaydedilmemiş ayar varken başka sayfaya geçilmek istendiğinde beklenen hedef; null ise uyarı yok.</summary>
    public NavigationItemViewModel? PendingNavigation
    {
        get => _pendingNavigation;
        private set
        {
            if (SetProperty(ref _pendingNavigation, value))
                OnPropertyChanged(nameof(IsUnsavedPromptVisible));
        }
    }

    public bool IsUnsavedPromptVisible => _pendingNavigation is not null;

    public IAsyncRelayCommand UnsavedSaveCommand { get; }

    public IRelayCommand UnsavedDiscardCommand { get; }

    public IRelayCommand UnsavedCancelCommand { get; }

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; }

    public NavigationItemViewModel SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            // ListBox yeniden bağlanırken null gönderebilir; seçim boşa düşmez.
            if (value is null)
                return;

            if (!SetProperty(ref _selectedNavigationItem, value))
                return;

            OnPropertyChanged(nameof(SelectedNavIndex));

            // Ayarlar sayfasında kaydedilmemiş değişiklik varken sayfa değişimi onaya bağlanır:
            // menü seçimi hedefi gösterir, sayfa onaya kadar değişmez.
            if (!ReferenceEquals(value, _pageItem) && ReferenceEquals(_pageItem.Page, Settings) && Settings.HasChanges)
            {
                PendingNavigation = value;
                return;
            }

            _pageItem = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    /// <summary>ListBox.SelectedIndex bağlamak isteyen XAML için SelectedNavigationItem'ın indeks karşılığı.</summary>
    public int SelectedNavIndex
    {
        get
        {
            for (var i = 0; i < NavigationItems.Count; i++)
            {
                if (ReferenceEquals(NavigationItems[i], _selectedNavigationItem))
                    return i;
            }

            return 0;
        }
        set
        {
            if (value >= 0 && value < NavigationItems.Count)
                SelectedNavigationItem = NavigationItems[value];
        }
    }

    public object CurrentPage => _pageItem.Page;

    public bool IsSplashVisible
    {
        get => _isSplashVisible;
        private set => SetProperty(ref _isSplashVisible, value);
    }

    // Yalnızca oturum içi. Kalıcı saklama yeni bir veri alanı gerektirir; kalıcılık için kullanıcı onayı gerekir.
    public bool DontShowSplashAgain
    {
        get => _dontShowSplashAgain;
        set => SetProperty(ref _dontShowSplashAgain, value);
    }

    public IRelayCommand StartCommand { get; }

    public IRelayCommand ConnectDeviceCommand { get; }

    /// <summary>Sayfa anahtarıyla (…PageKey sabitleri) ilgili sayfayı seçer; bilinmeyen anahtar yok sayılır.</summary>
    public void Navigate(string pageKey)
    {
        if (_itemsByKey.TryGetValue(pageKey, out var item))
            SelectedNavigationItem = item;
    }

    public void Dispose()
    {
        Settings.Dispose();
        Status.Dispose();
    }

    private async Task ResolveUnsavedSaveAsync()
    {
        // Hata varsa sayfa değişmez; hatalar Ayarlar sayfasında alanların yanında kalır.
        if (await Settings.SaveAsync())
            CompletePendingNavigation();
        else
            CancelPendingNavigation();
    }

    // Kal: menü seçimini açık sayfaya geri döndür.
    private void CancelPendingNavigation()
    {
        PendingNavigation = null;
        SelectedNavigationItem = _pageItem;
    }

    private void CompletePendingNavigation()
    {
        var target = _pendingNavigation;
        PendingNavigation = null;
        if (target is null)
            return;

        _pageItem = target;
        OnPropertyChanged(nameof(CurrentPage));
    }

    private NavigationItemViewModel Add(string key, string title, string iconKey, object page)
    {
        var item = new NavigationItemViewModel(title, iconKey, page);
        _itemsByKey[key] = item;
        return item;
    }

    private void Proceed(string pageKey)
    {
        IsSplashVisible = false;
        Navigate(pageKey);
    }
}
