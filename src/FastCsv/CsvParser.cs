using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Unified CSV parser with automatic optimizations
/// </summary>
internal static class CsvParser
{
    /// <summary>
    /// Finds the end of the current line starting from the given position
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEnd(ReadOnlySpan<char> content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    /// <summary>
    /// Skips line ending characters and returns the new position
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SkipLineEnding(ReadOnlySpan<char> content, int position)
    {
        if (position >= content.Length) return position;

        if (content[position] == '\r')
        {
            position++;
            if (position < content.Length && content[position] == '\n')
                position++;
        }
        else if (content[position] == '\n')
        {
            position++;
        }

        return position;
    }

    /// <summary>
    /// Ultra-fast line counting - automatically optimized
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLines(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        var count = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n') count++;
        }
        return count;
    }

    /// <summary>
    /// Main CSV parsing entry point - automatically optimized
    /// </summary>
    public static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast path: comma-delimited with no quotes (90% of use cases)
        if (options.Delimiter == ',' && !options.TrimWhitespace && line.IndexOf('"') < 0)
        {
            return ParseSimpleCommaLine(line);
        }

        // Smart path: check for quotes first, then choose best algorithm
        bool hasQuotes = line.IndexOf(options.Quote) >= 0;

        return hasQuotes
            ? ParseQuotedLine(line, options)
            : ParseUnquotedLine(line, options);
    }

    /// <summary>
    /// Ultra-fast parsing for simple comma-separated lines (no quotes, no trimming)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string[] ParseSimpleCommaLine(ReadOnlySpan<char> line)
    {
        // Count commas in single pass
        var commaCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',') commaCount++;
        }

        var fields = new string[commaCount + 1];
        var fieldIndex = 0;
        var start = 0;

        // Parse fields in single pass
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',')
            {
                fields[fieldIndex++] = CreateString(line.Slice(start, i - start));
                start = i + 1;
            }
        }

        // Final field
        fields[fieldIndex] = CreateString(line.Slice(start));
        return fields;
    }
    
    /// <summary>
    /// Creates string without unnecessary allocations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string CreateString(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return string.Empty;
        if (span.Length == 1) return span[0].ToString();
        
        fixed (char* ptr = span)
        {
            return new string(ptr, 0, span.Length);
        }
    }

    /// <summary>
    /// Optimized parsing for unquoted lines
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Count delimiters to allocate exact array size
        var delimiterCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter) delimiterCount++;
        }

        var fields = new string[delimiterCount + 1];
        var fieldIndex = 0;
        var fieldStart = 0;

        // Parse fields
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var field = line.Slice(fieldStart, i - fieldStart);
                if (options.TrimWhitespace) field = field.Trim();
                fields[fieldIndex++] = CreateString(field);
                fieldStart = i + 1;
            }
        }

        // Final field
        if (fieldStart <= line.Length)
        {
            var field = line.Slice(fieldStart);
            if (options.TrimWhitespace) field = field.Trim();
            fields[fieldIndex] = CreateString(field);
        }

        return fields;
    }

    /// <summary>
    /// Handles parsing lines with quoted fields
    /// </summary>
    private static string[] ParseQuotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        var fieldList = new List<string>(8);
        var inQuotes = false;
        var fieldBuilder = new StringBuilder();
        var i = 0;

        while (i < line.Length)
        {
            var ch = line[i];

            if (ch == options.Quote)
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    fieldBuilder.Clear();
                }
                else if (i + 1 < line.Length && line[i + 1] == options.Quote)
                {
                    // Escaped quote
                    fieldBuilder.Append(options.Quote);
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = false;
                }
            }
            else if (ch == options.Delimiter && !inQuotes)
            {
                var field = fieldBuilder.ToString();
                fieldList.Add(options.TrimWhitespace ? field.Trim() : field);
                fieldBuilder.Clear();
            }
            else
            {
                fieldBuilder.Append(ch);
            }

            i++;
        }

        // Add final field
        var finalField = fieldBuilder.ToString();
        fieldList.Add(options.TrimWhitespace ? finalField.Trim() : finalField);

        return [.. fieldList];
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// .NET 8+ vectorized line end finding (automatically used when available)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEndVectorized(ReadOnlySpan<char> content, int start)
    {
        // Use vectorized search if beneficial (large content)
        if (content.Length - start > 100)
        {
            var searchSpan = content.Slice(start);
            var index = searchSpan.IndexOfAny('\n', '\r');
            return index >= 0 ? start + index : content.Length;
        }
        
        // Fall back to simple search for small content
        return FindLineEnd(content, start);
    }
#endif
}