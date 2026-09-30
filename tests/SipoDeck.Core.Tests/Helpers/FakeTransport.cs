using System.Text;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests.Helpers;

/// <summary>Gönderilen veriyi kaydeden, durum ve gelen veri elle tetiklenen sahte taşıma katmanı.</summary>
public sealed class FakeTransport : ITransport
{
    private ConnectionState _state = ConnectionState.Disconnected;

    public string Name => "Fake";

    public ConnectionState State => _state;

    public bool IsConnected => _state == ConnectionState.Connected;

    public List<byte[]> Sent { get; } = new();

    public bool ThrowOnSend { get; set; }

    public event EventHandler<byte[]>? DataReceived;

    public event EventHandler<ConnectionState>? StateChanged;

    public IEnumerable<string> SentText => Sent.Select(Encoding.UTF8.GetString);

    public void SetState(ConnectionState state)
    {
        if (_state == state)
            return;

        _state = state;
        StateChanged?.Invoke(this, state);
    }

    public void Receive(byte[] data) => DataReceived?.Invoke(this, data);

    public void ReceiveText(string text) => Receive(Encoding.UTF8.GetBytes(text));

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetState(ConnectionState.Connected);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        SetState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend)
            throw new IOException("send failed");

        Sent.Add(data);
        return Task.CompletedTask;
    }
}
