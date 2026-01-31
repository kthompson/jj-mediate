namespace JJResolve;

/// <summary>
/// Parses merge conflicts from file content.
/// </summary>
public static class ConflictParser
{
    /// <summary>
    /// Parses content into a sequence of either plain lines or conflicts.
    /// </summary>
    public static List<Either<string, Conflict>> Parse(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var srcLines = lines
            .Select((line, index) => new SrcContent { LineNo = index + 1, Content = line })
            .ToList();

        return ParseFromNumberedLines(srcLines);
    }

    private static List<Either<string, Conflict>> ParseFromNumberedLines(List<SrcContent> lines)
    {
        var result = new List<Either<string, Conflict>>();
        var index = 0;

        while (index < lines.Count)
        {
            var (plainLines, markerA) = BreakUpToMarker(lines, ref index, '<', null);

            // Add all plain lines before the conflict marker
            result.AddRange(plainLines.Select(l => new Either<string, Conflict>.Left(l.Content)));

            if (markerA != null)
            {
                // Parse the conflict
                var conflict = ParseConflict(lines, ref index, markerA);
                result.Add(new Either<string, Conflict>.Right(conflict));
            }
        }

        return result;
    }

    private static Conflict ParseConflict(List<SrcContent> lines, ref int index, SrcContent markerA)
    {
        var markerCount = CountMarkerChars(markerA.Content, '<');

        var (linesA, markerBase) = ReadUpToMarker(lines, ref index, '|', markerCount);
        var (linesBase, markerB) = ReadUpToMarker(lines, ref index, '=', markerCount);
        var (linesB, markerEnd) = ReadUpToMarker(lines, ref index, '>', markerCount);

        return new Conflict
        {
            Markers = new Sides<SrcContent>
            {
                SideA = markerA,
                Base = markerBase,
                SideB = markerB,
            },
            MarkerEnd = markerEnd,
            Bodies = new Sides<List<string>>
            {
                SideA = linesA.Select(l => l.Content).ToList(),
                Base = linesBase.Select(l => l.Content).ToList(),
                SideB = linesB.Select(l => l.Content).ToList(),
            },
        };
    }

    private static (List<SrcContent> lines, SrcContent? marker) BreakUpToMarker(
        List<SrcContent> allLines,
        ref int index,
        char markerChar,
        int? count
    )
    {
        var lines = new List<SrcContent>();
        var markerCount = count ?? 7;
        var prefix = new string(markerChar, markerCount);

        while (index < allLines.Count)
        {
            var line = allLines[index];

            if (IsMarker(line.Content, prefix, markerChar, count))
            {
                index++; // Consume the marker
                return (lines, line);
            }

            lines.Add(line);
            index++;
        }

        return (lines, null);
    }

    private static (List<SrcContent> lines, SrcContent marker) ReadUpToMarker(
        List<SrcContent> allLines,
        ref int index,
        char markerChar,
        int count
    )
    {
        var (lines, marker) = BreakUpToMarker(allLines, ref index, markerChar, count);

        if (marker == null)
        {
            var preview = string.Join("\n", lines.Take(5).Select(l => $"{l.LineNo}\t{l.Content}"));
            throw new InvalidOperationException(
                $"Parse error: failed reading up to marker '{markerChar}', got:\n{preview}"
            );
        }

        return (lines, marker);
    }

    private static bool IsMarker(string line, string prefix, char markerChar, int? exactCount)
    {
        if (!line.StartsWith(prefix))
            return false;

        if (exactCount.HasValue)
        {
            // When we know the exact count, ensure the next char is NOT the marker char
            if (line.Length > exactCount.Value && line[exactCount.Value] == markerChar)
                return false;
        }

        return true;
    }

    private static int CountMarkerChars(string line, char markerChar)
    {
        return line.TakeWhile(c => c == markerChar).Count();
    }
}
