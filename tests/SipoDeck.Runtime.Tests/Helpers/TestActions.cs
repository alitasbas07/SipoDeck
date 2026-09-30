using SipoDeck.Core.Actions;

namespace SipoDeck.Runtime.Tests.Helpers;

/// <summary>Çalışma sırasını kaydeden ve tamamlandığında sinyal veren eylem.</summary>
public sealed class RecordingAction : IAction
{
    private readonly List<string> _log;
    private readonly string _name;
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public RecordingAction(List<string> log, string name)
    {
        _log = log;
        _name = name;
    }

    public Task Completed => _completed.Task;

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        lock (_log) _log.Add(_name);
        _completed.TrySetResult();
        return Task.CompletedTask;
    }
}

public sealed class ThrowingAction : IAction
{
    private readonly Exception _exception;

    public ThrowingAction(Exception exception) => _exception = exception;

    public Task ExecuteAsync(CancellationToken cancellationToken = default) => throw _exception;
}

/// <summary>Başladığını bildirir, iptal edilene kadar bekler ve iptali gözlemlediğini kaydeder.</summary>
public sealed class BlockUntilCancelledAction : IAction
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _cancelObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;

    public Task CancelObserved => _cancelObserved.Task;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => _cancelObserved.TrySetResult());
        _started.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

/// <summary>Başladığını bildirir, verilen görev tamamlanana kadar bekler, sonra kaydeder.</summary>
public sealed class GatedAction : IAction
{
    private readonly List<string> _log;
    private readonly string _name;
    private readonly Task _gate;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public GatedAction(List<string> log, string name, Task gate)
    {
        _log = log;
        _name = name;
        _gate = gate;
    }

    public Task Started => _started.Task;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _started.TrySetResult();
        await _gate;
        lock (_log) _log.Add(_name);
    }
}

public static class TaskExtensions
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static Task WithTimeout(this Task task) => task.WaitAsync(DefaultTimeout);

    public static Task<T> WithTimeout<T>(this Task<T> task) => task.WaitAsync(DefaultTimeout);
}
