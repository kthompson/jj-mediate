namespace JJMediate;

/// <summary>
/// Statistics tracking conflict resolution results.
/// </summary>
public record Result
{
    public int ResolvedSuccessfully { get; init; }
    public int ReducedConflicts { get; init; }
    public int FailedToResolve { get; init; }

    public static Result Empty =>
        new()
        {
            ResolvedSuccessfully = 0,
            ReducedConflicts = 0,
            FailedToResolve = 0,
        };

    public bool FullySuccessful => ReducedConflicts == 0 && FailedToResolve == 0;

    public static Result operator +(Result a, Result b)
    {
        return new Result
        {
            ResolvedSuccessfully = a.ResolvedSuccessfully + b.ResolvedSuccessfully,
            ReducedConflicts = a.ReducedConflicts + b.ReducedConflicts,
            FailedToResolve = a.FailedToResolve + b.FailedToResolve,
        };
    }
}
