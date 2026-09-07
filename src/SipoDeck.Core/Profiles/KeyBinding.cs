using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Profiles;

/// <summary>
/// Tek bir tuşa atanan eylemleri tutar.
/// Normal eylem tuş bırakıldığında çalışır; uzun basma eylemi tanımlıysa ve
/// eşik süre aşılırsa onun yerine uzun basma eylemi çalışır.
/// </summary>
public sealed class KeyBinding
{
    public KeyBinding(IAction? action = null, IAction? longPressAction = null)
    {
        Action = action;
        LongPressAction = longPressAction;
    }

    public IAction? Action { get; set; }

    public IAction? LongPressAction { get; set; }

    public bool HasLongPress => LongPressAction is not null;
}
