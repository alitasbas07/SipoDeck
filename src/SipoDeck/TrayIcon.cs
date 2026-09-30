using System.Drawing;
using System.Windows.Forms;

namespace SipoDeck;

/// <summary>
/// Sistem tepsisi simgesini ve menüsünü yönetir (WinForms NotifyIcon).
/// Menü: "Göster" ana pencereyi gösterir, "Tamamen Kapat" uygulamayı sonlandırır.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIcon(Action onShow, Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Göster", null, (_, _) => onShow());
        menu.Items.Add("Tamamen Kapat", null, (_, _) => onExit());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "SipoDeck",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => onShow();
    }

    /// <summary>İpucu metnini günceller (NotifyIcon.Text en fazla 63 karakter; UI thread'inde çağrılmalı).</summary>
    public void UpdateText(string text)
    {
        const int MaxLength = 63;
        _notifyIcon.Text = text.Length <= MaxLength ? text : text[..MaxLength];
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
