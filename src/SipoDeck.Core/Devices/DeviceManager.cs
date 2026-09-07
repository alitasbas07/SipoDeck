namespace SipoDeck.Core.Devices;

/// <summary>
/// Bağlı cihazları tutar. İç yapı birden fazla cihazı destekleyecek şekilde tasarlanmıştır;
/// cihazlar benzersiz kimlikleriyle yönetilir. Manuel cihaz eklemeye uygundur.
/// </summary>
public sealed class DeviceManager
{
    private readonly Dictionary<string, Device> _devices = new();

    public IReadOnlyCollection<Device> Devices => _devices.Values;

    /// <summary>Cihazı benzersiz kimliğiyle ekler veya günceller.</summary>
    public void Add(Device device) => _devices[device.Id] = device;

    public bool Remove(string deviceId) => _devices.Remove(deviceId);

    public Device? Get(string deviceId)
        => _devices.TryGetValue(deviceId, out var device) ? device : null;
}
