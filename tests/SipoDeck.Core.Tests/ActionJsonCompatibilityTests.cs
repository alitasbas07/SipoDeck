using System.Text.Json;
using System.Text.Json.Serialization;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Tests.Helpers;

namespace SipoDeck.Core.Tests;

public class ActionJsonCompatibilityTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static T RoundTrip<T>(T config) where T : ActionConfig
        => (T)JsonSerializer.Deserialize<ActionConfig>(JsonSerializer.Serialize<ActionConfig>(config, Options), Options)!;

    [Fact]
    public void KeyboardShortcut_RoundTrips()
        => Assert.Equal("Ctrl+Shift+M", RoundTrip(new KeyboardShortcutActionConfig { Shortcut = "Ctrl+Shift+M" }).Shortcut);

    [Fact]
    public void TypeText_RoundTrips()
        => Assert.Equal("merhaba\nçğış", RoundTrip(new TypeTextActionConfig { Text = "merhaba\nçğış" }).Text);

    [Fact]
    public void OpenPath_RoundTrips()
        => Assert.Equal(@"C:\Users\x\Belgeler", RoundTrip(new OpenPathActionConfig { Path = @"C:\Users\x\Belgeler" }).Path);

    [Fact]
    public void SwitchProfile_RoundTrips()
        => Assert.Equal("p2", RoundTrip(new SwitchProfileActionConfig { ProfileId = "p2" }).ProfileId);

    [Fact]
    public void NewActions_NestedInChain_RoundTrip()
    {
        var chain = new ActionChainConfig
        {
            Actions =
            {
                new KeyboardShortcutActionConfig { Shortcut = "F5" },
                new TypeTextActionConfig { Text = "x" },
                new OpenPathActionConfig { Path = "p" },
                new SwitchProfileActionConfig { ProfileId = "b" },
                new VolumeActionConfig { Command = VolumeCommand.Mute }
            }
        };

        var back = RoundTrip(chain);

        Assert.Equal(5, back.Actions.Count);
        Assert.IsType<KeyboardShortcutActionConfig>(back.Actions[0]);
        Assert.IsType<TypeTextActionConfig>(back.Actions[1]);
        Assert.IsType<OpenPathActionConfig>(back.Actions[2]);
        Assert.IsType<SwitchProfileActionConfig>(back.Actions[3]);
        Assert.Equal(VolumeCommand.Mute, ((VolumeActionConfig)back.Actions[4]).Command);
    }

    [Fact]
    public void NewActions_InProfilesData_SurviveStoreSaveLoad()
    {
        using var dir = new TempDirectory();
        var store = new ProfileStore(dir.FilePath("profiles.json"));
        var data = new ProfilesData
        {
            ActiveProfileId = "a",
            Profiles =
            {
                new ProfileData
                {
                    Id = "a", Name = "A",
                    Keys = { [0] = new KeyConfig { Action = new KeyboardShortcutActionConfig { Shortcut = "Win+Up" } } }
                }
            }
        };

        store.Save(data);
        var loaded = store.Load();

        var action = Assert.IsType<KeyboardShortcutActionConfig>(loaded.Profiles[0].Keys[0].Action);
        Assert.Equal("Win+Up", action.Shortcut);
    }

    [Fact]
    public void LegacyProfilesJson_WithOnlyOldTypes_StillLoads()
    {
        const string legacy = """
        {
          "version": 1,
          "activeProfileId": "p1",
          "profiles": [
            {
              "id": "p1",
              "name": "Eski",
              "isEnabled": true,
              "keys": {
                "0": { "action": { "type": "runProgram", "filePath": "notepad.exe", "arguments": "a.txt" } },
                "1": { "action": { "type": "openUrl", "url": "https://example.com" },
                       "longPressAction": { "type": "chain", "actions": [
                          { "type": "wait", "milliseconds": 250 },
                          { "type": "notification", "title": "T", "message": "M" },
                          { "type": "volume", "command": "Mute" },
                          { "type": "media", "command": "Next" } ] } }
              },
              "combinations": []
            }
          ]
        }
        """;
        using var dir = new TempDirectory();
        var path = dir.FilePath("profiles.json");
        File.WriteAllText(path, legacy);

        var data = new ProfileStore(path).Load();

        Assert.Equal("p1", data.ActiveProfileId);
        var profile = Assert.Single(data.Profiles);
        Assert.Equal("Eski", profile.Name);
        Assert.Equal("notepad.exe", Assert.IsType<RunProgramActionConfig>(profile.Keys[0].Action).FilePath);
        Assert.IsType<OpenUrlActionConfig>(profile.Keys[1].Action);
        var chain = Assert.IsType<ActionChainConfig>(profile.Keys[1].LongPressAction);
        Assert.Equal(4, chain.Actions.Count);
        Assert.Equal(250, Assert.IsType<WaitActionConfig>(chain.Actions[0]).Milliseconds);
        Assert.Equal(VolumeCommand.Mute, Assert.IsType<VolumeActionConfig>(chain.Actions[2]).Command);
        Assert.Equal(MediaCommand.Next, Assert.IsType<MediaActionConfig>(chain.Actions[3]).Command);
    }
}
