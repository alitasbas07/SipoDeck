using SipoDeck.Core.Actions;
using SipoDeck.Core.Input;

namespace SipoDeck.Core.Tests.Helpers;

/// <summary>Çalıştığında ortak günlüğe adını yazan eylem.</summary>
public sealed class RecordingAction : IAction
{
    private readonly List<string> _log;

    public RecordingAction(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    public string Name { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        lock (_log)
            _log.Add(Name);
        return Task.CompletedTask;
    }
}

/// <summary>Çalıştığında günlüğe yazıp hata fırlatan eylem.</summary>
public sealed class ThrowingAction : IAction
{
    private readonly List<string> _log;

    public ThrowingAction(string name, List<string> log, Exception? exception = null)
    {
        Name = name;
        _log = log;
        Exception = exception ?? new InvalidOperationException(name + " failed");
    }

    public string Name { get; }

    public Exception Exception { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        lock (_log)
            _log.Add(Name);
        throw Exception;
    }
}

/// <summary>İptal edilene kadar bekleyen eylem; başladığında <see cref="Started"/> tamamlanır.</summary>
public sealed class BlockingAction : IAction
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _started.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

/// <summary>Gönderilen eylemleri sırayla kaydeden sahte dağıtıcı.</summary>
public sealed class RecordingDispatcher : IActionDispatcher
{
    public List<IAction> Dispatched { get; } = new();

    public IEnumerable<string> Names => Dispatched.OfType<RecordingAction>().Select(a => a.Name);

    public void Dispatch(IAction action) => Dispatched.Add(action);
}
