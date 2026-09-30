using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Verilen adresi varsayılan tarayıcıda açan eylemdir.
/// </summary>
public sealed class OpenUrlAction : IAction
{
    public OpenUrlAction(string url) => Url = url;

    public string Url { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}
