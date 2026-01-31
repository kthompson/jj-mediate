namespace JJResolve;

/// <summary>
/// Represents the result of processing file content with resolved/reduced conflicts.
/// </summary>
public record NewContent
{
    public required Result Result { get; init; }
    public required string Content { get; init; }

    public static NewContent operator +(NewContent a, NewContent b)
    {
        return new NewContent { Result = a.Result + b.Result, Content = a.Content + b.Content };
    }

    public static NewContent Empty => new() { Result = Result.Empty, Content = string.Empty };
}
