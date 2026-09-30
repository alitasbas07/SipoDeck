using SipoDeck.Core.Actions;
using SipoDeck.Core.Tests.Helpers;

namespace SipoDeck.Core.Tests;

public class ActionChainTests
{
    private readonly List<string> _log = new();

    [Fact]
    public async Task ExecuteAsync_RunsActionsInDefinedOrder()
    {
        var chain = new ActionChain(new RecordingAction("a", _log), new RecordingAction("b", _log), new RecordingAction("c", _log));

        await chain.ExecuteAsync();

        Assert.Equal(new[] { "a", "b", "c" }, _log);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyChain_Completes()
    {
        await new ActionChain().ExecuteAsync();
    }

    [Fact]
    public async Task ExecuteAsync_FailingAction_DoesNotStopLaterActions_AndIsReported()
    {
        var boom = new InvalidOperationException("boom");
        var chain = new ActionChain(
            new RecordingAction("a", _log),
            new ThrowingAction("bad", _log, boom),
            new RecordingAction("c", _log));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => chain.ExecuteAsync());

        Assert.Equal(new[] { "a", "bad", "c" }, _log);
        Assert.Same(boom, Assert.Single(ex.InnerExceptions));
    }

    [Fact]
    public async Task ExecuteAsync_MultipleFailures_AreAllReported()
    {
        var chain = new ActionChain(new ThrowingAction("x", _log), new ThrowingAction("y", _log));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => chain.ExecuteAsync());

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_RunsNothing()
    {
        var chain = new ActionChain(new RecordingAction("a", _log));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => chain.ExecuteAsync(cts.Token));

        Assert.Empty(_log);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledWhileRunning_StopsChain()
    {
        var blocking = new BlockingAction();
        var chain = new ActionChain(new RecordingAction("a", _log), blocking, new RecordingAction("after", _log));
        using var cts = new CancellationTokenSource();

        var run = chain.ExecuteAsync(cts.Token);
        await blocking.Started;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(new[] { "a" }, _log);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledAfterFailure_StillCancels()
    {
        var blocking = new BlockingAction();
        var chain = new ActionChain(new ThrowingAction("bad", _log), blocking, new RecordingAction("after", _log));
        using var cts = new CancellationTokenSource();

        var run = chain.ExecuteAsync(cts.Token);
        await blocking.Started;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(new[] { "bad" }, _log);
    }

    [Fact]
    public async Task NestedChain_RunsInOrder()
    {
        var chain = new ActionChain(
            new RecordingAction("a", _log),
            new ActionChain(new RecordingAction("b1", _log), new RecordingAction("b2", _log)),
            new RecordingAction("c", _log));

        await chain.ExecuteAsync();

        Assert.Equal(new[] { "a", "b1", "b2", "c" }, _log);
    }
}

public class WaitActionTests
{
    [Fact]
    public async Task ExecuteAsync_ZeroDuration_CompletesImmediately()
    {
        await new WaitAction(TimeSpan.Zero).ExecuteAsync();
    }

    [Fact]
    public async Task ExecuteAsync_NegativeDuration_CompletesImmediately()
    {
        await new WaitAction(TimeSpan.FromSeconds(-1)).ExecuteAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CanBeCancelled()
    {
        var wait = new WaitAction(TimeSpan.FromHours(1));
        using var cts = new CancellationTokenSource();

        var run = wait.ExecuteAsync(cts.Token);
        Assert.False(run.IsCompleted);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new WaitAction(TimeSpan.FromHours(1)).ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task WaitInsideChain_CancelStopsRemainingActions()
    {
        var log = new List<string>();
        var chain = new ActionChain(new WaitAction(TimeSpan.FromHours(1)), new RecordingAction("after", log));
        using var cts = new CancellationTokenSource();

        var run = chain.ExecuteAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Empty(log);
    }
}
