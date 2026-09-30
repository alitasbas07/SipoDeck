using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Tests.Helpers;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    // Sürüm 1 biçiminde sabit örnek dosya; alan adları veya enum adları değişirse bu test kırılmalıdır.
    private const string FixedSettingsJson = """
        {
          "connection": {
            "connectionType": "Serial",
            "host": "192.168.1.50",
            "port": 8080,
            "serialPortName": "COM7",
            "baudRate": 9600
          },
          "input": {
            "fnKey": 4,
            "longPressThresholdMilliseconds": 750,
            "profileSwitchMap": { "1": "profile-a", "2": "profile-b" }
          },
          "application": {
            "runAtStartup": true,
            "runInSystemTray": false,
            "autoReconnect": false
          }
        }
        """;

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var store = new SettingsStore(_dir.FilePath("settings.json"));
        var settings = new AppSettings();
        settings.Connection.ConnectionType = ConnectionType.Serial;
        settings.Connection.Host = "10.0.0.2";
        settings.Connection.Port = 81;
        settings.Connection.SerialPortName = "COM3";
        settings.Connection.BaudRate = 57600;
        settings.Input.FnKey = 2;
        settings.Input.LongPressThresholdMilliseconds = 900;
        settings.Input.ProfileSwitchMap[3] = "p3";
        settings.Application.RunAtStartup = true;
        settings.Application.AutoReconnect = false;

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(ConnectionType.Serial, loaded.Connection.ConnectionType);
        Assert.Equal("10.0.0.2", loaded.Connection.Host);
        Assert.Equal(81, loaded.Connection.Port);
        Assert.Equal("COM3", loaded.Connection.SerialPortName);
        Assert.Equal(57600, loaded.Connection.BaudRate);
        Assert.Equal(2, loaded.Input.FnKey);
        Assert.Equal(900, loaded.Input.LongPressThresholdMilliseconds);
        Assert.Equal("p3", loaded.Input.ProfileSwitchMap[3]);
        Assert.True(loaded.Application.RunAtStartup);
        Assert.False(loaded.Application.AutoReconnect);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var loaded = new SettingsStore(_dir.FilePath("missing.json")).Load();

        AssertDefaults(loaded);
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("{ \"connection\": ")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{\"connection\":{\"port\":\"abc\"}}")]
    public void Load_CorruptFile_ReturnsDefaults(string content)
    {
        var path = _dir.FilePath("settings.json");
        File.WriteAllText(path, content);

        AssertDefaults(new SettingsStore(path).Load());
    }

    [Fact]
    public void Load_PartialFile_FillsMissingValuesWithDefaults()
    {
        var path = _dir.FilePath("settings.json");
        File.WriteAllText(path, "{\"connection\":{\"host\":\"h\"}}");

        var loaded = new SettingsStore(path).Load();

        Assert.Equal("h", loaded.Connection.Host);
        Assert.Equal(SettingsDefaults.BaudRate, loaded.Connection.BaudRate);
        Assert.Equal(SettingsDefaults.LongPressThresholdMilliseconds, loaded.Input.LongPressThresholdMilliseconds);
        Assert.Equal(SettingsDefaults.RunInSystemTray, loaded.Application.RunInSystemTray);
    }

    [Fact]
    public void Load_FixedSampleFile_IsBackwardCompatible()
    {
        var path = _dir.FilePath("settings.json");
        File.WriteAllText(path, FixedSettingsJson);

        var loaded = new SettingsStore(path).Load();

        Assert.Equal(ConnectionType.Serial, loaded.Connection.ConnectionType);
        Assert.Equal("192.168.1.50", loaded.Connection.Host);
        Assert.Equal(8080, loaded.Connection.Port);
        Assert.Equal("COM7", loaded.Connection.SerialPortName);
        Assert.Equal(9600, loaded.Connection.BaudRate);
        Assert.Equal(4, loaded.Input.FnKey);
        Assert.Equal(750, loaded.Input.LongPressThresholdMilliseconds);
        Assert.Equal("profile-a", loaded.Input.ProfileSwitchMap[1]);
        Assert.Equal("profile-b", loaded.Input.ProfileSwitchMap[2]);
        Assert.True(loaded.Application.RunAtStartup);
        Assert.False(loaded.Application.RunInSystemTray);
        Assert.False(loaded.Application.AutoReconnect);
    }

    private static void AssertDefaults(AppSettings s)
    {
        Assert.Equal(SettingsDefaults.ConnectionType, s.Connection.ConnectionType);
        Assert.Equal(string.Empty, s.Connection.Host);
        Assert.Equal(0, s.Connection.Port);
        Assert.Equal(SettingsDefaults.BaudRate, s.Connection.BaudRate);
        Assert.Equal(SettingsDefaults.FnKey, s.Input.FnKey);
        Assert.Equal(SettingsDefaults.LongPressThresholdMilliseconds, s.Input.LongPressThresholdMilliseconds);
        Assert.Empty(s.Input.ProfileSwitchMap);
        Assert.Equal(SettingsDefaults.RunAtStartup, s.Application.RunAtStartup);
        Assert.Equal(SettingsDefaults.RunInSystemTray, s.Application.RunInSystemTray);
        Assert.Equal(SettingsDefaults.AutoReconnect, s.Application.AutoReconnect);
    }
}

