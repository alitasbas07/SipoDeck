namespace SipoDeck.Core.Devices;

/// <summary>
/// Bir cihaz tarafından üretilen olayı temsil eder.
/// </summary>
public interface IDeviceEvent
{
    string DeviceId { get; }

    DateTimeOffset Timestamp { get; }
}
