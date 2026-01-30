namespace JJResolve;

/// <summary>
/// Core conflict resolution engine.
/// </summary>
public static class ConflictResolver
{
    /// <summary>
    /// Resolves conflicts in parsed content.
    /// </summary>
    public static NewContent ResolveContent(
        ResolutionOptions options,
        List<Either<string, Conflict>> parsed
    )
    {
        var result = NewContent.Empty;

        foreach (var item in parsed)
        {
            result += item.Match(
                plainLine => new NewContent
                {
                    Result = Result.Empty,
                    Content = plainLine + Environment.NewLine,
                },
                conflict => ResolveConflictFull(options, conflict)
            );
        }

        return result;
    }

    private static NewContent ResolveConflictFull(ResolutionOptions options, Conflict conflict)
    {
        // Apply preprocessing
        if (options.Untabify.HasValue)
            conflict = UntabifyConflict(conflict, options.Untabify.Value);

        if (options.LineEndings)
            conflict = LineBreakFix(conflict);

        // Split if needed
        var parts = options.SplitMarkers ? SplitConflict(conflict) : new[] { conflict };

        // Resolve each part
        var results = parts.Select(c => FormatResolution(ResolveConflict(options, c))).ToList();

        // Aggregate results
        var aggregated = results.Aggregate(NewContent.Empty, (a, b) => a + b);

        // If we split and got multiple parts, count as reduction if not fully successful
        if (
            parts.Length > 1
            && (aggregated.Result.FailedToResolve > 0 || aggregated.Result.ReducedConflicts > 0)
        )
        {
            return aggregated with
            {
                Result = new Result
                {
                    ResolvedSuccessfully = 0,
                    ReducedConflicts = 1,
                    FailedToResolve = 0,
                },
            };
        }
        else if (parts.Length > 1 && aggregated.Result.ResolvedSuccessfully > 0)
        {
            return aggregated with
            {
                Result = new Result
                {
                    ResolvedSuccessfully = 1,
                    ReducedConflicts = 0,
                    FailedToResolve = 0,
                },
            };
        }

        return aggregated;
    }

    /// <summary>
    /// Core three-way resolution logic for a single conflict.
    /// </summary>
    private static Resolution ResolveConflict(ResolutionOptions options, Conflict conflict)
    {
        var matchTop = 0;
        var matchBottom = 0;

        if (options.Reduce)
        {
            // Find matching lines at top and bottom
            matchTop = Match(conflict.Bodies.Base, conflict.Bodies.SideA, conflict.Bodies.SideB);

            var revBottom = conflict.Bodies.Select(body => body.Skip(matchTop).Reverse().ToList());
            matchBottom = Match(revBottom.Base, revBottom.SideA, revBottom.SideB);
        }

        if (matchTop == 0 && matchBottom == 0 || !options.Reduce)
        {
            // No reduction possible, try direct resolution
            var resolved = ResolveReduced(options, conflict.Bodies);
            return resolved != null
                ? new Resolution.Resolved(
                    string.Join(Environment.NewLine, resolved) + Environment.NewLine
                )
                : new Resolution.NoResolution(conflict);
        }
        else
        {
            // Reduce the conflict by removing matching parts
            var reduced = conflict.WithEachBody(body =>
                body.Skip(matchTop).Take(body.Count - matchTop - matchBottom).ToList()
            );

            var resolvedLines = ResolveGenLines(options, reduced.Bodies);

            if (resolvedLines != null)
            {
                // Successfully resolved the reduced conflict
                var result = conflict
                    .Bodies.SideA.Take(matchTop)
                    .Concat(resolvedLines)
                    .Concat(conflict.Bodies.SideA.TakeLast(matchBottom));

                return new Resolution.Resolved(
                    string.Join(Environment.NewLine, result) + Environment.NewLine
                );
            }
            else
            {
                // Couldn't resolve, but we reduced it
                var reducedText =
                    string.Join(Environment.NewLine, reduced.PrettyLines()) + Environment.NewLine;
                var result = conflict
                    .Bodies.SideA.Take(matchTop)
                    .Append(reducedText.TrimEnd('\r', '\n'))
                    .Concat(conflict.Bodies.SideA.TakeLast(matchBottom));

                return new Resolution.PartialResolution(
                    string.Join(Environment.NewLine, result) + Environment.NewLine
                );
            }
        }
    }

    private static int Match(List<string> baseLines, List<string> a, List<string> b)
    {
        if (baseLines.Count == 0)
            return LengthOfCommonPrefix(a, b);

        return Math.Min(LengthOfCommonPrefix(baseLines, a), LengthOfCommonPrefix(baseLines, b));
    }

