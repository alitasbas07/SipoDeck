using System.Text;

namespace SipoDeck.Core.Storage;

/// <summary>
/// Dosyayı atomik yazar: içerik önce aynı klasördeki geçici dosyaya yazılır, sonra hedefin
/// üzerine taşınır. Hata olursa geçici dosya silinir ve hedef dosya değişmez.
/// </summary>
internal static class AtomicFile
{
    public static void WriteAllText(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, Path.GetFileName(filePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }

            throw;
        }
    }
}
