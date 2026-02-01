namespace JJMediate;

/// <summary>
/// File operations for conflict resolution.
/// </summary>
public static class FileOperations
{
    /// <summary>
    /// Resolves conflicts in a file and updates it atomically if changes were made.
    /// </summary>
    public static Result ResolveFile(
        string filePath,
        ResolutionOptions options,
        bool verbose = true
    )
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"File not found: {filePath}");
            return Result.Empty;
        }

        try
        {
            // Read file content
            var content = File.ReadAllText(filePath);

            // Parse conflicts
            var parsed = ConflictParser.Parse(content);

            // Check if there are any conflicts
            var hasConflicts = parsed.Any(item => item is Either<string, Conflict>.Right);
            if (!hasConflicts)
            {
                if (verbose)
                    Console.WriteLine($"{filePath}: No conflicts found");
                return Result.Empty;
            }

            // Resolve conflicts
            var resolved = ConflictResolver.ResolveContent(options, parsed);

            // Handle results
            HandleFileResult(filePath, content, resolved, verbose);

            return resolved.Result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing {filePath}: {ex.Message}");
            return new Result { FailedToResolve = 1 };
        }
    }

    private static void HandleFileResult(
        string filePath,
        string originalContent,
        NewContent resolved,
        bool verbose
    )
    {
        var result = resolved.Result;
        var allGood = result.FullySuccessful;

        if (result.ResolvedSuccessfully == 0 && allGood)
        {
            // No actual conflicts were present (shouldn't happen due to earlier check)
            if (verbose)
                Console.WriteLine($"{filePath}: No conflicts");
        }
        else if (result.ResolvedSuccessfully == 0 && result.ReducedConflicts == 0)
        {
            // Failed to resolve any conflicts
            if (verbose)
                Console.WriteLine(
                    $"{filePath}: Failed to resolve {result.FailedToResolve} conflict(s)"
                );
        }
        else if (result.ResolvedSuccessfully == 0)
        {
            // Reduced some conflicts
            if (verbose)
                Console.WriteLine($"{filePath}: Reduced {result.ReducedConflicts} conflict(s)");

            if (resolved.Content != originalContent)
                OverwriteFile(filePath, resolved.Content);
        }
        else
        {
            // Successfully resolved some conflicts
            var remaining = result.ReducedConflicts + result.FailedToResolve;
            var message =
                $"{filePath}: Successfully resolved {result.ResolvedSuccessfully} conflict(s)";

            if (remaining > 0)
                message += $" (failed to resolve {remaining} conflict(s))";

            if (verbose)
                Console.WriteLine(message);

            if (resolved.Content != originalContent)
                OverwriteFile(filePath, resolved.Content);
        }
    }

    /// <summary>
    /// Overwrites a file atomically by writing to a temp file and renaming.
    /// </summary>
    private static void OverwriteFile(string filePath, string content)
    {
        var tempPath = filePath + ".jj-mediate-tmp";
        var backupPath = filePath + ".bk";

        try
        {
            // Write to temp file
            File.WriteAllText(tempPath, content);

            // Backup original
            if (File.Exists(filePath))
                File.Move(filePath, backupPath, overwrite: true);

            // Move temp to target
            File.Move(tempPath, filePath, overwrite: true);

            // Remove backup
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
        catch
        {
            // Clean up temp file on error
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            // Restore from backup if needed
            if (!File.Exists(filePath) && File.Exists(backupPath))
                File.Move(backupPath, filePath);

            throw;
        }
    }
}
