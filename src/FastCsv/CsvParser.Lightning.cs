using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Lightning-fast algorithms designed to beat Sep with minimal overhead
/// </summary>
internal static partial class CsvParser
{
    /// <summary>
    /// Lightning-fast line counting - absolute minimal algorithm
    /// Target: Beat Sep's 0.08ms performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesLightning(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        // Ultra-minimal approach - just count \n characters
        // Most CSV files use \n line endings, this is the fastest possible
        var count = 0;
        
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n') count++;
        }
        
        return count;
    }

    /// <summary>
    /// Lightning-fast parsing for simple CSV (no quotes, standard delimiters)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineLightning(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Super fast check - if no quotes at all, use lightning path
        if (line.IndexOf(options.Quote) < 0)
        {
            return ParseUnquotedLightning(line, options);
        }

        // Fall back to standard parsing for quoted content
        return ParseQuotedLine(line, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLightning(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Lightning-fast field splitting
        var delimiter = options.Delimiter;
        var fields = new List<string>(8); // Pre-size for common case
        
        var start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == delimiter)
            {
                var fieldSpan = line.Slice(start, i - start);
                fields.Add(options.TrimWhitespace ? fieldSpan.Trim().ToString() : fieldSpan.ToString());
                start = i + 1;
            }
        }
        
        // Add final field
        if (start <= line.Length)
        {
            var fieldSpan = line.Slice(start);
            fields.Add(options.TrimWhitespace ? fieldSpan.Trim().ToString() : fieldSpan.ToString());
        }
        
        return fields.ToArray();
    }

    /// <summary>
    /// Specialized super-fast parser for comma-separated values (most common case)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseCsvLineLightning(ReadOnlySpan<char> line)
    {
        if (line.IsEmpty) return [];

        // Hardcode comma delimiter for maximum speed
        var fields = new List<string>(8);
        var start = 0;
        
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',')
            {
                fields.Add(line.Slice(start, i - start).ToString());
                start = i + 1;
            }
        }
        
        // Add final field
        if (start <= line.Length)
        {
            fields.Add(line.Slice(start).ToString());
        }
        
        return fields.ToArray();
    }

    /// <summary>
    /// Ultra-optimized adaptive algorithm selector
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesUltraAdaptive(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        // Quick sample check - look at first 1000 chars
        var sampleSize = Math.Min(1000, content.Length);
        var sample = content.Slice(0, sampleSize);
        
        var crCount = 0;
        var lfCount = 0;
        
        for (int i = 0; i < sample.Length; i++)
        {
            if (sample[i] == '\r') crCount++;
            else if (sample[i] == '\n') lfCount++;
        }
        
        // Choose fastest algorithm based on line ending type
        if (lfCount > 0 && crCount == 0)
        {
            // Pure \n endings - use lightning fast algorithm
            return CountLinesLightning(content);
        }
        else if (crCount > 0 && lfCount == 0)
        {
            // Pure \r endings - count \r
            return CountCarriageReturns(content);
        }
        else
        {
            // Mixed endings - use careful algorithm
            return CountLinesMixed(content);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountCarriageReturns(ReadOnlySpan<char> content)
    {
        var count = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r') count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountLinesMixed(ReadOnlySpan<char> content)
    {
        var count = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                count++;
            }
            else if (content[i] == '\r')
            {
                count++;
                // Skip \r\n to avoid double counting
                if (i + 1 < content.Length && content[i + 1] == '\n')
                    i++;
            }
        }
        return count;
    }
}