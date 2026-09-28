using System;
using SipoDeck.Core.Devices;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;

// Task 010 — Cihaz İletişimi test aracı
// Gerçek ESP32 ile Wi-Fi (WebSocket) veya USB Serial bağlantısını dener; gelen tanıtım
// bilgisini, tuş olaylarını, reddedilen mesajları ve bağlantı durumunu konsola yazar.
//
// Kullanım:
//   dotnet run -- wifi <cihaz-ip> <port>
//   dotnet run -- serial <COMx> [baud]
//   dotnet run                      (bağlantı ayarları settings.json'dan okunur)

var transport = CreateTransport(args);
if (transport is null)
{
    Console.WriteLine("Bağlantı bilgisi yok. Kullanım: wifi <ip> <port> | serial <COMx> [baud]");
    return 1;
}

var connectionType = transport is SerialTransport ? ConnectionType.Serial : ConnectionType.WiFi;
var devices = new DeviceManager();
using var connection = new DeviceConnection(transport, connectionType, devices);

connection.StateChanged += (_, state) => Log($"Bağlantı durumu: {state}");
connection.DeviceIdentified += (_, device) =>
    Log($"Cihaz tanındı: id={device.Id} ad={device.Name} firmware={device.FirmwareVersion} protokol={device.ProtocolVersion}");
connection.EventReceived += (_, deviceEvent) =>
{
    if (deviceEvent is KeyEvent key)
        Log($"Tuş olayı: cihaz={key.DeviceId} tuş={key.Button} durum={key.State}");
};
connection.MessageRejected += (_, reason) => Log($"Mesaj reddedildi: {reason}");

Console.WriteLine($"=== SipoDeck Cihaz Monitörü — {transport.Name} ===");
Console.WriteLine("Çıkmak için Enter'a basın.\n");

using var reconnect = new ReconnectService(transport, TimeSpan.FromSeconds(3));
reconnect.Start();

Console.ReadLine();

reconnect.Stop();
await transport.DisconnectAsync();
return 0;

static void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

static ITransport? CreateTransport(string[] args)
{
    if (args.Length >= 3 && args[0].Equals("wifi", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[2], out var port))
        return new WiFiTransport(new WiFiTransportSettings { Host = args[1], Port = port });

    if (args.Length >= 2 && args[0].Equals("serial", StringComparison.OrdinalIgnoreCase))
    {
        var baud = args.Length >= 3 && int.TryParse(args[2], out var b) ? b : SettingsDefaults.BaudRate;
        return new SerialTransport(new SerialTransportSettings { PortName = args[1], BaudRate = baud });
    }

    if (args.Length > 0)
        return null;

    var connection = new SettingsStore().Load().Connection;
    return connection.ConnectionType switch
    {
        ConnectionType.WiFi when !string.IsNullOrWhiteSpace(connection.Host) && connection.Port > 0
            => new WiFiTransport(new WiFiTransportSettings { Host = connection.Host, Port = connection.Port }),
        ConnectionType.Serial when !string.IsNullOrWhiteSpace(connection.SerialPortName)
            => new SerialTransport(new SerialTransportSettings { PortName = connection.SerialPortName, BaudRate = connection.BaudRate }),
        _ => null
    };
}
