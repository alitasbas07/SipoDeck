using System.Text;

namespace SipoDeck.Core.Protocol;

/// <summary>
/// Transporttan parça parça gelen ham baytları '\n' ile biten satırlara (tek satır JSON mesajı)
/// ayırır. Wi-Fi ve USB Serial aynı çerçevelemeyi kullanır. Aşırı uzun satırlar atılır.
/// </summary>
public sealed class LineFramer
{
    private readonly int _maxLineLength;
    private readonly List<byte> _buffer = new();
    private bool _discarding;

    public LineFramer(int maxLineLength = 4096) => _maxLineLength = maxLineLength;

    /// <summary>Yeni gelen baytları ekler ve tamamlanan satırları döner.</summary>
    public IReadOnlyList<string> Append(ReadOnlySpan<byte> data)
    {
        var lines = new List<string>();

        foreach (var b in data)
        {
            if (b == (byte)'\n')
            {
                if (!_discarding)
                {
                    var line = Encoding.UTF8.GetString(_buffer.ToArray()).Trim();
                    if (line.Length > 0)
                        lines.Add(line);
                }

                _buffer.Clear();
                _discarding = false;
                continue;
            }

            if (_discarding)
                continue;

            if (_buffer.Count >= _maxLineLength)
            {
                // Satır sınırı aşıldı; satır sonuna kadar gelen veri yok sayılır.
                _buffer.Clear();
                _discarding = true;
                continue;
            }

            _buffer.Add(b);
        }

        return lines;
    }

    /// <summary>Yarım kalmış satırı temizler (örn. bağlantı koptuğunda).</summary>
    public void Reset()
    {
        _buffer.Clear();
        _discarding = false;
    }
}