public class ProfileStoreTests : IDisposable
{
    // Sürüm 1 biçiminde sabit örnek dosya; tüm ActionConfig türlerini içerir.
    private const string FixedProfilesJson = """
        {
          "version": 1,
          "activeProfileId": "profile-a",
          "profiles": [
            {
              "id": "profile-a",
              "name": "Ana Profil",
              "isEnabled": true,
              "keys": {
                "1": {
                  "action": { "type": "openUrl", "url": "https://example.com" },
                  "longPressAction": { "type": "notification", "title": "T", "message": "M" }
                },
                "2": { "action": { "type": "runProgram", "filePath": "C:\\Tools\\app.exe", "arguments": "-x", "workingDirectory": "C:\\Tools" } },
                "3": { "action": { "type": "volume", "command": "Mute" } },
                "4": { "action": { "type": "media", "command": "PlayPause" } },
                "5": { "action": { "type": "wait", "milliseconds": 250 } },
                "6": {
                  "action": {
                    "type": "chain",
                    "actions": [
                      { "type": "wait", "milliseconds": 10 },
                      { "type": "volume", "command": "Up" }
                    ]
                  }
                }
              },
              "combinations": [
                { "keys": [2, 1], "action": { "type": "media", "command": "Next" } },
                { "keys": [1], "action": { "type": "media", "command": "Stop" } }
              ]
            },
            { "id": "profile-b", "name": "Kapalı", "isEnabled": false, "keys": {}, "combinations": [] }
          ]
        }
        """;

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void SaveThenLoad_RoundTripsProfilesAndActions()
    {
        var store = new ProfileStore(_dir.FilePath("profiles.json"));
        var data = new ProfilesData { ActiveProfileId = "p1" };
        var profile = new ProfileData { Id = "p1", Name = "P1" };
        profile.Keys[1] = new KeyConfig
        {
            Action = new OpenUrlActionConfig { Url = "https://example.com" },
            LongPressAction = new WaitActionConfig { Milliseconds = 5 }
        };
        profile.Keys[2] = new KeyConfig
        {
            Action = new ActionChainConfig
            {
                Actions = { new VolumeActionConfig { Command = VolumeCommand.Down }, new MediaActionConfig { Command = MediaCommand.Previous } }
            }
        };
        profile.Combinations.Add(new CombinationConfig
        {
            Keys = { 1, 2 },
            Action = new RunProgramActionConfig { FilePath = "x.exe", Arguments = "a" }
        });
        data.Profiles.Add(profile);

        store.Save(data);
        var loaded = store.Load();

        Assert.Equal("p1", loaded.ActiveProfileId);
        var p = Assert.Single(loaded.Profiles);
        Assert.Equal("P1", p.Name);
        Assert.Equal("https://example.com", Assert.IsType<OpenUrlActionConfig>(p.Keys[1].Action).Url);
        Assert.Equal(5, Assert.IsType<WaitActionConfig>(p.Keys[1].LongPressAction).Milliseconds);
        var chain = Assert.IsType<ActionChainConfig>(p.Keys[2].Action);
        Assert.Equal(VolumeCommand.Down, Assert.IsType<VolumeActionConfig>(chain.Actions[0]).Command);
        Assert.Equal(MediaCommand.Previous, Assert.IsType<MediaActionConfig>(chain.Actions[1]).Command);
        var combo = Assert.Single(p.Combinations);
        Assert.Equal(new[] { 1, 2 }, combo.Keys);
        Assert.Equal("a", Assert.IsType<RunProgramActionConfig>(combo.Action).Arguments);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaultProfile()
    {
        var loaded = new ProfileStore(_dir.FilePath("missing.json")).Load();

        var profile = Assert.Single(loaded.Profiles);
        Assert.Equal(profile.Id, loaded.ActiveProfileId);
        Assert.True(profile.IsEnabled);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{ \"profiles\": [")]
    public void Load_CorruptFile_ReturnsDefaultProfile(string content)
    {
        var path = _dir.FilePath("profiles.json");
        File.WriteAllText(path, content);

        var loaded = new ProfileStore(path).Load();

        Assert.Single(loaded.Profiles);
        Assert.NotNull(loaded.ActiveProfileId);
    }

    [Fact]
    public void Load_FixedSampleFile_IsBackwardCompatible()
    {
        var path = _dir.FilePath("profiles.json");
        File.WriteAllText(path, FixedProfilesJson);

        var loaded = new ProfileStore(path).Load();

        Assert.Equal(1, loaded.Version);
        Assert.Equal("profile-a", loaded.ActiveProfileId);
        Assert.Equal(2, loaded.Profiles.Count);

        var profile = loaded.Profiles[0].ToProfile();
        Assert.Equal("Ana Profil", profile.Name);
        Assert.IsType<OpenUrlAction>(profile.Keys[1].Action);
        Assert.Equal("https://example.com", ((OpenUrlAction)profile.Keys[1].Action!).Url);
        Assert.IsType<NotificationAction>(profile.Keys[1].LongPressAction);
        Assert.True(profile.Keys[1].HasLongPress);

        var run = Assert.IsType<RunProgramAction>(profile.Keys[2].Action);
        Assert.Equal("C:\\Tools\\app.exe", run.FilePath);
        Assert.Equal("-x", run.Arguments);
        Assert.Equal("C:\\Tools", run.WorkingDirectory);

        Assert.Equal(VolumeCommand.Mute, Assert.IsType<VolumeAction>(profile.Keys[3].Action).Command);
        Assert.Equal(MediaCommand.PlayPause, Assert.IsType<MediaAction>(profile.Keys[4].Action).Command);
        Assert.Equal(TimeSpan.FromMilliseconds(250), Assert.IsType<WaitAction>(profile.Keys[5].Action).Duration);

        var chain = Assert.IsType<ActionChain>(profile.Keys[6].Action);
        Assert.Collection(chain.Actions, a => Assert.IsType<WaitAction>(a), a => Assert.IsType<VolumeAction>(a));

        // Tek tuşlu kombinasyon geçersiz sayılıp atlanır; iki tuşlu kombinasyon korunur.
        var combo = Assert.Single(profile.Combinations);
        Assert.Equal(new[] { 1, 2 }, combo.Key.Keys);
        Assert.Equal(MediaCommand.Next, Assert.IsType<MediaAction>(combo.Value).Command);

        Assert.False(loaded.Profiles[1].ToProfile().IsEnabled);
    }

    [Fact]
    public void Load_UnknownActionType_DoesNotThrow()
    {
        // Bilinmeyen eylem türü (ör. yeni sürümde yazılmış dosya) uygulamayı çökertmemeli.
        var path = _dir.FilePath("profiles.json");
        File.WriteAllText(path, """
            { "profiles": [ { "id": "a", "name": "A", "keys": { "1": { "action": { "type": "futureAction" } } } } ] }
            """);

        var loaded = new ProfileStore(path).Load();

        Assert.NotNull(loaded);
    }
}
