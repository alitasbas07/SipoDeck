namespace SipoDeck.Runtime.Tests.Helpers;

/// <summary>Olayları sırayla kaydeder; sıradaki olayı kısa zaman aşımıyla bekletir (sabit Task.Delay kullanılmaz).</summary>
public sealed class EventRecorder<T>
{
    private readonly List<T> _items = new();
    private readonly SemaphoreSlim _signal = new(0);
    private int _consumed;

    public void Add(T item)
    {
        lock (_items)
            _items.Add(item);
        _signal.Release();
    }

    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_items)
                return _items.ToArray();
        }
    }

    public int Count => Items.Count;

    public async Task<T> NextAsync()
    {
        if (!await _signal.WaitAsync(TaskExtensions.DefaultTimeout))
            throw new TimeoutException("Beklenen olay gelmedi.");

        lock (_items)
            return _items[_consumed++];
    }
}
