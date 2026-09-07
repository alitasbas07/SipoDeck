using System.Threading;
using System.Threading.Tasks;

namespace SipoDeck.Core.Transport;

/// <summary>
/// Bir transport bağlantısı koptuğunda, ana iş parçacığını engellemeden arka planda
/// yeniden bağlanmayı deneyen basit bir yapıdır. Başarısız denemeler uygulamayı kilitlemez.
/// Gerçek bağlantı Task 010 kapsamında devreye girer.
/// </summary>
public sealed class ReconnectService : IDisposable
{
    private readonly ITransport _transport;
    private readonly TimeSpan _retryDelay;
    private CancellationTokenSource? _cts;

    public ReconnectService(ITransport transport, TimeSpan retryDelay)
    {
        _transport = transport;
        _retryDelay = retryDelay;
    }

    public bool IsRunning => _cts is not null;

    public void Start()
    {
        if (_cts is not null)
            return;

        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_transport.IsConnected)
            {
                try
                {
                    await _transport.ConnectAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Bağlantı başarısız; beklenip tekrar denenecek. Uygulama kilitlenmez.
                }
            }

            try
            {
                await Task.Delay(_retryDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose() => Stop();
}
