#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// .NET 8+ SearchValues optimizations for ultra-fast character searching
/// </summary>
internal static partial class CsvParser
{
    private static readonly SearchValues<char> NewlineSearchValues = SearchValues.Create(['\r', '\n']);
    
    /// <summary>
    /// Ultra-fast line counting using SearchValues (.NET 8+)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesSearchValues(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        var count = 0;
        var searchSpan = content;
        
        // Use SearchValues for hardware-accelerated search
        while (!searchSpan.IsEmpty)
        {
            var index = searchSpan.IndexOfAny(NewlineSearchValues);
            if (index < 0) break;
            
            count++;
            
            // Skip line ending efficiently
            var nextPos = index + 1;
            if (searchSpan[index] == '\r' && 
                nextPos < searchSpan.Length && 
                searchSpan[nextPos] == '\n')
            {
                nextPos++;
            }
            
            searchSpan = searchSpan.Slice(nextPos);
        }
        
        return count;
    }

    /// <summary>
    /// Dynamic SearchValues creation for custom delimiters
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SearchValues<char> GetDelimiterSearchValues(char delimiter)
    {
        // Cache common delimiters
        return delimiter switch
        {
            ',' => SearchValues.Create([',']),
            ';' => SearchValues.Create([';']),
            '\t' => SearchValues.Create(['\t']),
            '|' => SearchValues.Create(['|']),
            _ => SearchValues.Create([delimiter])
        };
    }

    /// <summary>
    /// SearchValues-optimized delimiter counting
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDelimiterSearchValues(ReadOnlySpan<char> line, char delimiter)
    {
        var searchValues = GetDelimiterSearchValues(delimiter);
        var count = 0;
        var searchSpan = line;
        
        while (!searchSpan.IsEmpty)
        {
            var index = searchSpan.IndexOfAny(searchValues);
            if (index < 0) break;
            
            count++;
            searchSpan = searchSpan.Slice(index + 1);
        }
        
        return count;
    }

    /// <summary>
    /// SearchValues-optimized parsing for .NET 8+
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineSearchValues(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast quote detection using SearchValues
        var quoteSearchValues = SearchValues.Create([options.Quote]);
        bool hasQuotes = line.IndexOfAny(quoteSearchValues) >= 0;
        
        if (hasQuotes)
        {
            return ParseQuotedLine(line, options);
        }

        // Use SearchValues for delimiter parsing
        return ParseUnquotedSearchValues(line, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedSearchValues(ReadOnlySpan<char> line, CsvOptions options)
    {
        var delimiterCount = CountDelimiterSearchValues(line, options.Delimiter);
        var fieldCount = delimiterCount + 1;
        var fields = new string[fieldCount];
        
        var fieldIndex = 0;
        var searchValues = GetDelimiterSearchValues(options.Delimiter);
        var searchSpan = line;
        var currentPos = 0;
        
        // Parse using SearchValues
        while (fieldIndex < fieldCount - 1)
        {
            var index = searchSpan.IndexOfAny(searchValues);
            if (index < 0) break;
            
            var fieldSpan = line.Slice(currentPos, index);
            fields[fieldIndex++] = CreateOptimizedString(fieldSpan, options.TrimWhitespace);
            
            currentPos += index + 1;
            searchSpan = line.Slice(currentPos);
        }
        
        // Handle final field
        if (currentPos <= line.Length)
        {
            var fieldSpan = line.Slice(currentPos);
            fields[fieldIndex] = CreateOptimizedString(fieldSpan, options.TrimWhitespace);
        }
        
        return fields;
    }
}
#endif