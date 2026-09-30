using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime.Tests.Helpers;

namespace SipoDeck.Runtime.Tests;

/// <summary>Yerel sahte WebSocket cihazıyla: bağlantı/girdi ayarı uygulaması, yeniden bağlanma, FN ile aktif profil kalıcılığı.</summary>
public sealed class ManagementConnectionTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private AppRuntime CreateRuntime() => new(_dir.SettingsFile, _dir.ProfilesFile);

    private static ProfileData Profile(string id) => new() { Id = id, Name = id.ToUpperInvariant() };

    private void Seed(AppSettings settings, string active = "a")
    {
        new SettingsStore(_dir.SettingsFile).Save(settings);
        new ProfileStore(_dir.ProfilesFile).Save(new ProfilesData
        {
            ActiveProfileId = active,
            Profiles = { Profile("a"), Profile("b"), Profile("c") }
        });
    }

    private static AppSettings WifiSettings(int port, bool autoReconnect = true)
    {
        var settings = new AppSettings();
        settings.Connection.ConnectionType = ConnectionType.WiFi;
        settings.Connection.Host = "127.0.0.1";
        settings.Connection.Port = port;
        settings.Application.AutoReconnect = autoReconnect;
        return settings;
    }

    private static EventRecorder<string?> TrackActive(AppRuntime runtime)
    {
        var log = new EventRecorder<string?>();
        runtime.ActiveProfileChanged += (_, id) => log.Add(id);
        return log;
    }

    private static async Task PressAsync(FakeDeviceServer server, params int[] keys)
    {
        foreach (var key in keys)
            await server.SendButtonAsync(key, pressed: true);
        foreach (var key in keys.Reverse())
            await server.SendButtonAsync(key, pressed: false);
    }

    [Fact]
    public async Task FnKeyPress_ChangesActiveProfile_AndPersistsToProfilesJson()
    {
        await using var server = FakeDeviceServer.Start();
        var settings = WifiSettings(server.Port);
        settings.Input.FnKey = 0;
        settings.Input.ProfileSwitchMap[1] = "b";
        Seed(settings);
        var runtime = CreateRuntime();
        var active = TrackActive(runtime);

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();
            await PressAsync(server, 0, 1);

            Assert.Equal("b", await active.NextAsync());
            // Olay yazmadan sonra tetiklenir: dosya güncel olmalı, çift olay olmamalı.
            Assert.Equal("b", new ProfileStore(_dir.ProfilesFile).Load().ActiveProfileId);
            Assert.Equal("b", runtime.GetProfilesSnapshot().ActiveProfileId);

            // Yönetim çağrısı sonrası tek olay.
            Assert.True((await runtime.SetActiveProfileAsync("c")).IsSuccess);
            Assert.Equal("c", await active.NextAsync());
            Assert.Equal(2, active.Count);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task UpdateSettings_InputChange_ReplacesEngine_WithoutReconnect()
    {
        await using var server = FakeDeviceServer.Start();
        var settings = WifiSettings(server.Port);
        settings.Input.FnKey = 0;
        settings.Input.ProfileSwitchMap[1] = "b";
        Seed(settings);
        var runtime = CreateRuntime();
        var active = TrackActive(runtime);

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();
            await PressAsync(server, 0, 1);
            Assert.Equal("b", await active.NextAsync());
            Assert.True((await runtime.SetActiveProfileAsync("a")).IsSuccess);
            Assert.Equal("a", await active.NextAsync());

            var draft = runtime.GetSettingsSnapshot();
            draft.Input.FnKey = 5;
            draft.Input.ProfileSwitchMap.Clear();
            draft.Input.ProfileSwitchMap[6] = "c";
            Assert.True((await runtime.UpdateSettingsAsync(draft)).IsSuccess);

            // Eski FN (0+1) artık etkisiz; yeni FN (5+6) c profiline geçirir. Mesajlar sırayla işlenir.
            await PressAsync(server, 0, 1);
            await PressAsync(server, 5, 6);

            Assert.Equal("c", await active.NextAsync());
            Assert.Equal(3, active.Count);
            Assert.Equal(1, server.ConnectionCount);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task UpdateSettings_ConnectionChange_ReconnectsToNewEndpoint()
    {
        await using var first = FakeDeviceServer.Start();
        await using var second = FakeDeviceServer.Start();
        Seed(WifiSettings(first.Port));
        var runtime = CreateRuntime();
        var states = new EventRecorder<ConnectionState>();
        runtime.DeviceConnectionStateChanged += (_, e) => states.Add(e.State);

        await runtime.StartAsync();
        try
        {
            await first.WaitForConnectionAsync();
            Assert.Equal(ConnectionState.Connecting, await states.NextAsync());
            Assert.Equal(ConnectionState.Connected, await states.NextAsync());

            var draft = runtime.GetSettingsSnapshot();
            draft.Connection.Port = second.Port;
            Assert.True((await runtime.UpdateSettingsAsync(draft)).IsSuccess);

            await second.WaitForConnectionAsync();
            await first.WaitForDisconnectAsync();
            Assert.Equal(1, first.ConnectionCount);
            Assert.Equal(1, second.ConnectionCount);
            Assert.Equal(second.Port, new SettingsStore(_dir.SettingsFile).Load().Connection.Port);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task UpdateSettings_ConnectionChange_WithAutoReconnectOff_MakesSingleAttemptToNewEndpoint()
    {
        await using var first = FakeDeviceServer.Start();
        await using var second = FakeDeviceServer.Start();
        Seed(WifiSettings(first.Port, autoReconnect: false));
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        try
        {
            await first.WaitForConnectionAsync();

            var draft = runtime.GetSettingsSnapshot();
            draft.Connection.Port = second.Port;
            Assert.True((await runtime.UpdateSettingsAsync(draft)).IsSuccess);

            await second.WaitForConnectionAsync();
            await first.WaitForDisconnectAsync();
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task UpdateSettings_NonConnectionNonInputChange_DoesNotReconnect()
    {
        await using var server = FakeDeviceServer.Start();
        Seed(WifiSettings(server.Port));
        var runtime = CreateRuntime();
        var settingsEvents = new EventRecorder<string>();
        runtime.SettingsChanged += (_, _) => settingsEvents.Add("settings");

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();

            var draft = runtime.GetSettingsSnapshot();
            draft.Application.RunAtStartup = true;
            draft.Application.RunInSystemTray = false;
            Assert.True((await runtime.UpdateSettingsAsync(draft)).IsSuccess);

            await settingsEvents.NextAsync();
            Assert.Equal(1, server.ConnectionCount);
            Assert.Equal(ConnectionState.Connected, runtime.DeviceConnectionState);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task UpdateSettings_ConnectionSettingsRemoved_DisconnectsAndStaysRunning()
    {
        await using var server = FakeDeviceServer.Start();
        Seed(WifiSettings(server.Port));
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();

            var draft = runtime.GetSettingsSnapshot();
            draft.Connection.Host = string.Empty;
            draft.Connection.Port = 0;
            Assert.True((await runtime.UpdateSettingsAsync(draft)).IsSuccess);

            await server.WaitForDisconnectAsync();
            Assert.Equal(RuntimeState.Running, runtime.State);
            Assert.Equal(ConnectionState.Disconnected, runtime.DeviceConnectionState);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task ReconnectAsync_ClosesOldConnection_AndConnectsAgain()
    {
        await using var server = FakeDeviceServer.Start();
        Seed(WifiSettings(server.Port));
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();

            await runtime.ReconnectAsync();

            await server.WaitForConnectionAsync();
            await server.WaitForDisconnectAsync();
            Assert.Equal(2, server.ConnectionCount);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task ReconnectAsync_WithAutoReconnectOff_MakesSingleNewAttempt()
    {
        await using var server = FakeDeviceServer.Start();
        Seed(WifiSettings(server.Port, autoReconnect: false));
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        try
        {
            await server.WaitForConnectionAsync();

            await runtime.ReconnectAsync();

            await server.WaitForConnectionAsync();
            Assert.Equal(2, server.ConnectionCount);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Events_FromConnectionChange_DoNotContainHostOrPort()
    {
        await using var first = FakeDeviceServer.Start();
        await using var second = FakeDeviceServer.Start();
        Seed(WifiSettings(first.Port));
        var runtime = CreateRuntime();
        var payloads = new List<string>();
        void Record(string s) { lock (payloads) payloads.Add(s); }
        runtime.DeviceConnectionStateChanged += (_, e) => Record($"{e.ConnectionType}:{e.State}");
        runtime.SettingsChanged += (_, e) => Record(e.ToString() ?? "");
        runtime.ProfilesChanged += (_, e) => Record(e.ToString() ?? "");
        runtime.ActiveProfileChanged += (_, id) => Record(id ?? "");
        runtime.MessageRejected += (_, m) => Record(m);

        await runtime.StartAsync();
        try
        {
            await first.WaitForConnectionAsync();
            var draft = runtime.GetSettingsSnapshot();
            draft.Connection.Port = second.Port;
            await runtime.UpdateSettingsAsync(draft);
            await second.WaitForConnectionAsync();
        }
        finally
        {
            await runtime.StopAsync();
        }

        string all;
        lock (payloads) all = string.Join("\n", payloads);
        Assert.DoesNotContain("127.0.0.1", all);
        Assert.DoesNotContain(first.Port.ToString(), all);
        Assert.DoesNotContain(second.Port.ToString(), all);
    }
}
