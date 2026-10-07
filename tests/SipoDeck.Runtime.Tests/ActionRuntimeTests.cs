using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Runtime.Tests.Helpers;

namespace SipoDeck.Runtime.Tests;

/// <summary>ActionServices.ProfileSwitcher statik olduğundan bu testler diğerleriyle paralel çalışmaz.</summary>
[CollectionDefinition("ActionServicesRuntime", DisableParallelization = true)]
public sealed class ActionServicesRuntimeCollection;

[Collection("ActionServicesRuntime")]
public sealed class ActionRuntimeTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private AppRuntime CreateRuntime() => new(_dir.SettingsFile, _dir.ProfilesFile);

    private void SeedTwoProfiles()
        => new ProfileStore(_dir.ProfilesFile).Save(new ProfilesData
        {
            ActiveProfileId = "a",
            Profiles =
            {
                new ProfileData { Id = "a", Name = "A" },
                new ProfileData { Id = "b", Name = "B" }
            }
        });

    [Fact]
    public void GetActionCatalog_ReturnsCatalogWithBuiltInTypes()
    {
        var catalog = CreateRuntime().GetActionCatalog();

        Assert.NotNull(catalog);
        Assert.NotNull(catalog.Find("switchProfile"));
        Assert.NotNull(catalog.Find("chain"));
        Assert.Same(ActionCatalog.Default, catalog);
    }

    [Fact]
    public async Task Dispatch_SwitchProfileAction_ChangesActiveProfile()
    {
        SeedTwoProfiles();
        var runtime = CreateRuntime();
        var events = new EventRecorder<string?>();
        runtime.ActiveProfileChanged += (_, id) => events.Add(id);

        await runtime.StartAsync();
        try
        {
            Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);

            runtime.Dispatch(new SwitchProfileAction("b"));

            Assert.Equal("b", await events.NextAsync());
            Assert.Equal("b", runtime.GetProfilesSnapshot().ActiveProfileId);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Dispatch_SwitchProfileAction_UnknownProfile_ReportsActionFailed_AndKeepsActive()
    {
        SeedTwoProfiles();
        var runtime = CreateRuntime();
        var failures = new EventRecorder<string>();
        runtime.ActionFailed += (_, e) => failures.Add(e.Exception.Message);

        await runtime.StartAsync();
        try
        {
            runtime.Dispatch(new SwitchProfileAction("yok"));

            await failures.NextAsync();
            Assert.Equal("a", runtime.GetProfilesSnapshot().ActiveProfileId);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Start_SetsProfileSwitcher_StopClearsIt()
    {
        SeedTwoProfiles();
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        Assert.NotNull(ActionServices.ProfileSwitcher);

        await runtime.StopAsync();
        Assert.Null(ActionServices.ProfileSwitcher);
    }
}
