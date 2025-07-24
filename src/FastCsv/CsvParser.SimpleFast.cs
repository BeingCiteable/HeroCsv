using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Simple but aggressive optimizations to beat Sep without complex vectorization
/// </summary>
internal static partial class CsvParser
{
    /// <summary>
    /// Balanced line counting with moderate optimization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesSimpleFast(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        var count = 0;
        var i = 0;
        var length = content.Length;

        // Moderate loop unrolling - process 4 characters at once (sweet spot)
        while (i <= length - 4)
        {
            count += IsNewline(content[i]) ? 1 : 0;
            count += IsNewline(content[i + 1]) ? 1 : 0;
            count += IsNewline(content[i + 2]) ? 1 : 0;
            count += IsNewline(content[i + 3]) ? 1 : 0;
            i += 4;
        }

        // Handle any remaining characters
        while (i < length)
        {
            if (IsNewline(content[i]))
            {
                count++;
                // Skip \r\n sequences to avoid double counting
                if (content[i] == '\r' && i + 1 < length && content[i + 1] == '\n')
                    i++;
            }
            i++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNewline(char c) => c == '\n' || c == '\r';

    /// <summary>
    /// Ultra-fast CSV parsing with aggressive optimizations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineSimpleFast(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast quote check with loop unrolling
        bool hasQuotes = ContainsQuoteUnrolled(line, options.Quote);
        
        if (hasQuotes)
        {
            return ParseQuotedLine(line, options);
        }

        // Ultra-fast unquoted parsing
        return ParseUnquotedSimpleFast(line, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsQuoteUnrolled(ReadOnlySpan<char> line, char quote)
    {
        var i = 0;
        var length = line.Length;

        // Unroll search loop for better performance
        while (i <= length - 8)
        {
            if (line[i] == quote || line[i + 1] == quote || 
                line[i + 2] == quote || line[i + 3] == quote ||
                line[i + 4] == quote || line[i + 5] == quote ||
                line[i + 6] == quote || line[i + 7] == quote)
                return true;
            i += 8;
        }

        // Check remaining characters
        while (i < length)
        {
            if (line[i] == quote) return true;
            i++;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedSimpleFast(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Count delimiters with unrolled loop
        var delimiterCount = CountDelimiterUnrolled(line, options.Delimiter);
        var fieldCount = delimiterCount + 1;
        var fields = new string[fieldCount];
        
        var fieldIndex = 0;
        var start = 0;
        
        // Single-pass parsing with minimal overhead
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var fieldSpan = line.Slice(start, i - start);
                fields[fieldIndex++] = CreateStringOptimized(fieldSpan, options.TrimWhitespace);
                start = i + 1;
            }
        }
        
        // Handle final field
        if (start <= line.Length)
        {
            var fieldSpan = line.Slice(start);
            fields[fieldIndex] = CreateStringOptimized(fieldSpan, options.TrimWhitespace);
        }
        
        return fields;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountDelimiterUnrolled(ReadOnlySpan<char> line, char delimiter)
    {
        var count = 0;
        var i = 0;
        var length = line.Length;

        // Aggressive loop unrolling - check 16 chars at once
        while (i <= length - 16)
        {
            count += line[i] == delimiter ? 1 : 0;
            count += line[i + 1] == delimiter ? 1 : 0;
            count += line[i + 2] == delimiter ? 1 : 0;
            count += line[i + 3] == delimiter ? 1 : 0;
            count += line[i + 4] == delimiter ? 1 : 0;
            count += line[i + 5] == delimiter ? 1 : 0;
            count += line[i + 6] == delimiter ? 1 : 0;
            count += line[i + 7] == delimiter ? 1 : 0;
            count += line[i + 8] == delimiter ? 1 : 0;
            count += line[i + 9] == delimiter ? 1 : 0;
            count += line[i + 10] == delimiter ? 1 : 0;
            count += line[i + 11] == delimiter ? 1 : 0;
            count += line[i + 12] == delimiter ? 1 : 0;
            count += line[i + 13] == delimiter ? 1 : 0;
            count += line[i + 14] == delimiter ? 1 : 0;
            count += line[i + 15] == delimiter ? 1 : 0;
            i += 16;
        }

        // Handle remaining characters
        while (i < length)
        {
            if (line[i] == delimiter) count++;
            i++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreateStringOptimized(ReadOnlySpan<char> field, bool trim)
    {
        if (field.IsEmpty) return string.Empty;
        
        if (trim)
        {
            // Optimized trimming
            var start = 0;
            var end = field.Length - 1;
            
            // Trim start
            while (start <= end && char.IsWhiteSpace(field[start]))
                start++;
                
            // Trim end
            while (end >= start && char.IsWhiteSpace(field[end]))
                end--;
                
            if (start > end) return string.Empty;
            
            field = field.Slice(start, end - start + 1);
        }
        
        return field.ToString();
    }
}