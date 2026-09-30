using SipoDeck.Core.Devices;
using SipoDeck.Core.Protocol;
using SipoDeck.Core.Tests.Helpers;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Tests;

public class DeviceConnectionTests
{
    private const string HelloJson =
        "{\"type\":\"hello\",\"version\":1,\"device_id\":\"d1\",\"device_name\":\"Deck\",\"firmware_version\":\"1.2\",\"protocol_version\":1}\n";

    private readonly FakeTransport _transport = new();
    private readonly DeviceManager _devices = new();
    private readonly DeviceConnection _connection;
    private readonly List<IDeviceEvent> _events = new();
    private readonly List<string> _rejected = new();
    private readonly List<ConnectionState> _states = new();
    private readonly List<Device> _identified = new();

    public DeviceConnectionTests()
    {
        _connection = new DeviceConnection(_transport, ConnectionType.WiFi, _devices);
        _connection.EventReceived += (_, e) => _events.Add(e);
        _connection.MessageRejected += (_, m) => _rejected.Add(m);
        _connection.StateChanged += (_, s) => _states.Add(s);
        _connection.DeviceIdentified += (_, d) => _identified.Add(d);
    }

    [Fact]
    public void Connected_SendsHelloRequest()
    {
        _transport.SetState(ConnectionState.Connected);

        var sent = Assert.Single(_transport.SentText);
        Assert.EndsWith("\n", sent);
        Assert.True(ProtocolSerializer.TryDeserialize(sent.Trim(), out var message, out _));
        Assert.IsType<HelloMessage>(message);
    }

    [Fact]
    public void Connected_WhenSendFails_DoesNotThrow()
    {
        _transport.ThrowOnSend = true;

        _transport.SetState(ConnectionState.Connected);

        Assert.Equal(ConnectionState.Connected, Assert.Single(_states));
    }

    [Fact]
    public void Hello_RegistersDeviceInManager()
    {
        _transport.SetState(ConnectionState.Connected);
        _transport.ReceiveText(HelloJson);

        var device = Assert.Single(_devices.Devices);
        Assert.Equal("d1", device.Id);
        Assert.Equal("Deck", device.Name);
        Assert.Equal("1.2", device.FirmwareVersion);
        Assert.Equal(ConnectionType.WiFi, device.ConnectionType);
        Assert.Same(_transport, device.Transport);
        Assert.Equal(ConnectionState.Connected, device.State);
        Assert.Same(device, Assert.Single(_identified));
    }

    [Fact]
    public void Hello_WithoutName_UsesDeviceIdAsName()
    {
        _transport.ReceiveText("{\"type\":\"hello\",\"device_id\":\"d9\",\"protocol_version\":1}\n");

        Assert.Equal("d9", _devices.Get("d9")!.Name);
    }

    [Fact]
    public void Hello_Twice_UpdatesExistingDeviceInsteadOfDuplicating()
    {
        _transport.ReceiveText(HelloJson);
        _transport.ReceiveText(HelloJson.Replace("1.2", "2.0"));

        var device = Assert.Single(_devices.Devices);
        Assert.Equal("2.0", device.FirmwareVersion);
        Assert.Equal(2, _identified.Count);
    }

    [Fact]
    public void Hello_WithUnsupportedProtocolVersion_IsRejected()
    {
        _transport.ReceiveText("{\"type\":\"hello\",\"device_id\":\"d1\",\"protocol_version\":7}\n");

        Assert.Empty(_devices.Devices);
        Assert.Single(_rejected);
    }

    [Fact]
    public void ButtonMessage_IsConvertedToKeyEvent()
    {
        _transport.ReceiveText("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":5,\"state\":\"pressed\"}\n");

        var key = Assert.IsType<KeyEvent>(Assert.Single(_events));
        Assert.Equal("d1", key.DeviceId);
        Assert.Equal(5, key.Button);
        Assert.Equal(ButtonState.Pressed, key.State);
        Assert.Empty(_rejected);
    }

    [Fact]
    public void ButtonMessage_SplitAcrossChunks_ProducesSingleEvent()
    {
        _transport.ReceiveText("{\"type\":\"button\",\"device_id\":\"d1\",");
        _transport.ReceiveText("\"button\":2,\"state\":\"released\"}\r\n");

        var key = Assert.IsType<KeyEvent>(Assert.Single(_events));
        Assert.Equal(ButtonState.Released, key.State);
    }

    [Fact]
    public void TwoMessagesInOneChunk_ProduceTwoEvents()
    {
        _transport.ReceiveText(
            "{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}\n" +
            "{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"released\"}\n");

        Assert.Equal(2, _events.Count);
    }

    [Theory]
    [InlineData("garbage\n")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"state\":\"pressed\"}\n")]
    [InlineData("{\"type\":\"nope\",\"device_id\":\"d1\"}\n")]
    [InlineData("{\"type\":\"button\",\"version\":9,\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}\n")]
    [InlineData("{\"type\":\"button\",\"button\":1,\"state\":\"pressed\"}\n")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":-1,\"state\":\"pressed\"}\n")]
    public void InvalidMessage_RaisesMessageRejected_WithoutThrowing(string data)
    {
        _transport.ReceiveText(data);

        Assert.Single(_rejected);
        Assert.Empty(_events);
    }

    [Fact]
    public void Disconnect_RaisesDisconnectedAndDeviceReportsDisconnected()
    {
        _transport.SetState(ConnectionState.Connected);
        _transport.ReceiveText(HelloJson);
        var device = _devices.Get("d1")!;

        _transport.SetState(ConnectionState.Disconnected);

        Assert.Equal(ConnectionState.Disconnected, _states[^1]);
        Assert.Equal(ConnectionState.Disconnected, device.State);
    }

    [Fact]
    public void StateChange_DropsPartialLine()
    {
        _transport.SetState(ConnectionState.Connected);
        _transport.ReceiveText("{\"type\":\"button\",\"device_id\":\"d1\",\"but");
        _transport.SetState(ConnectionState.Disconnected);
        _transport.SetState(ConnectionState.Connected);

        _transport.ReceiveText("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}\n");

        Assert.Single(_events);
        Assert.Empty(_rejected);
    }

    [Fact]
    public void Dispose_StopsListeningToTransport()
    {
        _connection.Dispose();

        _transport.SetState(ConnectionState.Connected);
        _transport.ReceiveText("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}\n");

        Assert.Empty(_events);
        Assert.Empty(_states);
        Assert.Empty(_transport.Sent);
    }
}
