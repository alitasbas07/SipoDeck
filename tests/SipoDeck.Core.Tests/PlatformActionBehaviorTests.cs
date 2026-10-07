using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Tests;

/// <summary>ActionServices statik olduğundan, onu değiştiren testler paralel çalışmaz.</summary>
[CollectionDefinition("ActionServices", DisableParallelization = true)]
public sealed class ActionServicesCollection;

[Collection("ActionServices")]
public sealed class PlatformActionBehaviorTests : IDisposable
{
    private readonly IPlatformActions _originalPlatform = ActionServices.Platform;
    private readonly IProfileSwitcher? _originalSwitcher = ActionServices.ProfileSwitcher;
    private readonly FakePlatform _platform = new();

    public PlatformActionBehaviorTests() => ActionServices.Platform = _platform;

    public void Dispose()
    {
        ActionServices.Platform = _originalPlatform;
        ActionServices.ProfileSwitcher = _originalSwitcher;
    }

    private sealed class FakePlatform : IPlatformActions
    {
        public List<string> Calls { get; } = new();
        public KeyShortcut? Shortcut { get; private set; }

        public Task SetVolumeAsync(VolumeCommand command, CancellationToken cancellationToken = default)
        { Calls.Add($"volume:{command}"); return Task.CompletedTask; }

        public Task SendMediaAsync(MediaCommand command, CancellationToken cancellationToken = default)
        { Calls.Add($"media:{command}"); return Task.CompletedTask; }

        public Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default)
        { Calls.Add($"notify:{title}|{message}"); return Task.CompletedTask; }

        public Task SendShortcutAsync(KeyShortcut shortcut, CancellationToken cancellationToken = default)
        { Shortcut = shortcut; Calls.Add($"shortcut:{shortcut}"); return Task.CompletedTask; }

        public Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
        { Calls.Add($"type:{text}"); return Task.CompletedTask; }
    }

    private sealed class FakeSwitcher : IProfileSwitcher
    {
        public List<string> Ids { get; } = new();

        public Task SwitchProfileAsync(string profileId, CancellationToken cancellationToken = default)
        { Ids.Add(profileId); return Task.CompletedTask; }
    }

    [Fact]
    public async Task VolumeAction_CallsPlatform()
    {
        await new VolumeAction(VolumeCommand.Down).ExecuteAsync();
        Assert.Equal(new[] { "volume:Down" }, _platform.Calls);
    }

    [Fact]
    public async Task MediaAction_CallsPlatform()
    {
        await new MediaAction(MediaCommand.Previous).ExecuteAsync();
        Assert.Equal(new[] { "media:Previous" }, _platform.Calls);
    }

    [Fact]
    public async Task NotificationAction_CallsPlatform()
    {
        await new NotificationAction("Baslik", "Mesaj").ExecuteAsync();
        Assert.Equal(new[] { "notify:Baslik|Mesaj" }, _platform.Calls);
    }

    [Fact]
    public async Task KeyboardShortcutAction_SendsParsedShortcut()
    {
        await new KeyboardShortcutAction("ctrl+shift+m").ExecuteAsync();

        Assert.Equal(new KeyShortcut(true, false, true, false, "M"), _platform.Shortcut);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Foo")]
    public async Task KeyboardShortcutAction_InvalidShortcut_Throws_AndSendsNothing(string shortcut)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => new KeyboardShortcutAction(shortcut).ExecuteAsync());
        Assert.Empty(_platform.Calls);
    }

    [Fact]
    public async Task TypeTextAction_CallsPlatform()
    {
        await new TypeTextAction("merhaba").ExecuteAsync();
        Assert.Equal(new[] { "type:merhaba" }, _platform.Calls);
    }

    [Fact]
    public async Task ConfigToAction_ExecutesThroughPlatform()
    {
        await new ActionChainConfig
        {
            Actions =
            {
                new VolumeActionConfig { Command = VolumeCommand.Up },
                new KeyboardShortcutActionConfig { Shortcut = "F5" },
                new TypeTextActionConfig { Text = "x" }
            }
        }.ToAction().ExecuteAsync();

        Assert.Equal(new[] { "volume:Up", "shortcut:F5", "type:x" }, _platform.Calls);
    }

    [Fact]
    public async Task SwitchProfileAction_WithoutService_Throws()
    {
        ActionServices.ProfileSwitcher = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => new SwitchProfileAction("b").ExecuteAsync());
    }

    [Fact]
    public async Task SwitchProfileAction_WithService_ForwardsProfileId()
    {
        var switcher = new FakeSwitcher();
        ActionServices.ProfileSwitcher = switcher;

        await new SwitchProfileAction("b").ExecuteAsync();

        Assert.Equal(new[] { "b" }, switcher.Ids);
    }

    [Fact]
    public void Platform_SetToNull_FallsBackToNoOp()
    {
        ActionServices.Platform = null!;
        Assert.NotNull(ActionServices.Platform);
        Assert.True(new VolumeAction(VolumeCommand.Up).ExecuteAsync().IsCompletedSuccessfully);
    }
}
