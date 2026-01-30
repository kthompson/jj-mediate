using JJResolve;

// Parse command-line arguments
var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
var options = ResolutionOptions.Default;
var specificFile = cmdArgs.FirstOrDefault(arg => !arg.StartsWith("--") && !arg.StartsWith("-"));
var verbose = true;

// Simple argument parsing
foreach (var arg in cmdArgs)
{
    switch (arg)
    {
        case "--help" or "-h":
            ShowHelp();
            return 0;
        case "--version" or "-v":
            Console.WriteLine("jj-resolve 1.0.0");
            return 0;
        case "--no-trivial":
            options = options with { Trivial = false };
            break;
        case "--no-reduce":
            options = options with { Reduce = false };
            break;
        case "--no-indentation":
            options = options with { Indentation = false };
            break;
        case "--no-added-lines":
            options = options with { AddedLines = false };
            break;
        case "--no-line-endings":
            options = options with { LineEndings = false };
            break;
        case "--no-split":
            options = options with { SplitMarkers = false };
            break;
        case "--quiet" or "-q":
            verbose = false;
            break;
        default:
            if (arg.StartsWith("--untabify="))
            {
                if (int.TryParse(arg.Substring("--untabify=".Length), out var width))
                    options = options with { Untabify = width };
            }
            else if (arg.StartsWith("-"))
            {
                Console.Error.WriteLine($"Unknown option: {arg}");
                Console.Error.WriteLine("Use --help for usage information");
                return 1;
            }
            break;
    }
}

// Check if we're in a jj repository
if (!JujutsuIntegration.IsInJujutsuRepo())
{
    Console.Error.WriteLine("Error: Not in a Jujutsu repository");
    Console.Error.WriteLine("Please run this command from within a jj repository");
    return 1;
}

Result totalResult;

if (specificFile != null)
{
    // Resolve specific file
    if (!File.Exists(specificFile))
    {
        Console.Error.WriteLine($"Error: File not found: {specificFile}");
        return 1;
    }

    totalResult = FileOperations.ResolveFile(specificFile, options, verbose);
}
else
{
    // Find and resolve all conflicted files
    var conflictedFiles = JujutsuIntegration.GetConflictedFiles();

    if (conflictedFiles.Count == 0)
    {
        if (verbose)
            Console.WriteLine("No conflicted files found");
        return 0;
    }

    if (verbose)
        Console.WriteLine($"Found {conflictedFiles.Count} conflicted file(s)");

    totalResult = Result.Empty;
    foreach (var file in conflictedFiles)
    {
        totalResult += FileOperations.ResolveFile(file, options, verbose);
    }
}

// Summary
if (
    verbose
    && (
        totalResult.ResolvedSuccessfully > 0
        || totalResult.ReducedConflicts > 0
        || totalResult.FailedToResolve > 0
    )
)
{
    Console.WriteLine();
    Console.WriteLine("Summary:");
    Console.WriteLine($"  Resolved: {totalResult.ResolvedSuccessfully}");
    Console.WriteLine($"  Reduced: {totalResult.ReducedConflicts}");
    Console.WriteLine($"  Failed: {totalResult.FailedToResolve}");
}

// Exit code: 0 if fully successful, 1 otherwise
return totalResult.FullySuccessful ? 0 : 1;

static void ShowHelp()
{
    Console.WriteLine("jj-resolve - Intelligent conflict resolution for Jujutsu");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  jj-resolve [OPTIONS] [FILE]");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine("  -h, --help              Show this help message");
    Console.WriteLine("  -v, --version           Show version information");
    Console.WriteLine("  -q, --quiet             Quiet mode (minimal output)");
    Console.WriteLine("  --no-trivial            Don't resolve trivial conflicts");
    Console.WriteLine("  --no-reduce             Don't reduce conflicts");
    Console.WriteLine("  --no-indentation        Don't handle indentation differences");
    Console.WriteLine("  --no-added-lines        Don't detect added lines on both sides");
    Console.WriteLine("  --no-line-endings       Don't normalize line endings");
    Console.WriteLine("  --no-split              Don't split conflicts on ~~~~~~~ markers");
    Console.WriteLine("  --untabify=N            Expand tabs to N spaces");
    Console.WriteLine();
    Console.WriteLine("ARGUMENTS:");
    Console.WriteLine("  FILE                    Specific file to resolve (optional)");
    Console.WriteLine("                          If not specified, resolves all conflicted files");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  jj-resolve              Resolve all conflicts in repository");
    Console.WriteLine("  jj-resolve file.cs      Resolve conflicts in specific file");
    Console.WriteLine("  jj-resolve --untabify=4 Resolve with tab expansion");
}
