namespace JJMediate;

/// <summary>
/// Represents source content with its line number for error reporting.
/// </summary>
public record SrcContent
{
    public required int LineNo { get; init; }
    public required string Content { get; init; }
}
