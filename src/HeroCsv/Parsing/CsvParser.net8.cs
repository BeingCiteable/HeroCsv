#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace HeroCsv.Parsing;

public static partial class CsvParser
{
    // Enhanced SearchValues for various CSV operations
    private static readonly SearchValues<char> DelimiterSearchValues = SearchValues.Create(",;\t|");
    private static readonly SearchValues<char> QuoteSearchValues = SearchValues.Create("\"'");
    private static readonly SearchValues<char> QuoteAndDelimiterSearchValues = SearchValues.Create(",;\t|\"'");
    private static readonly SearchValues<char> SpecialCharsSearchValues = SearchValues.Create(",;\t|\"\'\r\n");
    private static readonly SearchValues<char> WhitespaceSearchValues = SearchValues.Create(" \t\r\n");

    /// <summary>
    /// Fast delimiter detection using SearchValues
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindNextDelimiter(ReadOnlySpan<char> content, char delimiter)
    {
        var searchValues = delimiter switch
        {
            ',' or ';' or '\t' or '|' => DelimiterSearchValues,
            _ => SearchValues.Create([delimiter])
        };

        return content.IndexOfAny(searchValues);
    }

    /// <summary>
    /// Optimized quote detection using SearchValues
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindNextQuote(ReadOnlySpan<char> content, char quoteChar)
    {
        var searchValues = quoteChar switch
        {
            '"' or '\'' => QuoteSearchValues,
            _ => SearchValues.Create([quoteChar])
        };

        return content.IndexOfAny(searchValues);
    }

    /// <summary>
    /// Fast detection of any special CSV characters
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsSpecialCharacters(ReadOnlySpan<char> content)
    {
        return content.IndexOfAny(SpecialCharsSearchValues) >= 0;
    }

    /// <summary>
    /// Optimized field boundary detection
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindFieldBoundary(ReadOnlySpan<char> content, char delimiter, char quoteChar)
    {
        var searchValues = SearchValues.Create([delimiter, quoteChar, '\r', '\n']);
        return content.IndexOfAny(searchValues);
    }

    /// <summary>
    /// Fast whitespace trimming using SearchValues
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> TrimWhitespace(ReadOnlySpan<char> content)
    {
        // Find first non-whitespace
        var start = 0;
        while (start < content.Length)
        {
            if (content.Slice(start, 1).IndexOfAny(WhitespaceSearchValues) < 0)
                break;
            start++;
        }

        // Find last non-whitespace
        var end = content.Length - 1;
        while (end >= start)
        {
            if (content.Slice(end, 1).IndexOfAny(WhitespaceSearchValues) < 0)
                break;
            end--;
        }

        return start <= end ? content.Slice(start, end - start + 1) : [];
    }
}
#endif