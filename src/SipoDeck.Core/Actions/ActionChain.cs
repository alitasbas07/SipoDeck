namespace SipoDeck.Core.Actions;

/// <summary>
/// Birden fazla eylemi tanımlandıkları sırayla çalıştıran bileşik bir eylemdir.
/// Bir eylemin başarısız olması sistemi çökertmez; zincir sıradaki eylemle devam eder.
/// (Durdur/devam et davranışı ileride yapılandırılabilir hale getirilebilir.)
/// </summary>
public sealed class ActionChain : IAction
{
    private readonly List<IAction> _actions;

    public ActionChain(params IAction[] actions)
        : this((IEnumerable<IAction>)actions)
    {
    }

    public ActionChain(IEnumerable<IAction> actions)
    {
        _actions = new List<IAction>(actions);
    }

    public IReadOnlyList<IAction> Actions => _actions;

    public void Execute()
    {
        foreach (var action in _actions)
        {
            try
            {
                action.Execute();
            }
            catch
            {
                // Bir eylem başarısız olsa bile zincir ve sistem çökmemeli; sıradaki eyleme geçilir.
            }
        }
    }
}
