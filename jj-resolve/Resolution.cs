namespace JJResolve;

/// <summary>
/// Result of attempting to resolve a single conflict.
/// </summary>
public abstract record Resolution
{
    /// <summary>
    /// The conflict could not be resolved.
    /// </summary>
    public record NoResolution(Conflict Conflict) : Resolution;

    /// <summary>
    /// The conflict was fully resolved.
    /// </summary>
    public record Resolved(string Content) : Resolution;

    /// <summary>
    /// The conflict was partially resolved (reduced but not eliminated).
    /// </summary>
    public record PartialResolution(string Content) : Resolution;
}
