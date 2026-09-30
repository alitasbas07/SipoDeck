using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Storage;
using SipoDeck.Core.Tests.Helpers;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests;

public class AtomicStorageTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string[] TempFiles(string? directory = null)
        => Directory.GetFiles(directory ?? _dir.Path, "*.tmp");

    [Fact]
    public void SettingsSave_LeavesNoTempFile_AndOverwritesExisting()
    {
        var path = _dir.FilePath("settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings();
        settings.Connection.Host = "1.1.1.1";
        settings.Connection.Port = 80;
        store.Save(settings);
        settings.Connection.Host = "2.2.2.2";
        store.Save(settings);

        Assert.Empty(TempFiles());
        Assert.Equal("2.2.2.2", store.Load().Connection.Host);
    }

    [Fact]
    public void ProfileSave_LeavesNoTempFile_AndOverwritesExisting()
    {
        var path = _dir.FilePath("profiles.json");
        var store = new ProfileStore(path);
        var data = ProfilesData.CreateDefault();
        store.Save(data);
        data.Profiles[0].Name = "Yeni";
        store.Save(data);

        Assert.Empty(TempFiles());
        Assert.Equal("Yeni", store.Load().Profiles[0].Name);
    }

    [Fact]
    public void Save_CreatesMissingDirectoryForCustomPath()
    {
        var path = Path.Combine(_dir.Path, "a", "b", "settings.json");
        new SettingsStore(path).Save(new AppSettings());
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SettingsSave_WhenTargetIsDirectory_ThrowsAndCleansTemp()
    {
        var path = _dir.FilePath("settings.json");
        Directory.CreateDirectory(path);
        var store = new SettingsStore(path);

        Assert.ThrowsAny<Exception>(() => store.Save(new AppSettings()));
        Assert.True(Directory.Exists(path));
        Assert.Empty(TempFiles());
    }

    [Fact]
    public void ProfileSave_WhenTargetLocked_KeepsExistingContentAndCleansTemp()
    {
        var path = _dir.FilePath("profiles.json");
        var store = new ProfileStore(path);
        var data = ProfilesData.CreateDefault();
        data.Profiles[0].Name = "Orijinal";
        store.Save(data);
        var before = File.ReadAllText(path);

        // Hedef dosya açık ve paylaşımsız tutulduğu için üzerine taşıma başarısız olur (Windows).
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            data.Profiles[0].Name = "Degisen";
            Assert.ThrowsAny<Exception>(() => store.Save(data));
        }

        Assert.Equal(before, File.ReadAllText(path));
        Assert.Empty(TempFiles());
    }

    [Fact]
    public void CustomPath_DoesNotCreateRealAppDataRoot()
    {
        var existedBefore = Directory.Exists(AppDataPaths.RootDirectory);
        var settingsPath = _dir.FilePath("s.json");
        var profilesPath = _dir.FilePath("p.json");

        new SettingsStore(settingsPath).Save(new AppSettings());
        new ProfileStore(profilesPath).Save(ProfilesData.CreateDefault());

        Assert.Equal(existedBefore, Directory.Exists(AppDataPaths.RootDirectory));
    }

    [Fact]
    public void ExistingFormat_StillLoads()
    {
        File.WriteAllText(_dir.FilePath("settings.json"), """
            { "connection": { "connectionType": "Serial", "serialPortName": "COM4", "baudRate": 9600 },
              "input": { "fnKey": 2, "longPressThresholdMilliseconds": 600, "profileSwitchMap": { "1": "p1" } },
              "application": { "runAtStartup": true } }
            """);
        File.WriteAllText(_dir.FilePath("profiles.json"), """
            { "version": 1, "activeProfileId": "p1",
              "profiles": [ { "id": "p1", "name": "A", "isEnabled": true,
                "keys": { "1": { "action": { "type": "openUrl", "url": "https://example.com" } } },
                "combinations": [ { "keys": [1, 2], "action": { "type": "wait", "milliseconds": 10 } } ] } ] }
            """);

        var settings = new SettingsStore(_dir.FilePath("settings.json")).Load();
        var data = new ProfileStore(_dir.FilePath("profiles.json")).Load();

        Assert.Equal(ConnectionType.Serial, settings.Connection.ConnectionType);
        Assert.Equal("p1", settings.Input.ProfileSwitchMap[1]);
        Assert.Equal("p1", data.ActiveProfileId);
        Assert.IsType<OpenUrlActionConfig>(data.Profiles[0].Keys[1].Action);
        Assert.Single(data.Profiles[0].Combinations);
    }

    [Fact]
    public void AppSettingsClone_IsIndependent()
    {
        var original = new AppSettings();
        original.Connection.ConnectionType = ConnectionType.Serial;
        original.Connection.Host = "h";
        original.Input.ProfileSwitchMap[1] = "p1";

        var clone = original.Clone();
        clone.Connection.Host = "changed";
        clone.Input.ProfileSwitchMap[2] = "p2";

        Assert.Equal(ConnectionType.Serial, clone.Connection.ConnectionType);
        Assert.Equal("h", original.Connection.Host);
        Assert.Single(original.Input.ProfileSwitchMap);
    }

    [Fact]
    public void ProfilesDataClone_IsIndependent_AndKeepsPolymorphicActions()
    {
        var original = new ProfilesData { ActiveProfileId = "p1" };
        var profile = new ProfileData { Id = "p1", Name = "A" };
        profile.Keys[1] = new KeyConfig
        {
            Action = new ActionChainConfig
            {
                Actions = { new OpenUrlActionConfig { Url = "u" }, new WaitActionConfig { Milliseconds = 5 } }
            },
            LongPressAction = new VolumeActionConfig { Command = VolumeCommand.Mute }
        };
        profile.Combinations.Add(new CombinationConfig { Keys = { 1, 2 }, Action = new OpenUrlActionConfig { Url = "c" } });
        original.Profiles.Add(profile);

        var clone = original.Clone();
        var cloneChain = Assert.IsType<ActionChainConfig>(clone.Profiles[0].Keys[1].Action);
        Assert.IsType<WaitActionConfig>(cloneChain.Actions[1]);
        Assert.IsType<VolumeActionConfig>(clone.Profiles[0].Keys[1].LongPressAction);

        clone.Profiles[0].Name = "B";
        cloneChain.Actions.Clear();
        clone.Profiles[0].Combinations.Clear();
        clone.Profiles.Add(new ProfileData { Id = "p2", Name = "x" });

        Assert.Equal("A", original.Profiles[0].Name);
        Assert.Equal(2, ((ActionChainConfig)original.Profiles[0].Keys[1].Action!).Actions.Count);
        Assert.Single(original.Profiles[0].Combinations);
        Assert.Single(original.Profiles);
    }
}
