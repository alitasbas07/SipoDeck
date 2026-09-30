namespace SipoDeck.Core.Devices;

/// <summary>
/// Bağlı cihazları tutar. İç yapı birden fazla cihazı destekleyecek şekilde tasarlanmıştır;
/// cihazlar benzersiz kimlikleriyle yönetilir. Manuel cihaz eklemeye uygundur.
/// Cihaz olayları transport'un okuma thread'inden gelebileceğinden erişim kilitlidir.
/// </summary>
public sealed class DeviceManager
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Device> _devices = new();

    public IReadOnlyCollection<Device> Devices
    {
        get
        {
            lock (_gate)
                return _devices.Values.ToArray();
        }
    }

    /// <summary>Cihazı benzersiz kimliğiyle ekler veya günceller.</summary>
    public void Add(Device device)
    {
        lock (_gate)
            _devices[device.Id] = device;
    }

    public bool Remove(string deviceId)
    {
        lock (_gate)
            return _devices.Remove(deviceId);
    }

    public Device? Get(string deviceId)
    {
        lock (_gate)
            return _devices.TryGetValue(deviceId, out var device) ? device : null;
    }
}
