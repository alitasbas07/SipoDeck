using System.Text;
using SipoDeck.Core.Protocol;

namespace SipoDeck.Core.Tests;

public class ProtocolSerializerTests
{
    [Fact]
    public void TryDeserialize_ValidButton_ReturnsButtonMessage()
    {
        var ok = ProtocolSerializer.TryDeserialize(
            "{\"type\":\"button\",\"version\":1,\"device_id\":\"d1\",\"button\":3,\"state\":\"pressed\",\"timestamp\":42}",
            out var message, out var error);

        Assert.True(ok);
        Assert.Null(error);
        var button = Assert.IsType<ButtonEventMessage>(message);
        Assert.Equal("d1", button.DeviceId);
        Assert.Equal(3, button.Button);
        Assert.Equal(ButtonState.Pressed, button.State);
        Assert.Equal(42, button.Timestamp);
    }

    [Fact]
    public void TryDeserialize_ReleasedState_IsParsed()
    {
        ProtocolSerializer.TryDeserialize(
            "{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"released\"}", out var message, out _);

        Assert.Equal(ButtonState.Released, Assert.IsType<ButtonEventMessage>(message).State);
    }

    [Fact]
    public void TryDeserialize_ValidHello_ReturnsHelloMessage()
    {
        var ok = ProtocolSerializer.TryDeserialize(
            "{\"type\":\"hello\",\"version\":1,\"device_id\":\"d1\",\"device_name\":\"Deck\",\"firmware_version\":\"1.2\",\"protocol_version\":1}",
            out var message, out _);

        Assert.True(ok);
        var hello = Assert.IsType<HelloMessage>(message);
        Assert.Equal("Deck", hello.DeviceName);
        Assert.Equal("1.2", hello.FirmwareVersion);
        var info = hello.ToDeviceInfo();
        Assert.Equal("d1", info.DeviceId);
        Assert.Equal(1, info.ProtocolVersion);
    }

    [Fact]
    public void TryDeserialize_OutOfOrderTypeProperty_IsAccepted()
    {
        var ok = ProtocolSerializer.TryDeserialize(
            "{\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\",\"type\":\"button\"}", out var message, out _);

        Assert.True(ok);
        Assert.IsType<ButtonEventMessage>(message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"type\":\"button\"")]
    [InlineData("null")]
    [InlineData("{\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}")]
    [InlineData("{\"type\":\"unknown\",\"device_id\":\"d1\"}")]
    [InlineData("{\"type\":\"button\",\"version\":2,\"device_id\":\"d1\",\"button\":1,\"state\":\"pressed\"}")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"state\":\"pressed\"}")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1}")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":1,\"state\":\"held\"}")]
    [InlineData("{\"type\":\"button\",\"device_id\":\"d1\",\"button\":\"x\",\"state\":\"pressed\"}")]
    public void TryDeserialize_InvalidInput_IsRejectedWithoutThrowing(string json)
    {
        var ok = ProtocolSerializer.TryDeserialize(json, out var message, out var error);

        Assert.False(ok);
        Assert.Null(message);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Serialize_Hello_RoundTrips()
    {
        var json = ProtocolSerializer.Serialize(new HelloMessage { DeviceId = "d1", DeviceName = "Deck" });

        Assert.Contains("\"type\":\"hello\"", json);
        Assert.True(ProtocolSerializer.TryDeserialize(json, out var message, out _));
        Assert.Equal("Deck", Assert.IsType<HelloMessage>(message).DeviceName);
    }
}

public class LineFramerTests
{
    private static ReadOnlySpan<byte> Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Append_CompleteLine_IsReturned()
    {
        var framer = new LineFramer();

        Assert.Equal(new[] { "abc" }, framer.Append(Bytes("abc\n")));
    }

    [Fact]
    public void Append_FragmentedData_IsJoinedAcrossCalls()
    {
        var framer = new LineFramer();

        Assert.Empty(framer.Append(Bytes("{\"a\":")));
        Assert.Empty(framer.Append(Bytes("1")));
        Assert.Equal(new[] { "{\"a\":1}" }, framer.Append(Bytes("}\n")));
    }

    [Fact]
    public void Append_CrLf_IsTrimmed()
    {
        var framer = new LineFramer();

        Assert.Equal(new[] { "one", "two" }, framer.Append(Bytes("one\r\ntwo\r\n")));
    }

    [Fact]
    public void Append_EmptyLines_AreSkipped()
    {
        var framer = new LineFramer();

        Assert.Equal(new[] { "x" }, framer.Append(Bytes("\n\r\n  \nx\n\n")));
    }

    [Fact]
    public void Append_MultipleLinesInOneChunk_AreAllReturned()
    {
        var framer = new LineFramer();

        Assert.Equal(new[] { "a", "b", "c" }, framer.Append(Bytes("a\nb\nc\n")));
    }

    [Fact]
    public void Append_OverlongLine_IsDiscardedAndNextLineSurvives()
    {
        var framer = new LineFramer(maxLineLength: 8);

        var lines = framer.Append(Bytes(new string('x', 50) + "\nok\n"));

        Assert.Equal(new[] { "ok" }, lines);
    }

    [Fact]
    public void Append_OverlongLineAcrossChunks_IsDiscardedUntilNewline()
    {
        var framer = new LineFramer(maxLineLength: 4);

        Assert.Empty(framer.Append(Bytes("abcdefgh")));
        Assert.Empty(framer.Append(Bytes("ijkl")));
        Assert.Equal(new[] { "ok" }, framer.Append(Bytes("\nok\n")));
    }

    [Fact]
    public void Append_LineExactlyAtLimit_IsAccepted()
    {
        var framer = new LineFramer(maxLineLength: 4);

        Assert.Equal(new[] { "abcd" }, framer.Append(Bytes("abcd\n")));
    }

    [Fact]
    public void Append_MultiByteCharacterSplitAcrossChunks_IsDecodedCorrectly()
    {
        var framer = new LineFramer();
        var bytes = Encoding.UTF8.GetBytes("çay\n");

        Assert.Empty(framer.Append(bytes.AsSpan(0, 1)));
        Assert.Equal(new[] { "çay" }, framer.Append(bytes.AsSpan(1)));
    }

    [Fact]
    public void Reset_DropsPartialLine()
    {
        var framer = new LineFramer();
        framer.Append(Bytes("partial"));

        framer.Reset();

        Assert.Equal(new[] { "fresh" }, framer.Append(Bytes("fresh\n")));
    }
}
