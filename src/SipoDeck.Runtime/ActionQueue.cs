using System.Threading.Channels;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Input;
using SipoDeck.Runtime.Events;

namespace SipoDeck.Runtime;

/// <summary>
/// Input Engine'in ürettiği eylemleri tek okuyuculu bir kuyrukta sırayla çalıştırır.
/// Aynı anda yalnızca bir eylem/zincir çalışır. Bir eylemin hata fırlatması kuyruğu
/// durdurmaz; <see cref="ActionFailed"/> ile bildirilip sıradaki eyleme geçilir.
/// Durdurulduğunda çalışan eylem iptal edilir, kuyrukta bekleyen eylemler çalıştırılmaz.
/// .NET'in yerleşik <see cref="Channel{T}"/> altyapısı kullanılır; harici paket eklenmez.
/// </summary>
internal sealed class ActionQueue : IActionDispatcher, IAsyncDisposable
{
    private readonly Channel<IAction> _channel = Channel.CreateUnbounded<IAction>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly object _gate = new();
    private readonly Task _worker;
    private CancellationTokenSource? _currentActionCts;

    public ActionQueue()
    {
        _worker = Task.Run(RunAsync);
    }

    /// <summary>Kuyruktan çalıştırılan bir eylem hata fırlattığında tetiklenir.</summary>
    public event EventHandler<ActionFailedEventArgs>? ActionFailed;

    public void Dispatch(IAction action)
    {
        // Durduruluyor/durduysa yeni eylem kabul edilmez; kanal tamamlandıktan sonra TryWrite sessizce false döner.
        _channel.Writer.TryWrite(action);
    }

    private async Task RunAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var action))
                {
                    // Durdurma isteği geldiyse kanalda bekleyen eylemler çalıştırılmadan atlanır.
                    if (_stoppingCts.IsCancellationRequested)
                        continue;

                    await ExecuteAsync(action).ConfigureAwait(false);
                }
            }
        }
        catch (Exception)
        {
            // Kuyruk okuma döngüsü beklenmedik şekilde çökmemeli; işçi görev burada sessizce sonlanır.
        }
    }

    private async Task ExecuteAsync(IAction action)
    {
        CancellationTokenSource actionCts;
        lock (_gate)
        {
            actionCts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingCts.Token);
            _currentActionCts = actionCts;
        }

        try
        {
            await action.ExecuteAsync(actionCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (actionCts.IsCancellationRequested)
        {
            // Runtime durduruluyor; iptal beklenen bir durumdur, hata olarak bildirilmez.
        }
        catch (Exception ex)
        {
            ActionFailed?.Invoke(this, new ActionFailedEventArgs(action.GetType().Name, ex));
        }
        finally
        {
            lock (_gate)
            {
                _currentActionCts = null;
            }

            actionCts.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();

        lock (_gate)
        {
            _currentActionCts?.Cancel();
        }

        _stoppingCts.Cancel();

        try
        {
            await _worker.ConfigureAwait(false);
        }
        catch
        {
            // İşçi görev kapanış sırasında hata verse bile Dispose akışı kesilmemeli.
        }

        _stoppingCts.Dispose();
    }
}
