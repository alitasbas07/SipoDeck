using System.Text.Json;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime.Events;
using SipoDeck.Runtime.Tests.Helpers;

namespace SipoDeck.Runtime.Tests;

public sealed class AppRuntimeTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private AppRuntime CreateRuntime() => new(_dir.SettingsFile, _dir.ProfilesFile);

    private static List<RuntimeState> TrackStates(AppRuntime runtime)
    {
        var states = new List<RuntimeState>();
        runtime.StateChanged += (_, s) => { lock (states) states.Add(s); };
        return states;
    }

    [Fact]
    public void NewRuntime_StartsStopped()
    {
        var runtime = CreateRuntime();

        Assert.Equal(RuntimeState.Stopped, runtime.State);
        Assert.Equal(ConnectionState.Disconnected, runtime.DeviceConnectionState);
    }

    [Fact]
    public async Task Start_WithoutConnectionSettings_RunsAndDeviceStaysDisconnected()
    {
        var runtime = CreateRuntime();
        var states = TrackStates(runtime);
        var deviceEvents = new List<DeviceConnectionStateChangedEventArgs>();
        runtime.DeviceConnectionStateChanged += (_, e) => { lock (deviceEvents) deviceEvents.Add(e); };

        await runtime.StartAsync();
        try
        {
            Assert.Equal(RuntimeState.Running, runtime.State);
            Assert.Equal(ConnectionState.Disconnected, runtime.DeviceConnectionState);
            Assert.Equal(new[] { RuntimeState.Starting, RuntimeState.Running }, states);
            Assert.Empty(deviceEvents);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Stop_AfterStart_ReturnsToStopped()
    {
        var runtime = CreateRuntime();
        var states = TrackStates(runtime);

        await runtime.StartAsync();
        await runtime.StopAsync();

        Assert.Equal(RuntimeState.Stopped, runtime.State);
        Assert.Equal(
            new[] { RuntimeState.Starting, RuntimeState.Running, RuntimeState.Stopping, RuntimeState.Stopped },
            states);
    }

    [Fact]
    public async Task Stop_WhenNeverStarted_IsNoOp()
    {
        var runtime = CreateRuntime();
        var states = TrackStates(runtime);

        await runtime.StopAsync();

        Assert.Equal(RuntimeState.Stopped, runtime.State);
        Assert.Empty(states);
    }

    [Fact]
    public async Task RepeatedStart_DoesNotRestartOrRaiseExtraStateChanges()
    {
        var runtime = CreateRuntime();
        var states = TrackStates(runtime);

        await runtime.StartAsync();
        await runtime.StartAsync();
        try
        {
            Assert.Equal(RuntimeState.Running, runtime.State);
            Assert.Equal(new[] { RuntimeState.Starting, RuntimeState.Running }, states);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task RepeatedStop_IsHarmless()
    {
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        var states = TrackStates(runtime);

        await runtime.StopAsync();
        await runtime.StopAsync();

        Assert.Equal(RuntimeState.Stopped, runtime.State);
        Assert.Equal(new[] { RuntimeState.Stopping, RuntimeState.Stopped }, states);
    }

    [Fact]
    public async Task StartStopStart_Works_AndQueueIsUsableAgain()
    {
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        await runtime.StopAsync();

        await runtime.StartAsync();
        try
        {
            Assert.Equal(RuntimeState.Running, runtime.State);

            var log = new List<string>();
            var action = new RecordingAction(log, "a");
            runtime.Dispatch(action);
            await action.Completed.WithTimeout();
            Assert.Equal(new[] { "a" }, log);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task ConcurrentStartCalls_ResultInSingleStart()
    {
        var runtime = CreateRuntime();
        var states = TrackStates(runtime);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(() => runtime.StartAsync()))).WithTimeout();
        try
        {
            Assert.Equal(RuntimeState.Running, runtime.State);
            Assert.Equal(new[] { RuntimeState.Starting, RuntimeState.Running }, states);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public void Dispatch_WhenNotRunning_IsIgnored()
    {
        var runtime = CreateRuntime();
        var log = new List<string>();

        runtime.Dispatch(new RecordingAction(log, "a"));

        Assert.Empty(log);
    }

    [Fact]
    public async Task Start_WithCorruptSettingsAndProfiles_FallsBackToDefaultsAndRuns()
    {
        File.WriteAllText(_dir.SettingsFile, "{ bozuk json");
        File.WriteAllText(_dir.ProfilesFile, "not json");
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        try
        {
            Assert.Equal(RuntimeState.Running, runtime.State);
        }
        finally
        {
            await runtime.StopAsync();
        }
    }

    [Fact]
    public async Task Events_DoNotExposeHostPortOrComPort()
    {
        // Ayarlarda host/port/COM var; yalnızca yerel loopback'te reddedilen porta (bağlantı kurulamaz) bağlanılır.
        const string host = "127.0.0.1";
        const int port = 1;
        const string com = "COM97";
        File.WriteAllText(_dir.SettingsFile, JsonSerializer.Serialize(new
        {
            connection = new { connectionType = "WiFi", host, port, serialPortName = com, baudRate = 115200 },
            application = new { autoReconnect = false }
        }));

        var runtime = CreateRuntime();
        var payloads = new List<string>();
        void Record(string s) { lock (payloads) payloads.Add(s); }
        runtime.StateChanged += (_, s) => Record($"StateChanged:{s}");
        runtime.DeviceConnectionStateChanged += (_, e) => Record($"Conn:{e.ConnectionType}:{e.State}");
        runtime.MessageRejected += (_, m) => Record(m);
        runtime.DeviceIdentified += (_, d) => Record($"{d.Id}|{d.Name}|{d.FirmwareVersion}|{d.ProtocolVersion}|{d.ConnectionType}");
        runtime.Faulted += (_, e) => Record(e.Exception.Message);

        await runtime.StartAsync();
        await runtime.StopAsync();

        string all;
        lock (payloads) all = string.Join("\n", payloads);
        Assert.Contains("StateChanged:Running", all);
        Assert.DoesNotContain(host, all);
        Assert.DoesNotContain(com, all);
        Assert.DoesNotContain($":{port}", all);
    }

    [Theory]
    [InlineData(typeof(DeviceConnectionStateChangedEventArgs))]
    [InlineData(typeof(DeviceSnapshot))]
    [InlineData(typeof(ActionFailedEventArgs))]
    [InlineData(typeof(RuntimeFaultedEventArgs))]
    public void EventPayloadTypes_HaveNoAddressMembers(Type type)
    {
        var forbidden = new[] { "host", "port", "ip", "address", "endpoint", "com", "serial" };

        foreach (var prop in type.GetProperties())
        {
            var words = System.Text.RegularExpressions.Regex.Split(prop.Name, "(?<=[a-z])(?=[A-Z])")
                .Select(w => w.ToLowerInvariant());
            Assert.DoesNotContain(words, w => forbidden.Contains(w));
            Assert.NotEqual(typeof(System.Net.EndPoint), prop.PropertyType);
            Assert.NotEqual(typeof(ITransport), prop.PropertyType);
        }
    }
}
