using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Girdiye karşılık çalıştırılabilen bir eylemi temsil eder.
/// Çalıştırma iptal edilebilir olmalıdır; uzun süren eylemler iptal isteğine saygı göstermelidir.
/// </summary>
public interface IAction
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
