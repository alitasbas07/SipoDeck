using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Birden fazla eylemi tanımlandıkları sırayla çalıştıran bileşik bir eylemdir.
/// Bir eylemin başarısız olması zinciri durdurmaz; sıradaki eylemle devam edilir.
/// Zincir tamamlandığında oluşan hatalar kaybolmaz: hepsi <see cref="AggregateException"/>
/// içinde toplanıp çağırana (Runtime'ın raporlayabilmesi için) fırlatılır.
/// İptal isteği geldiğinde zincir durur ve kalan eylemler çalıştırılmaz.
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

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        List<Exception>? failures = null;

        foreach (var action in _actions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Bir eylem başarısız olsa bile zincir ve sistem çökmemeli; sıradaki eyleme geçilir.
                // Hata kaybolmaz: sonda toplanıp çağırana bildirilir.
                (failures ??= new List<Exception>()).Add(ex);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Eylem zincirinde bir veya daha fazla eylem başarısız oldu.", failures);
    }
}
