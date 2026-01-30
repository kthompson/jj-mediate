namespace JJResolve.Tests;

public class ConflictParserTests
{
    [Fact]
    public void Parse_SimpleConflict_ParsesCorrectly()
    {
        var content =
            @"plain line
<<<<<<< Side A
A content
||||||| Base
Base content
======= Side B
B content
>>>>>>>";

        var result = ConflictParser.Parse(content);

        Assert.Equal(2, result.Count);
        Assert.IsType<Either<string, Conflict>.Left>(result[0]);
        Assert.Equal("plain line", ((Either<string, Conflict>.Left)result[0]).Value);
        Assert.IsType<Either<string, Conflict>.Right>(result[1]);

        var conflict = ((Either<string, Conflict>.Right)result[1]).Value;
        Assert.Single(conflict.Bodies.SideA);
        Assert.Equal("A content", conflict.Bodies.SideA[0]);
        Assert.Single(conflict.Bodies.Base);
        Assert.Equal("Base content", conflict.Bodies.Base[0]);
        Assert.Single(conflict.Bodies.SideB);
        Assert.Equal("B content", conflict.Bodies.SideB[0]);
    }

    [Fact]
    public void Parse_NoConflict_ReturnsPlainLines()
    {
        var content =
            @"line 1
line 2
line 3";

        var result = ConflictParser.Parse(content);

        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.IsType<Either<string, Conflict>.Left>(r));
        Assert.Equal("line 1", ((Either<string, Conflict>.Left)result[0]).Value);
        Assert.Equal("line 2", ((Either<string, Conflict>.Left)result[1]).Value);
        Assert.Equal("line 3", ((Either<string, Conflict>.Left)result[2]).Value);
    }

    [Fact]
    public void Parse_MultipleConflicts_ParsesAll()
    {
        var content =
            @"before
<<<<<<< Side A
A1
||||||| Base
B1
======= Side B
C1
>>>>>>>
middle
<<<<<<< Side A
A2
||||||| Base
B2
======= Side B
C2
>>>>>>>
after";

        var result = ConflictParser.Parse(content);

        Assert.Equal(5, result.Count);
        Assert.IsType<Either<string, Conflict>.Left>(result[0]);
        Assert.Equal("before", ((Either<string, Conflict>.Left)result[0]).Value);
        Assert.IsType<Either<string, Conflict>.Right>(result[1]);
        Assert.IsType<Either<string, Conflict>.Left>(result[2]);
        Assert.Equal("middle", ((Either<string, Conflict>.Left)result[2]).Value);
        Assert.IsType<Either<string, Conflict>.Right>(result[3]);
        Assert.IsType<Either<string, Conflict>.Left>(result[4]);
        Assert.Equal("after", ((Either<string, Conflict>.Left)result[4]).Value);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyList()
    {
        var content = "";
        var result = ConflictParser.Parse(content);
        Assert.Single(result);
        Assert.IsType<Either<string, Conflict>.Left>(result[0]);
        Assert.Equal("", ((Either<string, Conflict>.Left)result[0]).Value);
    }

    [Fact]
    public void Parse_ConflictWithEmptySection_ParsesCorrectly()
    {
        var content =
            @"<<<<<<< Side A
||||||| Base
Base content
======= Side B
B content
>>>>>>>";

        var result = ConflictParser.Parse(content);

        Assert.Single(result);
        Assert.IsType<Either<string, Conflict>.Right>(result[0]);

        var conflict = ((Either<string, Conflict>.Right)result[0]).Value;
        Assert.Empty(conflict.Bodies.SideA);
        Assert.Single(conflict.Bodies.Base);
        Assert.Single(conflict.Bodies.SideB);
    }
}
