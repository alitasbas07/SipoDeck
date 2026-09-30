using SipoDeck.Core.Actions;
using SipoDeck.Runtime.Events;
using SipoDeck.Runtime.Tests.Helpers;

namespace SipoDeck.Runtime.Tests;

// ActionQueue internal olduğundan public AppRuntime.Dispatch üzerinden test edilir.
public sealed class ActionQueueTests : IAsyncLifetime, IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly AppRuntime _runtime;

    public ActionQueueTests() => _runtime = new AppRuntime(_dir.SettingsFile, _dir.ProfilesFile);

    public Task InitializeAsync() => _runtime.StartAsync();

    public async Task DisposeAsync() => await _runtime.StopAsync();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task Actions_RunInDispatchOrder()
    {
        var log = new List<string>();
        var actions = Enumerable.Range(0, 20).Select(i => new RecordingAction(log, i.ToString())).ToList();

        foreach (var a in actions)
            _runtime.Dispatch(a);

        await actions[^1].Completed.WithTimeout();
        Assert.Equal(Enumerable.Range(0, 20).Select(i => i.ToString()), log);
    }

    [Fact]
    public async Task Actions_DoNotOverlap_NextWaitsForCurrent()
    {
        var log = new List<string>();
        var gate = new TaskCompletionSource();
        var first = new GatedAction(log, "first", gate.Task);
        var second = new RecordingAction(log, "second");

        _runtime.Dispatch(first);
        _runtime.Dispatch(second);
        await first.Started.WithTimeout();

        Assert.False(second.Completed.IsCompleted);
        gate.SetResult();
        await second.Completed.WithTimeout();

        Assert.Equal(new[] { "first", "second" }, log);
    }

    [Fact]
    public async Task FailingAction_RaisesTypedActionFailed_AndQueueContinues()
    {
        var raised = new TaskCompletionSource<ActionFailedEventArgs>();
        var count = 0;
        _runtime.ActionFailed += (_, e) => { Interlocked.Increment(ref count); raised.TrySetResult(e); };

        var error = new InvalidOperationException("boom");
        var log = new List<string>();
        var next = new RecordingAction(log, "next");

        _runtime.Dispatch(new ThrowingAction(error));
        _runtime.Dispatch(next);

        var failure = await raised.Task.WithTimeout();
        await next.Completed.WithTimeout();

        Assert.Equal(nameof(ThrowingAction), failure.ActionType);
        Assert.Same(error, failure.Exception);
        Assert.Equal(1, Volatile.Read(ref count));
        Assert.Equal(new[] { "next" }, log);
        Assert.Equal(RuntimeState.Running, _runtime.State);
    }

    [Fact]
    public async Task FailingChain_ReportsAsActionChain_WithAggregateException_AndQueueContinues()
    {
        var raised = new TaskCompletionSource<ActionFailedEventArgs>();
        _runtime.ActionFailed += (_, e) => raised.TrySetResult(e);

        var log = new List<string>();
        var chain = new ActionChain(
            new ThrowingAction(new InvalidOperationException("x")),
            new RecordingAction(log, "afterFailure"));
        var next = new RecordingAction(log, "next");

        _runtime.Dispatch(chain);
        _runtime.Dispatch(next);

        var failure = await raised.Task.WithTimeout();
        await next.Completed.WithTimeout();

        Assert.Equal(nameof(ActionChain), failure.ActionType);
        Assert.IsType<AggregateException>(failure.Exception);
        Assert.Equal(new[] { "afterFailure", "next" }, log);
    }

    [Fact]
    public async Task Stop_CancelsRunningAction_AndDoesNotRunPendingActions_AndIsNotReportedAsFailure()
    {
        var failures = 0;
        _runtime.ActionFailed += (_, _) => Interlocked.Increment(ref failures);

        var log = new List<string>();
        var running = new BlockUntilCancelledAction();
        var pending1 = new RecordingAction(log, "p1");
        var pending2 = new RecordingAction(log, "p2");

        _runtime.Dispatch(running);
        _runtime.Dispatch(pending1);
        _runtime.Dispatch(pending2);
        await running.Started.WithTimeout();

        await _runtime.StopAsync().WithTimeout();

        Assert.True(running.CancelObserved.IsCompleted);
        Assert.Empty(log);
        Assert.False(pending1.Completed.IsCompleted);
        Assert.False(pending2.Completed.IsCompleted);
        Assert.Equal(0, Volatile.Read(ref failures));
        Assert.Equal(RuntimeState.Stopped, _runtime.State);
    }

    [Fact]
    public async Task Dispatch_AfterStop_IsIgnored_AndRestartGetsFreshQueue()
    {
        await _runtime.StopAsync();
        var log = new List<string>();
        _runtime.Dispatch(new RecordingAction(log, "dropped"));

        await _runtime.StartAsync();
        var kept = new RecordingAction(log, "kept");
        _runtime.Dispatch(kept);
        await kept.Completed.WithTimeout();

        Assert.Equal(new[] { "kept" }, log);
    }
}
