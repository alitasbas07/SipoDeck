namespace SipoDeck.Core.Tests.Helpers;

/// <summary>Zamanı yalnızca <see cref="Advance"/> ile ilerleyen sahte TimeProvider.</summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private readonly List<FakeTimer> _timers = new();
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public int ActiveTimerCount
    {
        get
        {
            lock (_timers)
                return _timers.Count(t => t.IsActive);
        }
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state);
        timer.Change(dueTime, period);
        lock (_timers)
            _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan delta)
    {
        var target = _now + delta;

        while (true)
        {
            FakeTimer? next;
            lock (_timers)
                next = _timers.Where(t => t.IsActive && t.Due <= target).OrderBy(t => t.Due).FirstOrDefault();

            if (next is null)
                break;

            if (next.Due > _now)
                _now = next.Due;
            next.Fire();
        }

        _now = target;
    }

    private sealed class FakeTimer : ITimer
    {
        private readonly FakeTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private TimeSpan _period = Timeout.InfiniteTimeSpan;

        public FakeTimer(FakeTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        public bool IsActive { get; private set; }

        public DateTimeOffset Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                IsActive = false;
                return true;
            }

            Due = _owner._now + dueTime;
            IsActive = true;
            return true;
        }

        public void Fire()
        {
            if (_period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero)
                IsActive = false;
            else
                Due += _period;

            _callback(_state);
        }

        public void Dispose() => IsActive = false;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
