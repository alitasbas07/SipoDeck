using System.Text;
using SipoDeck.Core.Protocol;
using SipoDeck.Core.Transport;

namespace SipoDeck.Core.Devices;

/// <summary>
/// Bir transporttan gelen ham veriyi satırlara ayırır, JSON protokol mesajlarına çözümler
/// ve cihaz olaylarına dönüştürür. Transport mesajın anlamını bilmez; yorumlama burada yapılır.
/// Bağlantı kurulduğunda cihazdan tanıtım (hello) ister ve cihazı <see cref="DeviceManager"/>'a kaydeder.
/// Geçersiz mesajlar istisna fırlatmaz; <see cref="MessageRejected"/> ile bildirilir.
/// </summary>
public sealed class DeviceConnection : IDisposable
{
    private readonly ITransport _transport;
    private readonly DeviceManager _devices;
    private readonly ConnectionType _connectionType;
    private readonly LineFramer _framer = new();
    private readonly object _gate = new();

    public DeviceConnection(ITransport transport, ConnectionType connectionType, DeviceManager devices)
    {
        _transport = transport;
        _connectionType = connectionType;
        _devices = devices;

        _transport.DataReceived += OnDataReceived;
        _transport.StateChanged += OnStateChanged;
    }

    /// <summary>Cihazdan bir olay (örn. tuş basma/bırakma) geldiğinde tetiklenir.</summary>
    public event EventHandler<IDeviceEvent>? EventReceived;

    /// <summary>Cihaz kendini tanıttığında tetiklenir.</summary>
    public event EventHandler<Device>? DeviceIdentified;

    /// <summary>Geçersiz veya desteklenmeyen bir mesaj reddedildiğinde tetiklenir.</summary>
    public event EventHandler<string>? MessageRejected;

    /// <summary>Bağlantı durumu değiştiğinde tetiklenir.</summary>
    public event EventHandler<ConnectionState>? StateChanged;

    public ITransport Transport => _transport;

    private void OnStateChanged(object? sender, ConnectionState state)
    {
        lock (_gate)
            _framer.Reset();

        StateChanged?.Invoke(this, state);

        if (state == ConnectionState.Connected)
            _ = RequestHelloAsync();
    }

    private async Task RequestHelloAsync()
    {
        try
        {
            var json = ProtocolSerializer.Serialize(new HelloMessage()) + "\n";
            await _transport.SendAsync(Encoding.UTF8.GetBytes(json)).ConfigureAwait(false);
        }
        catch
        {
            // Tanıtım isteği gönderilemese de cihaz bağlanınca kendiliğinden hello gönderebilir.
        }
    }

    private void OnDataReceived(object? sender, byte[] data)
    {
        IReadOnlyList<string> lines;
        lock (_gate)
            lines = _framer.Append(data);

        foreach (var line in lines)
            HandleLine(line);
    }

    private void HandleLine(string line)
    {
        if (!ProtocolSerializer.TryDeserialize(line, out var message, out var error))
        {
            MessageRejected?.Invoke(this, error!);
            return;
        }

        if (string.IsNullOrWhiteSpace(message!.DeviceId))
        {
            MessageRejected?.Invoke(this, "Mesajda device_id yok.");
            return;
        }

        switch (message)
        {
            case HelloMessage hello:
                HandleHello(hello);
                break;

            case ButtonEventMessage button:
                if (button.Button < 0)
                {
                    MessageRejected?.Invoke(this, $"Geçersiz tuş numarası: {button.Button}.");
                    return;
                }

                EventReceived?.Invoke(this, ToKeyEvent(button));
                break;

            default:
                MessageRejected?.Invoke(this, $"Desteklenmeyen mesaj türü: {message.GetType().Name}.");
                break;
        }
    }

    private void HandleHello(HelloMessage hello)
    {
        if (hello.ProtocolVersion != ProtocolVersion.Current)
        {
            MessageRejected?.Invoke(this, $"Cihaz desteklenmeyen protokol sürümü bildirdi: {hello.ProtocolVersion}.");
            return;
        }

        var info = hello.ToDeviceInfo();
        var device = _devices.Get(info.DeviceId);
        if (device is null)
        {
            var name = string.IsNullOrWhiteSpace(info.DeviceName) ? info.DeviceId : info.DeviceName;
            device = new Device(info.DeviceId, name, _connectionType, _transport);
            _devices.Add(device);
        }

        device.FirmwareVersion = info.FirmwareVersion;
        device.ProtocolVersion = info.ProtocolVersion;
        device.ConnectionType = _connectionType;
        device.Transport = _transport;

        DeviceIdentified?.Invoke(this, device);
    }

    /// <summary>
    /// Protokol mesajını domain olayına dönüştürür. Cihazın timestamp alanı cihazın kendi
    /// saatine göre olduğundan olay zamanı olarak alınma zamanı kullanılır.
    /// </summary>
    private static KeyEvent ToKeyEvent(ButtonEventMessage message)
        => new(message.DeviceId, message.Button, message.State, DateTimeOffset.UtcNow);

    public void Dispose()
    {
        _transport.DataReceived -= OnDataReceived;
        _transport.StateChanged -= OnStateChanged;
    }
}
