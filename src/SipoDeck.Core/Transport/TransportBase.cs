using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// Transport uygulamaları için ortak durum yönetimi ve olay bildirimini sağlayan taban sınıf.
/// Gerçek bağlantı ve veri gönderimi türe özgü sınıflarda uygulanır.
/// </summary>
public abstract class TransportBase : ITransport
{
    private ConnectionState _state = ConnectionState.Disconnected;

    protected TransportBase(string name) => Name = name;

    public string Name { get; }

    public ConnectionState State => _state;

    public bool IsConnected => _state == ConnectionState.Connected;

    public event EventHandler<byte[]>? DataReceived;

    public event EventHandler<ConnectionState>? StateChanged;

    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

    public abstract Task SendAsync(byte[] data, CancellationToken cancellationToken = default);

    public virtual Task DisconnectAsync()
    {
        SetState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    /// <summary>Bağlantı durumunu günceller ve değişimi bildirir.</summary>
    protected void SetState(ConnectionState state)
    {
        if (_state == state)
            return;

        _state = state;
        StateChanged?.Invoke(this, state);
    }

    /// <summary>Alınan ham veriyi üst katmana bildirir.</summary>
    protected void RaiseDataReceived(byte[] data) => DataReceived?.Invoke(this, data);
}
