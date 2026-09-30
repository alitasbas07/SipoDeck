using SipoDeck.Core.Profiles;

namespace SipoDeck.Core.Tests;

public class ProfileManagerTests
{
    private static Profile P(string id, bool enabled = true) => new(id, id) { IsEnabled = enabled };

    private static (ProfileManager Manager, List<string?> Events) Create(params Profile[] profiles)
    {
        var manager = new ProfileManager();
        foreach (var p in profiles)
            manager.Add(p);
        var events = new List<string?>();
        manager.ActiveProfileChanged += (_, id) => events.Add(id);
        return (manager, events);
    }

    [Fact]
    public void Add_FirstEnabledBecomesActive_AndRaisesEvent()
    {
        var manager = new ProfileManager();
        var events = new List<string?>();
        manager.ActiveProfileChanged += (_, id) => events.Add(id);

        manager.Add(P("a", enabled: false));
        Assert.Null(manager.ActiveProfile);
        manager.Add(P("b"));
        manager.Add(P("c"));

        Assert.Equal("b", manager.ActiveProfile!.Id);
        Assert.Equal(new string?[] { "b" }, events);
    }

    [Fact]
    public void SwitchTo_RaisesOnlyOnRealChange()
    {
        var (manager, events) = Create(P("a"), P("b"), P("off", enabled: false));

        Assert.True(manager.SwitchTo("a"));
        Assert.Empty(events);
        Assert.True(manager.SwitchTo("b"));
        Assert.False(manager.SwitchTo("off"));
        Assert.False(manager.SwitchTo("none"));

        Assert.Equal("b", manager.ActiveProfile!.Id);
        Assert.Equal(new string?[] { "b" }, events);
    }

    [Fact]
    public void Remove_InactiveProfile_KeepsActive()
    {
        var (manager, events) = Create(P("a"), P("b"));

        Assert.True(manager.Remove("b"));
        Assert.Equal("a", manager.ActiveProfile!.Id);
        Assert.Empty(events);
        Assert.False(manager.Remove("b"));
    }

    [Fact]
    public void Remove_ActiveProfile_SwitchesToAnotherEnabled()
    {
        var (manager, events) = Create(P("a"), P("off", enabled: false), P("c"));

        Assert.True(manager.Remove("a"));

        Assert.Equal("c", manager.ActiveProfile!.Id);
        Assert.Equal(new string?[] { "c" }, events);
    }

    [Fact]
    public void Remove_LastProfile_ClearsActive()
    {
        var (manager, events) = Create(P("a"));

        manager.Remove("a");

        Assert.Null(manager.ActiveProfile);
        Assert.Empty(manager.Profiles);
        Assert.Equal(new string?[] { null }, events);
    }

    [Fact]
    public void Replace_ActiveProfile_PointsToNewObject_WithoutEvent()
    {
        var (manager, events) = Create(P("a"));
        var replacement = P("a");

        manager.Replace(replacement);

        Assert.Same(replacement, manager.ActiveProfile);
        Assert.Same(replacement, Assert.Single(manager.Profiles));
        Assert.Empty(events);
    }

    [Fact]
    public void Replace_ActiveDisabled_MovesToAnotherEnabled()
    {
        var (manager, events) = Create(P("a"), P("b"));

        manager.Replace(P("a", enabled: false));

        Assert.Equal("b", manager.ActiveProfile!.Id);
        Assert.Equal(new string?[] { "b" }, events);
    }

    [Fact]
    public void Replace_OnlyProfileDisabled_ClearsActive()
    {
        var (manager, events) = Create(P("a"));

        manager.Replace(P("a", enabled: false));

        Assert.Null(manager.ActiveProfile);
        Assert.Equal(new string?[] { null }, events);
    }

    [Fact]
    public void Replace_EnablingInactive_DoesNotChangeActive()
    {
        var (manager, events) = Create(P("a"), P("b", enabled: false));

        manager.Replace(P("b"));

        Assert.Equal("a", manager.ActiveProfile!.Id);
        Assert.Empty(events);
    }

    [Fact]
    public void Replace_UnknownId_Throws()
    {
        var (manager, _) = Create(P("a"));
        Assert.Throws<KeyNotFoundException>(() => manager.Replace(P("zzz")));
    }

    [Fact]
    public void Add_SameIdAsActive_ReplacesAndKeepsActivePointingToNew()
    {
        var (manager, events) = Create(P("a"));
        var replacement = P("a");

        manager.Add(replacement);

        Assert.Same(replacement, manager.ActiveProfile);
        Assert.Single(manager.Profiles);
        Assert.Empty(events);
    }

    [Fact]
    public void ReplaceAll_PicksPreferredActive_AndRaisesSingleEvent()
    {
        var (manager, events) = Create(P("old"));

        manager.ReplaceAll(new[] { P("x"), P("y"), P("z") }, "y");

        Assert.Equal("y", manager.ActiveProfile!.Id);
        Assert.Equal(3, manager.Profiles.Count);
        Assert.Equal(new string?[] { "y" }, events);
    }

    [Fact]
    public void ReplaceAll_UnknownPreferred_FallsBackToFirstEnabled()
    {
        var (manager, _) = Create();

        manager.ReplaceAll(new[] { P("x", enabled: false), P("y") }, "nope");

        Assert.Equal("y", manager.ActiveProfile!.Id);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var (manager, events) = Create(P("a"));

        manager.Clear();

        Assert.Null(manager.ActiveProfile);
        Assert.Empty(manager.Profiles);
        Assert.Equal(new string?[] { null }, events);
    }

    [Fact]
    public void Profiles_ReturnsSnapshot()
    {
        var (manager, _) = Create(P("a"));
        var snapshot = manager.Profiles;

        manager.Add(P("b"));

        Assert.Single(snapshot);
        Assert.Equal(2, manager.Profiles.Count);
    }

    [Fact]
    public async Task Event_IsRaisedOutsideLock()
    {
        var (manager, _) = Create(P("a"), P("b"));
        Task<Profile?>? worker = null;
        manager.ActiveProfileChanged += (_, _) => worker = Task.Run(() => manager.ActiveProfile);

        manager.SwitchTo("b");

        // Olay sırasında başka thread kilitli okuma yapabilmeli (kilit dışında tetiklenir).
        var seen = await worker!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("b", seen!.Id);
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_StayConsistent()
    {
        var (manager, _) = Create(P("base"));
        var failures = 0;
        using var cts = new CancellationTokenSource();

        var reader = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                var snapshot = manager.Profiles;
                var active = manager.ActiveProfile;
                if (snapshot.Count == 0 || active is null)
                    Interlocked.Increment(ref failures);
            }
        });

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                var id = "p" + (i % 10);
                manager.Add(P(id));
                manager.SwitchTo(id);
                manager.Replace(P(id));
                manager.Remove(id);
            }
        });

        await writer.WaitAsync(TimeSpan.FromSeconds(30));
        cts.Cancel();
        await reader.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, failures);
        Assert.Equal("base", manager.ActiveProfile!.Id);
    }
}
