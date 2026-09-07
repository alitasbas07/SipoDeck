using System.Diagnostics;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Verilen adresi varsayılan tarayıcıda açan eylemdir.
/// </summary>
public sealed class OpenUrlAction : IAction
{
    public OpenUrlAction(string url) => Url = url;

    public string Url { get; }

    public void Execute()
        => Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
}
