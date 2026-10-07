namespace SipoDeck.Plugin;

/// <summary>
/// Plugin'e verilen sınırlı bağlam. Plugin; kullanıcı ayarlarına, profillere, bağlantı bilgilerine,
/// transport'a veya Input Engine'e buradan erişemez.
/// </summary>
public interface IPluginContext
{
    /// <summary>Manifestteki plugin kimliği.</summary>
    string PluginId { get; }

    /// <summary>Plugin'e özel, kalıcı veri klasörü (yalnızca bu plugin'e aittir; ayarlar da burada saklanır).</summary>
    string DataDirectory { get; }

    /// <summary>Kullanıcı onaylı yetkiler.</summary>
    IReadOnlySet<string> GrantedPermissions { get; }

    /// <summary>Günlük kaydı; kayıtlar plugin kimliğiyle ayrıştırılır.</summary>
    IPluginLogger Logger { get; }

    /// <summary>Plugin'in kendi ayarları (veri klasöründe saklanır).</summary>
    IPluginSettings Settings { get; }

    /// <summary>Yetkiye bağlı servisi döner; yetki verilmemişse veya servis yoksa null.</summary>
    T? GetService<T>() where T : class;
}

public interface IPluginLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}

/// <summary>Anahtar/değer ayarları. Değerler metindir; kalıcı hale getirmek için <see cref="SaveAsync"/> çağrılır.</summary>
public interface IPluginSettings
{
    string? Get(string key);

    void Set(string key, string? value);

    Task SaveAsync(CancellationToken cancellationToken = default);
}

/// <summary>Yetki: <see cref="PluginPermissions.Notifications"/>.</summary>
public interface INotificationService
{
    Task ShowAsync(string title, string message, CancellationToken cancellationToken = default);
}

/// <summary>Yetki: <see cref="PluginPermissions.InputSend"/>. Odaktaki uygulamaya girdi gönderir.</summary>
public interface IInputService
{
    /// <summary>"Ctrl+Shift+M" biçiminde kısayol gönderir.</summary>
    Task SendShortcutAsync(string shortcut, CancellationToken cancellationToken = default);

    Task TypeTextAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>Yetki: <see cref="PluginPermissions.ProcessRun"/>.</summary>
public interface IProcessService
{
    Task StartAsync(string filePath, string? arguments = null, CancellationToken cancellationToken = default);
}

/// <summary>Yetki: <see cref="PluginPermissions.DeviceEvents"/>. Ham tuş olaylarını (eylem çalışmasından bağımsız) bildirir.</summary>
public interface IDeviceEventService
{
    event EventHandler<DeviceKeyEventArgs>? KeyChanged;
}

public sealed class DeviceKeyEventArgs : EventArgs
{
    public DeviceKeyEventArgs(int key, bool isPressed)
    {
        Key = key;
        IsPressed = isPressed;
    }

    public int Key { get; }

    public bool IsPressed { get; }
}
