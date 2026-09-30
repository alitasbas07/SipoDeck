using SipoDeck.Core.Devices;
using SipoDeck.Core.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Protocol;
using SipoDeck.Core.Tests.Helpers;

namespace SipoDeck.Core.Tests;

public class InputEngineTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(500);

    private readonly List<string> _log = new();
    private readonly ProfileManager _profiles = new();
    private readonly RecordingDispatcher _dispatcher = new();
    private readonly FakeTimeProvider _time = new();
    private readonly Profile _profile = new("p1", "Profile 1");

    private RecordingAction A(string name) => new(name, _log);

    private InputEngine CreateEngine(int? fnKey = null, Dictionary<int, string>? map = null)
    {
        if (!_profiles.Profiles.Contains(_profile))
            _profiles.Add(_profile);
        return new InputEngine(_profiles, _dispatcher, Threshold, fnKey, map, _time);
    }

    private static KeyEvent Press(int key) => new("d1", key, ButtonState.Pressed, DateTimeOffset.UtcNow);

    private static KeyEvent Release(int key) => new("d1", key, ButtonState.Released, DateTimeOffset.UtcNow);

    [Fact]
    public void SingleKey_RunsOnRelease_NotOnPress()
    {
        _profile.Keys[1] = new KeyBinding(A("one"));
        using var engine = CreateEngine();

        engine.Process(Press(1));
        Assert.Empty(_dispatcher.Dispatched);

        engine.Process(Release(1));
        Assert.Equal(new[] { "one" }, _dispatcher.Names);
    }

    [Fact]
    public void UndefinedKey_ProducesNoAction()
    {
        _profile.Keys[1] = new KeyBinding(A("one"));
        using var engine = CreateEngine();

        engine.Process(Press(9));
        engine.Process(Release(9));

        Assert.Empty(_dispatcher.Dispatched);
    }

    [Fact]
    public void NonKeyEvent_IsIgnored()
    {
        using var engine = CreateEngine();

        engine.Process(new OtherEvent());

        Assert.Empty(_dispatcher.Dispatched);
    }

    [Fact]
    public void DisabledActiveProfile_ProducesNoAction()
    {
        _profile.Keys[1] = new KeyBinding(A("one"));
        using var engine = CreateEngine();
        _profile.IsEnabled = false;

        engine.Process(Press(1));
        engine.Process(Release(1));

        Assert.Empty(_dispatcher.Dispatched);
    }

    [Fact]
    public void Combination_TakesPriorityOverSingleKeys()
    {
        _profile.Keys[1] = new KeyBinding(A("single1"));
        _profile.Keys[2] = new KeyBinding(A("single2"));
        _profile.Combinations[new KeyCombination(new[] { 1, 2 })] = A("combo");
        using var engine = CreateEngine();

        engine.Process(Press(1));
        engine.Process(Press(2));
        Assert.Equal(new[] { "combo" }, _dispatcher.Names);

        engine.Process(Release(2));
        engine.Process(Release(1));
        Assert.Equal(new[] { "combo" }, _dispatcher.Names);
    }

    [Fact]
    public void Combination_PressedInReverseOrder_AlsoTriggers()
    {
        _profile.Combinations[new KeyCombination(new[] { 1, 2 })] = A("combo");
        using var engine = CreateEngine();

        engine.Process(Press(2));
        engine.Process(Press(1));

        Assert.Equal(new[] { "combo" }, _dispatcher.Names);
    }

    [Fact]
    public void SingleKey_AfterCombinationReleased_WorksAgain()
    {
        _profile.Keys[1] = new KeyBinding(A("single1"));
        _profile.Combinations[new KeyCombination(new[] { 1, 2 })] = A("combo");
        using var engine = CreateEngine();
        engine.Process(Press(1));
        engine.Process(Press(2));
        engine.Process(Release(2));
        engine.Process(Release(1));

        engine.Process(Press(1));
        engine.Process(Release(1));

        Assert.Equal(new[] { "combo", "single1" }, _dispatcher.Names);
    }

    [Fact]
    public void LongPress_FiresAtThreshold_AndReleaseDoesNotRunNormalAction()
    {
        _profile.Keys[1] = new KeyBinding(A("normal"), A("long"));
        using var engine = CreateEngine();

        engine.Process(Press(1));
        _time.Advance(Threshold - TimeSpan.FromMilliseconds(1));
        Assert.Empty(_dispatcher.Dispatched);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(new[] { "long" }, _dispatcher.Names);

        engine.Process(Release(1));
        Assert.Equal(new[] { "long" }, _dispatcher.Names);
    }

    [Fact]
    public void LongPress_ReleasedBeforeThreshold_RunsNormalActionOnly()
    {
        _profile.Keys[1] = new KeyBinding(A("normal"), A("long"));
        using var engine = CreateEngine();

        engine.Process(Press(1));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        engine.Process(Release(1));
        _time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "normal" }, _dispatcher.Names);
        Assert.Equal(0, _time.ActiveTimerCount);
    }

    [Fact]
    public void LongPress_FiresOnlyOncePerPress()
    {
        _profile.Keys[1] = new KeyBinding(A("normal"), A("long"));
        using var engine = CreateEngine();

        engine.Process(Press(1));
        _time.Advance(TimeSpan.FromSeconds(5));
        engine.Process(Release(1));
        engine.Process(Press(1));
        engine.Process(Release(1));

        Assert.Equal(new[] { "long", "normal" }, _dispatcher.Names);
    }

    [Fact]
    public void LongPress_DoesNotFire_WhenKeyConsumedByCombination()
    {
        _profile.Keys[1] = new KeyBinding(A("normal"), A("long"));
        _profile.Combinations[new KeyCombination(new[] { 1, 2 })] = A("combo");
        using var engine = CreateEngine();

        engine.Process(Press(1));
        engine.Process(Press(2));
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { "combo" }, _dispatcher.Names);
    }

    [Fact]
    public void FnPlusKey_SwitchesProfile_AndRunsNoAction()
    {
        _profiles.Add(_profile);
        var other = new Profile("p2", "Profile 2");
        _profiles.Add(other);
        _profile.Keys[1] = new KeyBinding(A("one"));
        using var engine = CreateEngine(fnKey: 0, map: new Dictionary<int, string> { [1] = "p2" });

        engine.Process(Press(0));
        engine.Process(Press(1));
        engine.Process(Release(1));
        engine.Process(Release(0));

        Assert.Same(other, _profiles.ActiveProfile);
        Assert.Empty(_dispatcher.Dispatched);
    }

    [Fact]
    public void KeyWithoutFn_DoesNotSwitchProfile()
    {
        _profiles.Add(_profile);
        var other = new Profile("p2", "Profile 2");
        _profiles.Add(other);
        using var engine = CreateEngine(fnKey: 0, map: new Dictionary<int, string> { [1] = "p2" });

        engine.Process(Press(1));
        engine.Process(Release(1));

        Assert.Same(_profile, _profiles.ActiveProfile);
    }

    [Fact]
    public void FnKeyAlone_ProducesNoAction()
    {
        _profile.Keys[0] = new KeyBinding(A("fn"));
        using var engine = CreateEngine(fnKey: 0);

        engine.Process(Press(0));
        engine.Process(Release(0));

        Assert.Empty(_dispatcher.Dispatched);
    }

    [Fact]
    public void FnPlusKey_WithUnknownTargetProfile_KeepsActiveProfile()
    {
        using var engine = CreateEngine(fnKey: 0, map: new Dictionary<int, string> { [1] = "missing" });

        engine.Process(Press(0));
        engine.Process(Press(1));

        Assert.Same(_profile, _profiles.ActiveProfile);
    }

    [Fact]
    public void Dispose_CancelsPendingLongPress()
    {
        _profile.Keys[1] = new KeyBinding(A("normal"), A("long"));
        var engine = CreateEngine();
        engine.Process(Press(1));

        engine.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.Empty(_dispatcher.Dispatched);
    }

    private sealed class OtherEvent : IDeviceEvent
    {
        public string DeviceId => "d1";

        public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
    }
}
