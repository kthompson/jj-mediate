namespace JJResolve;

/// <summary>
/// Container for the three sides of a merge conflict: SideA (ours), Base (original), and SideB (theirs).
/// </summary>
public record Sides<T>
{
    public required T SideA { get; init; }
    public required T Base { get; init; }
    public required T SideB { get; init; }

    public Sides<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        return new Sides<TResult>
        {
            SideA = selector(SideA),
            Base = selector(Base),
            SideB = selector(SideB),
        };
    }

    public void ForEach(Action<T> action)
    {
        action(SideA);
        action(Base);
        action(SideB);
    }

    public IEnumerable<T> AsEnumerable()
    {
        yield return SideA;
        yield return Base;
        yield return SideB;
    }
}
