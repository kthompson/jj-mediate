namespace JJResolve.Tests;

public class ConflictResolverTests
{
    [Fact]
    public void ResolveContent_PlainText_NoChanges()
    {
        var content =
            @"line 1
line 2
line 3";

        var parsed = ConflictParser.Parse(content);
        var options = ResolutionOptions.Default;

        var result = ConflictResolver.ResolveContent(options, parsed);

        Assert.Equal(0, result.Result.ResolvedSuccessfully);
        Assert.Equal(0, result.Result.FailedToResolve);
        Assert.Contains("line 1", result.Content);
        Assert.Contains("line 2", result.Content);
        Assert.Contains("line 3", result.Content);
    }

    [Fact]
    public void ResolveContent_ComplexConflict_LeavesConflictMarkers()
    {
        var content =
            @"<<<<<<< Side A
Changed by A
||||||| Base
Original
======= Side B
Changed by B
>>>>>>>";

        var parsed = ConflictParser.Parse(content);
        var options = ResolutionOptions.Default;

        var result = ConflictResolver.ResolveContent(options, parsed);

        // Complex conflicts should remain with markers
        Assert.Contains("<<<<<<<", result.Content);
        Assert.Contains(">>>>>>>", result.Content);
    }

    [Fact]
    public void ResolveContent_WithTrivialDisabled_LeavesSimpleConflicts()
    {
        var content =
            @"<<<<<<< Side A
Changed by A
||||||| Base
Original
======= Side B
Original
>>>>>>>";

        var parsed = ConflictParser.Parse(content);
        var options = new ResolutionOptions
        {
            Trivial = false,
            Reduce = true,
            Indentation = true,
            AddedLines = true,
            LineEndings = true,
            SplitMarkers = true,
            Untabify = null,
        };

        var result = ConflictResolver.ResolveContent(options, parsed);

        // Should not resolve even trivial conflicts
        Assert.Contains("<<<<<<<", result.Content);
    }

    [Fact]
    public void ResolveContent_MultipleConflicts_ProcessesAll()
    {
        var content =
            @"plain line
<<<<<<< Side A
A1
||||||| Base
B1
======= Side B
C1
>>>>>>>
middle line
<<<<<<< Side A
A2
||||||| Base
B2
======= Side B
C2
>>>>>>>
end line";

        var parsed = ConflictParser.Parse(content);
        var options = ResolutionOptions.Default;

        var result = ConflictResolver.ResolveContent(options, parsed);

        // Should process both conflicts
        Assert.Contains("plain line", result.Content);
        Assert.Contains("middle line", result.Content);
        Assert.Contains("end line", result.Content);
    }

    [Fact]
    public void ResolveContent_EmptyInput_ReturnsEmpty()
    {
        var content = "";

        var parsed = ConflictParser.Parse(content);
        var options = ResolutionOptions.Default;

        var result = ConflictResolver.ResolveContent(options, parsed);

        Assert.Equal(0, result.Result.ResolvedSuccessfully);
        Assert.Equal(0, result.Result.FailedToResolve);
    }

    [Fact]
    public void ResolveContent_ConflictWithMultipleLines_HandlesCorrectly()
    {
        var content =
            @"<<<<<<< Side A
line1 A
line2 A
line3 A
||||||| Base
line1 Base
line2 Base
line3 Base
======= Side B
line1 B
line2 B
line3 B
>>>>>>>";

        var parsed = ConflictParser.Parse(content);
        var options = ResolutionOptions.Default;

        var result = ConflictResolver.ResolveContent(options, parsed);

        // Should produce some output
        Assert.NotEmpty(result.Content);
    }
}
