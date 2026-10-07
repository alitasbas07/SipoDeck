using System.ComponentModel;
using System.Runtime.InteropServices;
using SipoDeck.Core.Actions;
using static SipoDeck.Platform.Windows.NativeMethods;

namespace SipoDeck.Platform.Windows;

/// <summary>
/// <see cref="IPlatformActions"/> için Windows uygulaması: tuş olayları SendInput ile,
/// bildirim NotifyIcon balloon ile gönderilir.
/// </summary>
public sealed class WindowsPlatformActions : IPlatformActions
{
    private const ushort VK_BACK = 0x08, VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_CONTROL = 0x11,
        VK_MENU = 0x12, VK_ESCAPE = 0x1B, VK_SPACE = 0x20, VK_PRIOR = 0x21, VK_NEXT = 0x22, VK_END = 0x23,
        VK_HOME = 0x24, VK_LEFT = 0x25, VK_UP = 0x26, VK_RIGHT = 0x27, VK_DOWN = 0x28, VK_INSERT = 0x2D,
        VK_DELETE = 0x2E, VK_LWIN = 0x5B, VK_F1 = 0x70,
        VK_VOLUME_MUTE = 0xAD, VK_VOLUME_DOWN = 0xAE, VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0, VK_MEDIA_PREV_TRACK = 0xB1, VK_MEDIA_STOP = 0xB2, VK_MEDIA_PLAY_PAUSE = 0xB3;

    private static readonly Dictionary<string, ushort> NamedVirtualKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = VK_RETURN, ["Esc"] = VK_ESCAPE, ["Tab"] = VK_TAB, ["Space"] = VK_SPACE,
        ["Backspace"] = VK_BACK, ["Delete"] = VK_DELETE, ["Insert"] = VK_INSERT, ["Home"] = VK_HOME,
        ["End"] = VK_END, ["PageUp"] = VK_PRIOR, ["PageDown"] = VK_NEXT, ["Up"] = VK_UP,
        ["Down"] = VK_DOWN, ["Left"] = VK_LEFT, ["Right"] = VK_RIGHT
    };

    // Bu tuşlar genişletilmiş tuş olarak gönderilmezse numpad tuşu gibi davranır.
    private static readonly HashSet<ushort> ExtendedKeys = new()
    {
        VK_PRIOR, VK_NEXT, VK_END, VK_HOME, VK_LEFT, VK_UP, VK_RIGHT, VK_DOWN, VK_INSERT, VK_DELETE
    };

    public Task SetVolumeAsync(VolumeCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vk = command switch
        {
            VolumeCommand.Up => VK_VOLUME_UP,
            VolumeCommand.Down => VK_VOLUME_DOWN,
            VolumeCommand.Mute => VK_VOLUME_MUTE,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
        Send(KeyInput(vk, false), KeyInput(vk, true));
        return Task.CompletedTask;
    }

    public Task SendMediaAsync(MediaCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vk = command switch
        {
            MediaCommand.PlayPause => VK_MEDIA_PLAY_PAUSE,
            MediaCommand.Next => VK_MEDIA_NEXT_TRACK,
            MediaCommand.Previous => VK_MEDIA_PREV_TRACK,
            MediaCommand.Stop => VK_MEDIA_STOP,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
        Send(KeyInput(vk, false), KeyInput(vk, true));
        return Task.CompletedTask;
    }

    public Task SendShortcutAsync(KeyShortcut shortcut, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = ResolveKey(shortcut.Key)
            ?? throw new ArgumentException($"Bilinmeyen tuş: {shortcut.Key}", nameof(shortcut));

        var modifiers = new List<ushort>(4);
        if (shortcut.Ctrl) modifiers.Add(VK_CONTROL);
        if (shortcut.Alt) modifiers.Add(VK_MENU);
        if (shortcut.Shift) modifiers.Add(VK_SHIFT);
        if (shortcut.Win) modifiers.Add(VK_LWIN);

        // Değiştiriciler bas, tuşu bas/bırak, değiştiricileri ters sırada bırak.
        var inputs = new List<INPUT>(modifiers.Count * 2 + 2);
        foreach (var m in modifiers) inputs.Add(KeyInput(m, false));
        inputs.Add(KeyInput(key, false));
        inputs.Add(KeyInput(key, true));
        for (var i = modifiers.Count - 1; i >= 0; i--) inputs.Add(KeyInput(modifiers[i], true));

        Send(inputs.ToArray());
        return Task.CompletedTask;
    }

    public Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return Task.CompletedTask;

        // Her UTF-16 kod birimi ayrı gönderilir; surrogate çiftleri sırayla gittiği için doğru birleşir.
        foreach (var ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Send(UnicodeInput(ch, false), UnicodeInput(ch, true));
        }
        return Task.CompletedTask;
    }

    public async Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // NotifyIcon mesaj döngüsü gerektirir; kendi STA iş parçacığında kısa ömürlü bir döngü çalıştırılır.
        var thread = new Thread(() =>
        {
            try
            {
                using var icon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true
                };
                using var timer = new System.Windows.Forms.Timer { Interval = 6000 };
                timer.Tick += (_, _) => System.Windows.Forms.Application.ExitThread();
                icon.BalloonTipClosed += (_, _) => System.Windows.Forms.Application.ExitThread();
                icon.BalloonTipClicked += (_, _) => System.Windows.Forms.Application.ExitThread();

                icon.ShowBalloonTip(5000, title, string.IsNullOrEmpty(message) ? " " : message, System.Windows.Forms.ToolTipIcon.Info);
                timer.Start();
                System.Windows.Forms.Application.Run();
                icon.Visible = false; // Tepsi ikonunun artığı kalmasın.
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "SipoDeck.Notification"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        // Eylemi bildirim süresi boyunca bekletmemek için yalnızca başlatma hatası kısa süre beklenir.
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(300, cancellationToken));
        if (finished == tcs.Task) await tcs.Task;
    }

    private static ushort? ResolveKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (NamedVirtualKeys.TryGetValue(key, out var named)) return named;

        // Harf ve rakamların sanal tuş kodu ASCII büyük harf/rakam kodudur.
        if (key.Length == 1 && char.IsAsciiLetterOrDigit(key[0])) return char.ToUpperInvariant(key[0]);

        if ((key[0] is 'F' or 'f') && int.TryParse(key.AsSpan(1), out var n) && n is >= 1 and <= 24)
            return (ushort)(VK_F1 + n - 1);

        return null;
    }

    private static INPUT KeyInput(ushort vk, bool keyUp)
    {
        var flags = keyUp ? KEYEVENTF_KEYUP : 0u;
        if (ExtendedKeys.Contains(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } } };
    }

    private static INPUT UnicodeInput(char ch, bool keyUp)
    {
        var flags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0u);
        return new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = flags } } };
    }

    private static void Send(params INPUT[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput tuş olaylarını gönderemedi (yetki/UIPI olabilir).");
    }
}