    private static int LengthOfCommonPrefix(List<string> a, List<string> b)
    {
        return a.Zip(b, (x, y) => x == y).TakeWhile(match => match).Count();
    }

    /// <summary>
    /// Generic three-way resolution: if A==Base use B, if B==Base use A, if A==B use A.
    /// </summary>
    private static T? ResolveGen<T>(Sides<T> sides)
    {
        if (EqualityComparer<T>.Default.Equals(sides.SideA, sides.Base))
            return sides.SideB;
        if (EqualityComparer<T>.Default.Equals(sides.SideB, sides.Base))
            return sides.SideA;
        if (EqualityComparer<T>.Default.Equals(sides.SideA, sides.SideB))
            return sides.SideA;

        return default;
    }

    /// <summary>
    /// Resolves lines with optional added-lines detection.
    /// </summary>
    private static List<T>? ResolveGenLines<T>(ResolutionOptions options, Sides<List<T>> sides)
    {
        // Try trivial resolution first
        if (options.Trivial)
        {
            var trivial = ResolveGen(sides);
            if (trivial != null)
                return trivial;
        }

        // Try added-lines strategy
        if (options.AddedLines)
        {
            var added = AddedBothSides(sides.SideA, sides.SideB, sides.Base)
                .Concat(AddedBothSides(sides.SideB, sides.SideA, sides.Base))
                .ToList();

            if (added.Count == 1)
                return added[0];
        }

        return null;
    }

    private static IEnumerable<List<T>> AddedBothSides<T>(List<T> x, List<T> y, List<T> baseList)
    {
        var n = baseList.Count;

        // Check if x ends with base and y starts with base (both added to opposite ends)
        if (x.Count >= n && y.Count >= n)
        {
            var xSuffix = x.Skip(x.Count - n).ToList();
            var yPrefix = y.Take(n).ToList();

            if (SequenceEqual(xSuffix, baseList) && SequenceEqual(yPrefix, baseList))
            {
                yield return x.Concat(y.Skip(n)).ToList();
            }
        }
    }

    /// <summary>
    /// Resolves with indentation awareness.
    /// </summary>
    private static List<string>? ResolveReduced(
        ResolutionOptions options,
        Sides<List<string>> sides
    )
    {
        if (options.Indentation)
        {
            var prefixes = sides.Select(body => CommonPrefix(body));
            var prefixResolved = ResolveGen(prefixes);

            if (prefixResolved != null)
            {
                var unprefixed = new Sides<List<string>>
                {
                    SideA = sides
                        .SideA.Select(line =>
                            line.Substring(Math.Min(prefixes.SideA.Length, line.Length))
                        )
                        .ToList(),
                    Base = sides
                        .Base.Select(line =>
                            line.Substring(Math.Min(prefixes.Base.Length, line.Length))
                        )
                        .ToList(),
                    SideB = sides
                        .SideB.Select(line =>
                            line.Substring(Math.Min(prefixes.SideB.Length, line.Length))
                        )
                        .ToList(),
                };

                var resolved = ResolveGenLines(options, unprefixed);
                if (resolved != null)
                    return resolved.Select(line => prefixResolved + line).ToList();
            }
        }

        return ResolveGenLines(options, sides);
    }

    private static string CommonPrefix(List<string> lines)
    {
        if (lines.Count == 0)
            return "";
        if (lines.Count == 1)
            return lines[0].TakeWhile(c => c == ' ').Aggregate("", (s, c) => s + c);

        var prefix = lines[0].TakeWhile(c => c == ' ').Aggregate("", (s, c) => s + c);

        foreach (var line in lines.Skip(1))
        {
            var linePrefix = line.TakeWhile(c => c == ' ').Aggregate("", (s, c) => s + c);
            prefix = prefix
                .Zip(linePrefix, (a, b) => a == b ? a : '\0')
                .TakeWhile(c => c != '\0')
                .Aggregate("", (s, c) => s + c);
        }

        return prefix;
    }

    private static bool SequenceEqual<T>(List<T> a, List<T> b)
    {
        return a.Count == b.Count
            && a.Zip(b, (x, y) => EqualityComparer<T>.Default.Equals(x, y)).All(x => x);
    }

