namespace SipoDeck.ViewModels;

/// <summary>Henüz geliştirilmemiş sayfalar için boş durum ViewModel'i (design-system §10.e).</summary>
public sealed class PlaceholderPageViewModel
{
    public PlaceholderPageViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
