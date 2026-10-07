using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SipoDeck.Core.Actions;

namespace SipoDeck.ViewModels;

/// <summary>Seçim türü parametrelerinde bir seçenek.</summary>
public sealed record ChoiceOption(object Value, string Label);

/// <summary>Katalogdaki tek bir parametrenin form alanı. Değer değişince sahibi olan editörü günceller.</summary>
public sealed partial class ActionParameterViewModel : ObservableObject
{
    private readonly ActionEditorViewModel _owner;
    private bool _suppress;
    private string? _localError;
    private string? _catalogError;

    public ActionParameterViewModel(ActionEditorViewModel owner, ActionParameterDefinition definition,
        object? value, IReadOnlyList<ProfileOption> profiles)
    {
        _owner = owner;
        Definition = definition;
        Choices = definition.Choices.Select(c => new ChoiceOption(c.Value, c.Label)).ToList();
        Profiles = profiles.ToList();

        _suppress = true;
        switch (definition.Kind)
        {
            case ActionParameterKind.Choice:
                SelectedChoice = Choices.FirstOrDefault(c => Equals(c.Value, value));
                break;
            case ActionParameterKind.ProfileId:
                var id = value as string;
                if (!string.IsNullOrEmpty(id) && Profiles.All(p => p.Id != id))
                    Profiles.Add(new ProfileOption(id, "(Bulunamayan profil)"));
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == id);
                break;
            default:
                TextValue = value?.ToString() ?? string.Empty;
                break;
        }
        _suppress = false;

        BrowseFileCommand = new RelayCommand(BrowseFile);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
    }

    public ActionParameterDefinition Definition { get; }
    public string Name => Definition.Name;
    public string Label => Definition.Required ? Definition.Label + " *" : Definition.Label;
    public IReadOnlyList<ChoiceOption> Choices { get; }
    public List<ProfileOption> Profiles { get; }

    public bool IsChoice => Definition.Kind == ActionParameterKind.Choice;
    public bool IsProfile => Definition.Kind == ActionParameterKind.ProfileId;
    public bool IsFilePath => Definition.Kind == ActionParameterKind.FilePath;
    public bool IsLongText => Definition.Kind == ActionParameterKind.LongText;
    public bool IsPlainText => Definition.Kind is ActionParameterKind.Text or ActionParameterKind.Number or ActionParameterKind.KeyShortcut;
    public bool CanBrowseFile => IsFilePath && Definition.Name != "WorkingDirectory";
    public bool CanBrowseFolder => IsFilePath && Definition.Name is "Path" or "WorkingDirectory";

    public IRelayCommand BrowseFileCommand { get; }
    public IRelayCommand BrowseFolderCommand { get; }

    [ObservableProperty]
    private string _textValue = string.Empty;

    [ObservableProperty]
    private ChoiceOption? _selectedChoice;

    [ObservableProperty]
    private ProfileOption? _selectedProfile;

    public string? Error => _localError ?? _catalogError;

    /// <summary>Katalog doğrulamasından gelen hata iletisini (yoksa null) günceller.</summary>
    public void SetCatalogError(string? message)
    {
        _catalogError = message;
        OnPropertyChanged(nameof(Error));
    }

    partial void OnTextValueChanged(string value)
    {
        if (_suppress) return;

        if (Definition.Kind == ActionParameterKind.Number)
        {
            if (int.TryParse(value.Trim(), out var number))
            {
                _localError = null;
                OnPropertyChanged(nameof(Error));
                _owner.SetParameter(this, number);
            }
            else
            {
                _localError = "Geçerli bir tam sayı girin.";
                OnPropertyChanged(nameof(Error));
            }
            return;
        }

        _owner.SetParameter(this, string.IsNullOrWhiteSpace(value) ? null : value);
    }

    partial void OnSelectedChoiceChanged(ChoiceOption? value)
    {
        if (!_suppress && value is not null) _owner.SetParameter(this, value.Value);
    }

    partial void OnSelectedProfileChanged(ProfileOption? value)
    {
        if (!_suppress && value is not null) _owner.SetParameter(this, value.Id);
    }

    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { CheckFileExists = true, Title = Definition.Label };
        if (dialog.ShowDialog() == true) TextValue = dialog.FileName;
    }

    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = Definition.Label };
        if (dialog.ShowDialog() == true) TextValue = dialog.FolderName;
    }
}
