using SipoDeck.Core.Devices;

namespace SipoDeck.Core.Input;

/// <summary>
/// Cihaz olaylarını işleyip ilgili eylemleri tetikleyen motoru temsil eder.
/// </summary>
public interface IInputEngine
{
    void Process(IDeviceEvent deviceEvent);
}
