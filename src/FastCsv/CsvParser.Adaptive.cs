using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Adaptive CSV parser that selects optimal algorithms based on data characteristics
/// </summary>
internal static partial class CsvParser
{
    /// <summary>
    /// Adaptive line counting that selects the best algorithm
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesAdaptive(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

#if NET8_0_OR_GREATER
        // Use SearchValues for .NET 8+ (fastest)
        return CountLinesSearchValues(content);
#else
        // Use IndexOfAny optimization for older versions
        return CountLinesMemoryOptimized(content);
#endif
    }

    /// <summary>
    /// Adaptive parsing that selects optimal algorithm based on content analysis
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineAdaptive(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Quick content analysis to choose best parser
        var characteristics = AnalyzeLineCharacteristics(line, options);

#if NET8_0_OR_GREATER
        // Use SearchValues for .NET 8+
        if (!characteristics.HasQuotes)
        {
            return characteristics.IsNumericHeavy ? 
                ParseLineNumericOptimized(line, options) :
                ParseLineSearchValues(line, options);
        }
#endif

        // Choose best parser based on characteristics
        if (!characteristics.HasQuotes)
        {
            return characteristics.IsNumericHeavy ?
                ParseLineNumericOptimized(line, options) :
                ParseLineMemoryOptimized(line, options);
        }

        // Fall back to quoted parsing
        return ParseQuotedLine(line, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LineCharacteristics AnalyzeLineCharacteristics(ReadOnlySpan<char> line, CsvOptions options)
    {
        var hasQuotes = false;
        var numericCharCount = 0;
        var totalChars = line.Length;
        
        // Single pass analysis
        for (int i = 0; i < line.Length && i < 100; i++) // Sample first 100 chars for speed
        {
            var c = line[i];
            if (c == options.Quote)
            {
                hasQuotes = true;
            }
            else if (char.IsDigit(c) || c == '.' || c == '-' || c == '+')
            {
                numericCharCount++;
            }
        }

        var numericRatio = totalChars > 0 ? (double)numericCharCount / Math.Min(totalChars, 100) : 0;
        
        return new LineCharacteristics(hasQuotes, numericRatio > 0.6);
    }

    private readonly struct LineCharacteristics
    {
        public bool HasQuotes { get; }
        public bool IsNumericHeavy { get; }
        
        public LineCharacteristics(bool hasQuotes, bool isNumericHeavy)
        {
            HasQuotes = hasQuotes;
            IsNumericHeavy = isNumericHeavy;
        }
    }
}

/// <summary>
/// Extension methods for performance optimization helpers
/// </summary>
internal static class SpanExtensions
{
    /// <summary>
    /// Optimized character counting for spans
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count(this ReadOnlySpan<char> span, char target)
    {
        var count = 0;
        
        // Use IndexOf-based approach for better performance
        var searchSpan = span;
        int index;
        
        while ((index = searchSpan.IndexOf(target)) >= 0)
        {
            count++;
            searchSpan = searchSpan.Slice(index + 1);
        }
        
        return count;
    }
}