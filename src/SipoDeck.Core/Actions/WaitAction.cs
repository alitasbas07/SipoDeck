using System.Threading;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Zincirdeki sıradaki eyleme geçmeden önce belirtilen süre kadar bekleyen eylemdir.
/// </summary>
public sealed class WaitAction : IAction
{
    public WaitAction(TimeSpan duration) => Duration = duration;

    public TimeSpan Duration { get; }

    public void Execute()
    {
        if (Duration > TimeSpan.Zero)
            Thread.Sleep(Duration);
    }
}
