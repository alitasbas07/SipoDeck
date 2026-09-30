using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Zincirdeki sıradaki eyleme geçmeden önce belirtilen süre kadar bekleyen eylemdir.
/// Bekleme iptal edilebilir; iptal isteği geldiğinde beklemeyi bloklamadan sonlandırır.
/// </summary>
public sealed class WaitAction : IAction
{
    public WaitAction(TimeSpan duration) => Duration = duration;

    public TimeSpan Duration { get; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (Duration > TimeSpan.Zero)
            await Task.Delay(Duration, cancellationToken).ConfigureAwait(false);
    }
}
