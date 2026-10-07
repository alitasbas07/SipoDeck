namespace SipoDeck.Plugin;

/// <summary>
/// Plugin sözleşmesinin sürümü. Kırıcı değişiklik yalnızca bu sürüm artırılarak yapılır.
/// Uygulama, manifestteki <c>apiVersion</c> değeri <see cref="Current"/> ile eşit olmayan plugin'i yüklemez.
/// </summary>
public static class PluginApi
{
    public const int Current = 1;
}

/// <summary>Bilinen yetkiler. Manifestte beyan edilmeyen yetkiye bağlı servis plugin'e verilmez.</summary>
public static class PluginPermissions
{
    /// <summary>Program çalıştırma (<see cref="IProcessService"/>).</summary>
    public const string ProcessRun = "process.run";

    /// <summary>Ağ erişimi. Süreç içi modelde teknik olarak zorlanamaz; yalnızca beyan ve onay düzeyindedir.</summary>
    public const string Network = "network";

    /// <summary>Plugin veri klasörü dışındaki dosyaları okuma. Teknik olarak zorlanamaz; beyan düzeyindedir.</summary>
    public const string FilesystemRead = "filesystem.read";

    /// <summary>Plugin veri klasörü dışına dosya yazma. Teknik olarak zorlanamaz; beyan düzeyindedir.</summary>
    public const string FilesystemWrite = "filesystem.write";

    /// <summary>Bildirim gösterme (<see cref="INotificationService"/>).</summary>
    public const string Notifications = "notifications";

    /// <summary>Klavye kısayolu / metin gönderme (<see cref="IInputService"/>).</summary>
    public const string InputSend = "input.send";

    /// <summary>Cihaz tuş olaylarını dinleme (<see cref="IDeviceEventService"/>).</summary>
    public const string DeviceEvents = "device.events";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ProcessRun, Network, FilesystemRead, FilesystemWrite, Notifications, InputSend, DeviceEvents
    };

    public static bool IsKnown(string permission) => All.Contains(permission);
}