    private static NewContent FormatResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.NoResolution nr => new NewContent
            {
                Result = new Result
                {
                    ResolvedSuccessfully = 0,
                    ReducedConflicts = 0,
                    FailedToResolve = 1,
                },
                Content = nr.Conflict.Pretty(),
            },
            Resolution.Resolved r => new NewContent
            {
                Result = new Result
                {
                    ResolvedSuccessfully = 1,
                    ReducedConflicts = 0,
                    FailedToResolve = 0,
                },
                Content = r.Content,
            },
            Resolution.PartialResolution pr => new NewContent
            {
                Result = new Result
                {
                    ResolvedSuccessfully = 0,
                    ReducedConflicts = 1,
                    FailedToResolve = 0,
                },
                Content = pr.Content,
            },
            _ => throw new InvalidOperationException(),
        };
    }

    private static Conflict UntabifyConflict(Conflict conflict, int tabWidth)
    {
        return conflict.WithStrings(line => UntabifyStr(line, tabWidth));
    }

    private static string UntabifyStr(string str, int size)
    {
        var result = new System.Text.StringBuilder();
        var col = 0;

        foreach (var c in str)
        {
            if (c == '\t')
            {
                var spaces = size - col;
                result.Append(new string(' ', spaces));
                col = 0;
            }
            else
            {
                result.Append(c);
                col = (col + 1) % size;
            }
        }

        return result.ToString();
    }

    private enum LineEnding
    {
        LF,
        CRLF,
        Mixed,
    }

    private static LineEnding InferLineEnding(string line)
    {
        return line.Length > 0 && line[^1] == '\r' ? LineEnding.CRLF : LineEnding.LF;
    }

    private static LineEnding InferLineEndings(List<string> lines)
    {
        if (lines.Count == 0)
            return LineEnding.Mixed;

        var endings = lines.Select(InferLineEnding).Distinct().ToList();
        return endings.Count == 1 ? endings[0] : LineEnding.Mixed;
    }

    private static Conflict LineBreakFix(Conflict conflict)
    {
        if (conflict.Bodies.AsEnumerable().Any(body => body.Count == 0))
            return conflict;

        var endings = conflict.Bodies.Select(InferLineEndings);

        if (endings.AsEnumerable().Distinct().Count() == 1)
            return conflict;

        // For LineEnding we need special handling since it's an enum
        if (endings.SideA == endings.Base && endings.SideA != endings.SideB)
        {
            return endings.SideB switch
            {
                LineEnding.LF => conflict.WithStrings(RemoveCr),
                LineEnding.CRLF => conflict.WithStrings(MakeCr),
                _ => conflict,
            };
        }
        else if (endings.SideB == endings.Base && endings.SideA != endings.SideB)
        {
            return endings.SideA switch
            {
                LineEnding.LF => conflict.WithStrings(RemoveCr),
                LineEnding.CRLF => conflict.WithStrings(MakeCr),
                _ => conflict,
            };
        }
        else if (endings.SideA == endings.SideB)
        {
            return endings.SideA switch
            {
                LineEnding.LF => conflict.WithStrings(RemoveCr),
                LineEnding.CRLF => conflict.WithStrings(MakeCr),
                _ => conflict,
            };
        }

        return conflict;
    }

    private static string RemoveCr(string line)
    {
        return line.Length > 0 && line[^1] == '\r' ? line[..^1] : line;
    }

    private static string MakeCr(string line)
    {
        return line.Length > 0 && line[^1] == '\r' ? line : line + "\r";
    }

    private static Conflict[] SplitConflict(Conflict conflict)
    {
        var splits = conflict.Bodies.Select(body =>
            body.Select((line, index) => (line, index))
                .Where(x => x.line.StartsWith("~~~~~~~"))
                .Select(x => x.index)
                .ToList()
        );

        // Check if all sides have splits at the same positions
        if (!splits.SideA.SequenceEqual(splits.Base) || !splits.SideA.SequenceEqual(splits.SideB))
            return new[] { conflict };

        if (splits.SideA.Count == 0)
            return new[] { conflict };

        var result = new List<Conflict>();
        var prevIndex = 0;

        foreach (var splitIndex in splits.SideA.Append(conflict.Bodies.SideA.Count))
        {
            result.Add(
                conflict.WithBodies(sides => new Sides<List<string>>
                {
                    SideA = sides.SideA.Skip(prevIndex).Take(splitIndex - prevIndex).ToList(),
                    Base = sides.Base.Skip(prevIndex).Take(splitIndex - prevIndex).ToList(),
                    SideB = sides.SideB.Skip(prevIndex).Take(splitIndex - prevIndex).ToList(),
                })
            );

            prevIndex = splitIndex + 1; // Skip the separator line
        }

        return result.Where(c => c.Bodies.SideA.Count > 0).ToArray();
    }
}
