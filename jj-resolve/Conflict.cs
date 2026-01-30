namespace JJResolve;

/// <summary>
/// Represents a merge conflict with diff3-style markers.
/// </summary>
public class Conflict
{
    /// <summary>
    /// The marker lines at the beginning of each section (<<<<<<< ||||||| =======)
    /// </summary>
    public required Sides<SrcContent> Markers { get; init; }

    /// <summary>
    /// The marker line at the end of the conflict (>>>>>>>)
    /// </summary>
    public required SrcContent MarkerEnd { get; init; }

    /// <summary>
    /// The actual content lines for each side (A, Base, B)
    /// </summary>
    public required Sides<List<string>> Bodies { get; init; }

    /// <summary>
    /// Creates a modified conflict with transformed bodies.
    /// </summary>
    public Conflict WithBodies(Func<Sides<List<string>>, Sides<List<string>>> transform)
    {
        return new Conflict
        {
            Markers = Markers,
            MarkerEnd = MarkerEnd,
            Bodies = transform(Bodies),
        };
    }

    /// <summary>
    /// Creates a modified conflict with a transformation applied to each body.
    /// </summary>
    public Conflict WithEachBody(Func<List<string>, List<string>> transform)
    {
        return WithBodies(sides => sides.Select(transform));
    }

    /// <summary>
    /// Creates a modified conflict with a transformation applied to each string line.
    /// </summary>
    public Conflict WithStrings(Func<string, string> transform)
    {
        return WithEachBody(lines => lines.Select(transform).ToList());
    }

    /// <summary>
    /// Converts the conflict back to its text representation with markers.
    /// </summary>
    public IEnumerable<string> PrettyLines()
    {
        yield return Markers.SideA.Content;
        foreach (var line in Bodies.SideA)
            yield return line;

        yield return Markers.Base.Content;
        foreach (var line in Bodies.Base)
            yield return line;

        yield return Markers.SideB.Content;
        foreach (var line in Bodies.SideB)
            yield return line;

        yield return MarkerEnd.Content;
    }

    /// <summary>
    /// Converts the conflict back to its full text representation.
    /// </summary>
    public string Pretty()
    {
        return string.Join(Environment.NewLine, PrettyLines()) + Environment.NewLine;
    }
}
