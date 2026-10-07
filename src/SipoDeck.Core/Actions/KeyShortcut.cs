namespace SipoDeck.Core.Actions;

/// <summary>
/// "Ctrl+Shift+M" gibi bir klavye kısayolu. Değiştiriciler: Ctrl, Alt, Shift, Win.
/// Tuş: tek harf/rakam, F1-F24 veya <see cref="NamedKeys"/> içindeki bir ad.
/// </summary>
public sealed record KeyShortcut(bool Ctrl, bool Alt, bool Shift, bool Win, string Key)
{
    public static readonly IReadOnlyList<string> NamedKeys = new[]
    {
        "Enter", "Esc", "Tab", "Space", "Backspace", "Delete", "Insert", "Home", "End",
        "PageUp", "PageDown", "Up", "Down", "Left", "Right"
    };

    public static bool TryParse(string? text, out KeyShortcut shortcut, out string? error)
    {
        shortcut = new KeyShortcut(false, false, false, false, string.Empty);
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Kısayol boş olamaz.";
            return false;
        }

        bool ctrl = false, alt = false, shift = false, win = false;
        string? key = null;

        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries))
        {
            if (raw.Length == 0)
            {
                error = "Kısayol biçimi geçersiz (örn. Ctrl+Shift+M).";
                return false;
            }

            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": ctrl = true; continue;
                case "alt": alt = true; continue;
                case "shift": shift = true; continue;
                case "win" or "windows": win = true; continue;
            }

            if (key is not null)
            {
                error = "Kısayol yalnızca bir ana tuş içermelidir.";
                return false;
            }

            var normalized = Normalize(raw);
            if (normalized is null)
            {
                error = $"Bilinmeyen tuş: {raw}";
                return false;
            }
            key = normalized;
        }

        if (key is null)
        {
            error = "Kısayol bir ana tuş içermelidir.";
            return false;
        }

        shortcut = new KeyShortcut(ctrl, alt, shift, win, key);
        return true;
    }

    private static string? Normalize(string token)
    {
        if (token.Length == 1 && char.IsAsciiLetterOrDigit(token[0]))
            return token.ToUpperInvariant();

        if (token.Length is 2 or 3 && (token[0] is 'f' or 'F')
            && int.TryParse(token.AsSpan(1), out var n) && n is >= 1 and <= 24)
            return "F" + n;

        return NamedKeys.FirstOrDefault(k => k.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
        => string.Join("+", new[] { Ctrl ? "Ctrl" : null, Alt ? "Alt" : null, Shift ? "Shift" : null, Win ? "Win" : null, Key }.Where(s => s is not null));
}
