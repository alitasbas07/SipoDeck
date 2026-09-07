using System.Diagnostics;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Belirtilen programı veya dosyayı çalıştıran eylemdir.
/// </summary>
public sealed class RunProgramAction : IAction
{
    public RunProgramAction(string filePath, string? arguments = null, string? workingDirectory = null)
    {
        FilePath = filePath;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
    }

    public string FilePath { get; }

    public string? Arguments { get; }

    public string? WorkingDirectory { get; }

    public void Execute()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FilePath,
            UseShellExecute = true
        };

        if (!string.IsNullOrEmpty(Arguments))
            startInfo.Arguments = Arguments;

        if (!string.IsNullOrEmpty(WorkingDirectory))
            startInfo.WorkingDirectory = WorkingDirectory;

        Process.Start(startInfo);
    }
}
