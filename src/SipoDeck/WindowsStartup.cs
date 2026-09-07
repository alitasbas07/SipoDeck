using Microsoft.Win32;

namespace SipoDeck;

/// <summary>
/// Uygulamanın Windows açılışında otomatik çalıştırılmasını HKCU Run anahtarı
/// üzerinden açıp kapatır. Ayar, 007 ayar sistemi tarafından kontrol edilir.
/// </summary>
public static class WindowsStartup
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SipoDeck";

    public static void Apply(bool enabled)
    {
        if (enabled)
            Enable();
        else
            Disable();
    }

    public static void Enable()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
            return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.SetValue(ValueName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is not null;
    }
}
