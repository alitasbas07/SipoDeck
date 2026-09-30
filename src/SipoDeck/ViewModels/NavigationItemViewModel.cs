namespace SipoDeck.ViewModels;

/// <summary>Sol menüdeki tek bir öğe. IconKey XAML kaynak anahtarıdır (ör. "IconHome").</summary>
public sealed class NavigationItemViewModel
{
    public NavigationItemViewModel(string title, string iconKey, object page)
    {
        Title = title;
        IconKey = iconKey;
        Page = page;
    }

    public string Title { get; }

    public string IconKey { get; }

    public object Page { get; }
}
