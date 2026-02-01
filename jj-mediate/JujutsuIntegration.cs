using System.Diagnostics;

namespace JJMediate;

/// <summary>
/// Jujutsu VCS integration.
/// </summary>
public static class JujutsuIntegration
{
    /// <summary>
    /// Finds all files with conflicts in the current jj repository.
    /// </summary>
    public static List<string> GetConflictedFiles()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "jj",
                Arguments = "status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start jj process");

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"jj status failed: {error}");
            }

            // Parse output to find files marked as conflicted
            // jj status output format: "filename (conflicted)"
            return output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains("(conflicted)") || line.Contains("conflict"))
                .Select(line => line.Split(' ')[0].Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting conflicted files: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if we're in a jj repository.
    /// </summary>
    public static bool IsInJujutsuRepo()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "jj",
                Arguments = "status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
