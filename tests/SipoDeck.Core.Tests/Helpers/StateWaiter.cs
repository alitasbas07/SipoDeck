using System.Threading.Channels;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests.Helpers;

/// <summary>Taşıma katmanının durum geçişlerini sırayla toplar ve belirli bir duruma gelinmesini bekler.</summary>
public sealed class StateWaiter
{
    private readonly Channel<ConnectionState> _states = Channel.CreateUnbounded<ConnectionState>();

    public StateWaiter(ITransport transport) => transport.StateChanged += (_, s) => _states.Writer.TryWrite(s);

    public async Task WaitForAsync(ConnectionState expected)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (await _states.Reader.ReadAsync(cts.Token) != expected)
        {
        }
    }
}
