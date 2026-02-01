namespace JJMediate;

/// <summary>
/// Options for conflict resolution behavior.
/// </summary>
public record ResolutionOptions
{
    /// <summary>
    /// Only resolve trivial conflicts where exactly one side changed.
    /// </summary>
    public bool Trivial { get; init; } = true;

    /// <summary>
    /// Reduce conflicts by removing common prefix/suffix.
    /// </summary>
    public bool Reduce { get; init; } = true;

    /// <summary>
    /// Handle indentation-only differences.
    /// </summary>
    public bool Indentation { get; init; } = true;

    /// <summary>
    /// Detect and resolve when both sides added lines.
    /// </summary>
    public bool AddedLines { get; init; } = true;

    /// <summary>
    /// Normalize line endings (CRLF vs LF).
    /// </summary>
    public bool LineEndings { get; init; } = true;

    /// <summary>
    /// Expand tabs to spaces (null = disabled, otherwise tab width).
    /// </summary>
    public int? Untabify { get; init; } = null;

    /// <summary>
    /// Split conflicts on ~~~~~~~ separators.
    /// </summary>
    public bool SplitMarkers { get; init; } = true;

    public static ResolutionOptions Default => new();
}
