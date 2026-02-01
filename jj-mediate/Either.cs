namespace JJMediate;

/// <summary>
/// Discriminated union representing either a plain line or a conflict.
/// </summary>
public abstract record Either<TLeft, TRight>
{
    private Either() { }

    public sealed record Left(TLeft Value) : Either<TLeft, TRight>;

    public sealed record Right(TRight Value) : Either<TLeft, TRight>;

    public TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
    {
        return this switch
        {
            Left l => onLeft(l.Value),
            Right r => onRight(r.Value),
            _ => throw new InvalidOperationException(),
        };
    }
}
