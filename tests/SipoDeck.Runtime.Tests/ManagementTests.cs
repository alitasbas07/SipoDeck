using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime.Tests.Helpers;

namespace SipoDeck.Runtime.Tests;

/// <summary>Yönetim API'si: doğrulama, kalıcılık, profil kuralları ve olaylar (çoğu Runtime durmuşken diskteki veriyle).</summary>
public sealed class ManagementTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private AppRuntime CreateRuntime() => new(_dir.SettingsFile, _dir.ProfilesFile);

    private static ProfileData Profile(string id, string name, bool enabled = true)
        => new() { Id = id, Name = name, IsEnabled = enabled };

    private void SeedProfiles(string? activeId, params ProfileData[] profiles)
        => new ProfileStore(_dir.ProfilesFile).Save(new ProfilesData { ActiveProfileId = activeId, Profiles = profiles.ToList() });

    private void SeedSettings(AppSettings settings) => new SettingsStore(_dir.SettingsFile).Save(settings);

    private ProfilesData ProfilesOnDisk() => new ProfileStore(_dir.ProfilesFile).Load();

    private AppSettings SettingsOnDisk() => new SettingsStore(_dir.SettingsFile).Load();

    private static EventRecorder<string> Track(AppRuntime runtime)
    {
        var log = new EventRecorder<string>();
        runtime.SettingsChanged += (_, _) => log.Add("settings");
        runtime.ProfilesChanged += (_, _) => log.Add("profiles");
        runtime.ActiveProfileChanged += (_, id) => log.Add($"active:{id}");
        return log;
    }

    // ---- Ayarlar ----

    [Fact]
    public async Task UpdateSettings_Invalid_ReturnsFieldErrors_FileAndSnapshotUnchanged()
    {
        SeedProfiles("a", Profile("a", "A"));
        SeedSettings(new AppSettings());
        var before = File.ReadAllText(_dir.SettingsFile);
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var draft = runtime.GetSettingsSnapshot();
        draft.Connection.Host = "10.9.8.7";
        draft.Connection.Port = 70000;
        draft.Input.LongPressThresholdMilliseconds = 0;

        var result = await runtime.UpdateSettingsAsync(draft);

        Assert.False(result.IsSuccess);
        Assert.Null(result.FailureMessage);
        Assert.Contains(result.Errors, e => e.Field == "Connection.Port");
        Assert.Contains(result.Errors, e => e.Field == "Input.LongPressThresholdMilliseconds");
        Assert.Equal(before, File.ReadAllText(_dir.SettingsFile));
        Assert.Equal(0, runtime.GetSettingsSnapshot().Connection.Port);
        Assert.Equal(0, log.Count);
    }

    [Fact]
    public async Task UpdateSettings_UnknownProfileInSwitchMap_IsInvalid()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();

        var draft = runtime.GetSettingsSnapshot();
        draft.Input.ProfileSwitchMap[3] = "yok";

        var result = await runtime.UpdateSettingsAsync(draft);

        Assert.Contains(result.Errors, e => e.Field == "Input.ProfileSwitchMap[3]");
    }

    [Fact]
    public async Task UpdateSettings_Valid_WritesFile_UpdatesSnapshot_RaisesEvent_WhileStopped()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var draft = runtime.GetSettingsSnapshot();
        draft.Application.RunAtStartup = true;
        draft.Application.RunInSystemTray = false;
        draft.Input.FnKey = 4;

        var result = await runtime.UpdateSettingsAsync(draft);

        Assert.True(result.IsSuccess);
        Assert.Equal("settings", await log.NextAsync());
        Assert.Equal(RuntimeState.Stopped, runtime.State);
        var disk = SettingsOnDisk();
        Assert.True(disk.Application.RunAtStartup);
        Assert.False(disk.Application.RunInSystemTray);
        Assert.Equal(4, disk.Input.FnKey);
        Assert.Equal(4, runtime.GetSettingsSnapshot().Input.FnKey);
    }

    [Fact]
    public void Snapshots_AreIndependentCopies()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();

        var settings = runtime.GetSettingsSnapshot();
        settings.Input.FnKey = 9;
        var profiles = runtime.GetProfilesSnapshot();
        profiles.Profiles[0].Name = "Değişti";

        Assert.Equal(0, runtime.GetSettingsSnapshot().Input.FnKey);
        Assert.Equal("A", runtime.GetProfilesSnapshot().Profiles[0].Name);
        Assert.Equal("A", ProfilesOnDisk().Profiles[0].Name);
    }

    [Fact]
    public async Task UpdateSettings_WriteFailure_ReturnsFailed_SystemUnchanged()
    {
        SeedProfiles("a", Profile("a", "A"));
        Directory.CreateDirectory(_dir.SettingsFile); // hedef yol bir klasör: yazma başarısız olur
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var draft = runtime.GetSettingsSnapshot();
        draft.Input.FnKey = 7;

        var result = await runtime.UpdateSettingsAsync(draft);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureMessage));
        Assert.DoesNotContain(_dir.Path, result.FailureMessage);
        Assert.Equal(0, runtime.GetSettingsSnapshot().Input.FnKey);
        Assert.Equal(0, log.Count);
        Assert.Empty(Directory.GetFiles(_dir.Path, "*.tmp"));
    }

    // ---- Profiller ----

    [Fact]
    public async Task CreateProfile_EmptyId_GeneratesId_WritesAndRaisesEvent()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var result = await runtime.CreateProfileAsync(Profile("", "Yeni"));

        Assert.True(result.IsSuccess);
        Assert.Equal("profiles", await log.NextAsync());
        var disk = ProfilesOnDisk();
        Assert.Equal(2, disk.Profiles.Count);
        var created = disk.Profiles[1];
        Assert.Equal("Yeni", created.Name);
        Assert.Equal(32, created.Id.Length);
        Assert.Equal(2, runtime.GetProfilesSnapshot().Profiles.Count);
        Assert.Equal("a", disk.ActiveProfileId);
        Assert.Equal(1, log.Count);
    }

    [Fact]
    public async Task CreateProfile_DuplicateIdOrEmptyName_IsInvalid_FileUnchanged()
    {
        SeedProfiles("a", Profile("a", "A"));
        var before = File.ReadAllText(_dir.ProfilesFile);
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var duplicate = await runtime.CreateProfileAsync(Profile("a", "Kopya"));
        var noName = await runtime.CreateProfileAsync(Profile("b", "  "));

        Assert.Contains(duplicate.Errors, e => e.Field.EndsWith(".Id"));
        Assert.Contains(noName.Errors, e => e.Field.EndsWith(".Name"));
        Assert.Equal(before, File.ReadAllText(_dir.ProfilesFile));
        Assert.Single(runtime.GetProfilesSnapshot().Profiles);
        Assert.Equal(0, log.Count);
    }

    [Fact]
    public async Task CreateProfile_InvalidAction_ReportsActionField()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();

        var draft = Profile("b", "B");
        draft.Keys[1] = new KeyConfig { Action = new OpenUrlActionConfig { Url = "" } };

        var result = await runtime.CreateProfileAsync(draft);

        Assert.Contains(result.Errors, e => e.Field == "Profiles[1].Keys[1].Action.Url");
    }

    [Fact]
    public async Task CreateProfile_DoesNotKeepReferenceToDraft()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();
        var draft = Profile("b", "B");

        await runtime.CreateProfileAsync(draft);
        draft.Name = "Sonradan değişti";

        Assert.Equal("B", runtime.GetProfilesSnapshot().Profiles.Single(p => p.Id == "b").Name);
    }

    [Fact]
    public async Task UpdateProfile_UnknownId_IsInvalidOnId()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();

        var result = await runtime.UpdateProfileAsync(Profile("yok", "X"));

        Assert.Contains(result.Errors, e => e.Field == "Id");
    }

    [Fact]
    public async Task UpdateProfile_Valid_WritesAndRaisesEvent_Invalid_LeavesFile()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var draft = Profile("b", "B2");
        draft.Keys[2] = new KeyConfig { Action = new OpenUrlActionConfig { Url = "https://example.com" } };
        Assert.True((await runtime.UpdateProfileAsync(draft)).IsSuccess);
        Assert.Equal("profiles", await log.NextAsync());
        Assert.Equal("B2", ProfilesOnDisk().Profiles[1].Name);
        Assert.IsType<OpenUrlActionConfig>(ProfilesOnDisk().Profiles[1].Keys[2].Action);

        var before = File.ReadAllText(_dir.ProfilesFile);
        Assert.False((await runtime.UpdateProfileAsync(Profile("b", ""))).IsSuccess);
        Assert.Equal(before, File.ReadAllText(_dir.ProfilesFile));
        Assert.Equal("B2", runtime.GetProfilesSnapshot().Profiles[1].Name);
    }

    [Fact]
    public async Task UpdateProfile_DisablingActive_MovesActiveToAnotherEnabledProfile()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var result = await runtime.UpdateProfileAsync(Profile("a", "A", enabled: false));

        Assert.True(result.IsSuccess);
        Assert.Equal("b", ProfilesOnDisk().ActiveProfileId);
        Assert.Contains("active:b", log.Items);
    }

    [Fact]
    public async Task DeleteProfile_LastProfile_IsRejected()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();

        var result = await runtime.DeleteProfileAsync("a");

        Assert.Contains(result.Errors, e => e.Field == "Profiles");
        Assert.Single(ProfilesOnDisk().Profiles);
    }

    [Fact]
    public async Task DeleteProfile_UnknownId_IsInvalid()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();

        var result = await runtime.DeleteProfileAsync("yok");

        Assert.Contains(result.Errors, e => e.Field == "Id");
    }

    [Fact]
    public async Task DeleteProfile_ReferencedByFnMap_IsRejected_NothingChanges()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var settings = new AppSettings();
        settings.Input.ProfileSwitchMap[5] = "b";
        SeedSettings(settings);
        var before = File.ReadAllText(_dir.ProfilesFile);
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var result = await runtime.DeleteProfileAsync("b");

        Assert.Contains(result.Errors, e => e.Field == "Input.ProfileSwitchMap[5]" && !string.IsNullOrWhiteSpace(e.Message));
        Assert.Equal(before, File.ReadAllText(_dir.ProfilesFile));
        Assert.Equal(2, runtime.GetProfilesSnapshot().Profiles.Count);
        Assert.Equal(0, log.Count);
    }

    [Fact]
    public async Task DeleteProfile_Active_SwitchesToAnotherEnabled_PersistsInSameWrite()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B", enabled: false), Profile("c", "C"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var result = await runtime.DeleteProfileAsync("a");

        Assert.True(result.IsSuccess);
        var disk = ProfilesOnDisk();
        Assert.Equal(new[] { "b", "c" }, disk.Profiles.Select(p => p.Id));
        Assert.Equal("c", disk.ActiveProfileId);
        Assert.Equal("c", runtime.GetProfilesSnapshot().ActiveProfileId);
        Assert.Equal("profiles", await log.NextAsync());
        Assert.Equal("active:c", await log.NextAsync());
    }

    [Fact]
    public async Task DeleteProfile_ActiveWithNoOtherEnabled_ActiveBecomesNull()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B", enabled: false));
        var runtime = CreateRuntime();

        var result = await runtime.DeleteProfileAsync("a");

        Assert.True(result.IsSuccess);
        Assert.Null(ProfilesOnDisk().ActiveProfileId);
        Assert.Null(runtime.GetProfilesSnapshot().ActiveProfileId);
    }

    [Fact]
    public async Task SetActiveProfile_UnknownOrDisabled_IsInvalid()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B", enabled: false));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var unknown = await runtime.SetActiveProfileAsync("yok");
        var disabled = await runtime.SetActiveProfileAsync("b");

        Assert.Contains(unknown.Errors, e => e.Field == "ActiveProfileId");
        Assert.Contains(disabled.Errors, e => e.Field == "ActiveProfileId");
        Assert.Equal("a", ProfilesOnDisk().ActiveProfileId);
        Assert.Equal(0, log.Count);
    }

    [Fact]
    public async Task SetActiveProfile_Valid_WritesAndRaisesSingleActiveEvent()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var result = await runtime.SetActiveProfileAsync("b");

        Assert.True(result.IsSuccess);
        Assert.Equal("active:b", await log.NextAsync());
        Assert.Equal("b", ProfilesOnDisk().ActiveProfileId);
        Assert.Equal("b", runtime.GetProfilesSnapshot().ActiveProfileId);
        Assert.Equal(1, log.Count);
    }

    [Fact]
    public async Task ProfileWriteFailure_ReturnsFailed_MemoryUnchanged()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();
        runtime.GetProfilesSnapshot(); // diskten yükle
        File.Delete(_dir.ProfilesFile);
        Directory.CreateDirectory(_dir.ProfilesFile); // yazma hedefi klasör
        var log = Track(runtime);

        var result = await runtime.SetActiveProfileAsync("b");

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureMessage));
        Assert.DoesNotContain(_dir.Path, result.FailureMessage);
        Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);
        Assert.Equal(0, log.Count);
    }

    [Fact]
    public async Task ConcurrentManagementCalls_AreSerialisedAndConsistent()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => runtime.CreateProfileAsync(Profile($"p{i}", $"Profil {i}")))))
            .WithTimeout();

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(21, runtime.GetProfilesSnapshot().Profiles.Count);
        Assert.Equal(21, ProfilesOnDisk().Profiles.Count);
        Assert.Equal(20, log.Items.Count(e => e == "profiles"));
    }

    [Fact]
    public async Task StartAfterManagementWhileStopped_UsesPersistedData()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var first = CreateRuntime();
        await first.SetActiveProfileAsync("b");

        var second = CreateRuntime();
        await second.StartAsync();
        try
        {
            Assert.Equal("b", second.GetProfilesSnapshot().ActiveProfileId);
        }
        finally
        {
            await second.StopAsync();
        }
    }

    // ---- Çalışan Runtime ----

    [Fact]
    public async Task Running_SetActiveAndDeleteActive_AreReflectedInSnapshot()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"), Profile("c", "C"));
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        try
        {
            Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);

            Assert.True((await runtime.SetActiveProfileAsync("b")).IsSuccess);
            Assert.Equal("b", runtime.GetProfilesSnapshot().ActiveProfileId);

            Assert.True((await runtime.DeleteProfileAsync("b")).IsSuccess);
            Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);
            Assert.Equal("a", ProfilesOnDisk().ActiveProfileId);
            Assert.Equal(new[] { "a", "c" }, runtime.GetProfilesSnapshot().Profiles.Select(p => p.Id));
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Running_ProfileWriteFailure_LeavesLiveSystemUnchanged()
    {
        SeedProfiles("a", Profile("a", "A"), Profile("b", "B"));
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        try
        {
            File.Delete(_dir.ProfilesFile);
            Directory.CreateDirectory(_dir.ProfilesFile);

            var result = await runtime.SetActiveProfileAsync("b");

            Assert.False(result.IsSuccess);
            Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Running_ActionChain_IsNotInterruptedBySettingsOrProfileSaves()
    {
        SeedProfiles("a", Profile("a", "A"));
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        try
        {
            var log = new List<string>();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gated = new GatedAction(log, "g", gate.Task);
            var queued = new RecordingAction(log, "q");
            runtime.Dispatch(gated);
            await gated.Started.WithTimeout();
            runtime.Dispatch(queued);

            // Girdi + bağlantı ayarı değişir; çalışan zincir ve kuyruktaki eylem etkilenmemeli.
            var settings = runtime.GetSettingsSnapshot();
            settings.Input.FnKey = 3;
            settings.Input.LongPressThresholdMilliseconds = 900;
            settings.Connection.Host = "127.0.0.1";
            settings.Connection.Port = 1;
            settings.Application.AutoReconnect = false;
            Assert.True((await runtime.UpdateSettingsAsync(settings).WithTimeout()).IsSuccess);
            Assert.True((await runtime.CreateProfileAsync(Profile("b", "B")).WithTimeout()).IsSuccess);
            Assert.True((await runtime.UpdateProfileAsync(Profile("b", "B2")).WithTimeout()).IsSuccess);
            Assert.True((await runtime.DeleteProfileAsync("b").WithTimeout()).IsSuccess);

            gate.SetResult();
            await queued.Completed.WithTimeout();
            Assert.Equal(new[] { "g", "q" }, log);

            // Aynı kuyruk hâlâ çalışıyor; yeni bir ayar kaydı iptal tetiklemez.
            var blocking = new BlockUntilCancelledAction();
            runtime.Dispatch(blocking);
            await blocking.Started.WithTimeout();
            var settings2 = runtime.GetSettingsSnapshot();
            settings2.Input.FnKey = 4;
            Assert.True((await runtime.UpdateSettingsAsync(settings2).WithTimeout()).IsSuccess);
            Assert.False(blocking.CancelObserved.IsCompleted);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    // ---- Diğer ----

    [Fact]
    public void GetAvailablePortNames_DoesNotThrow_AndHasNoDuplicates()
    {
        var names = CreateRuntime().GetAvailablePortNames();

        Assert.NotNull(names);
        Assert.Equal(names.Distinct().Count(), names.Count);
    }

    [Fact]
    public async Task Reconnect_WhenStopped_OrWithoutConnectionSettings_IsNoOp()
    {
        var runtime = CreateRuntime();
        await runtime.ReconnectAsync();
        Assert.Equal(RuntimeState.Stopped, runtime.State);

        await runtime.StartAsync();
        try
        {
            await runtime.ReconnectAsync();
            Assert.Equal(RuntimeState.Running, runtime.State);
            Assert.Equal(ConnectionState.Disconnected, runtime.DeviceConnectionState);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task ManagementResultAndEvents_DoNotExposeHostPortOrComPort()
    {
        SeedProfiles("a", Profile("a", "A"));
        Directory.CreateDirectory(_dir.SettingsFile); // yazma hatası mesajını da denetlemek için
        var runtime = CreateRuntime();
        var log = Track(runtime);

        var draft = runtime.GetSettingsSnapshot();
        draft.Connection.Host = "10.77.66.55";
        draft.Connection.Port = 54321;
        draft.Connection.SerialPortName = "COM97";
        var failed = await runtime.UpdateSettingsAsync(draft);

        draft.Connection.Port = 70000;
        var invalid = await runtime.UpdateSettingsAsync(draft);

        var all = string.Join("|", failed.FailureMessage, string.Join("|", invalid.Errors.Select(e => e.Field + e.Message)), string.Join("|", log.Items));
        Assert.DoesNotContain("10.77.66.55", all);
        Assert.DoesNotContain("54321", all);
        Assert.DoesNotContain("70000", all);
        Assert.DoesNotContain("COM97", all);
    }
}
