using System.Buffers;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Memory-optimized parsing with string pooling and allocation reduction
/// </summary>
internal static partial class CsvParser
{
    private static readonly ArrayPool<string> StringPool = ArrayPool<string>.Shared;
    private static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;

    /// <summary>
    /// Memory-optimized parsing that minimizes allocations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineMemoryOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast quote detection
        bool hasQuotes = line.IndexOf(options.Quote) >= 0;
        
        if (hasQuotes)
        {
            return ParseQuotedLine(line, options);
        }

        // Ultra-optimized unquoted parsing with minimal allocations
        return ParseUnquotedMemoryOptimized(line, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedMemoryOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Pre-count delimiters using optimized search
        var delimiterCount = CountDelimiterOptimized(line, options.Delimiter);
        var fieldCount = delimiterCount + 1;
        
        // Rent array from pool to reduce GC pressure
        var fields = fieldCount <= 32 ? new string[fieldCount] : StringPool.Rent(fieldCount);
        
        try
        {
            var fieldIndex = 0;
            var start = 0;
            
            // Single-pass parsing with span slicing
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == options.Delimiter)
                {
                    var fieldSpan = line.Slice(start, i - start);
                    fields[fieldIndex++] = CreateOptimizedString(fieldSpan, options.TrimWhitespace);
                    start = i + 1;
                }
            }
            
            // Handle final field
            if (start <= line.Length)
            {
                var fieldSpan = line.Slice(start);
                fields[fieldIndex] = CreateOptimizedString(fieldSpan, options.TrimWhitespace);
            }
            
            // Copy to exact-sized array if we used pooled array
            if (fieldCount > 32)
            {
                var result = new string[fieldCount];
                Array.Copy(fields, result, fieldCount);
                return result;
            }
            
            return fields;
        }
        finally
        {
            // Return rented array to pool
            if (fieldCount > 32)
            {
                StringPool.Return(fields, clearArray: true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountDelimiterOptimized(ReadOnlySpan<char> line, char delimiter)
    {
        var count = 0;
        
        // Use IndexOf for better performance on larger spans
        var searchSpan = line;
        int index;
        
        while ((index = searchSpan.IndexOf(delimiter)) >= 0)
        {
            count++;
            searchSpan = searchSpan.Slice(index + 1);
        }
        
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreateOptimizedString(ReadOnlySpan<char> field, bool trim)
    {
        if (field.IsEmpty) return string.Empty;
        
        // Handle common cases efficiently
        if (field.Length == 1) return field[0].ToString();
        
        if (trim)
        {
            field = field.Trim();
            if (field.IsEmpty) return string.Empty;
            if (field.Length == 1) return field[0].ToString();
        }
        
        // Use ToString for compatibility
        return field.ToString();
    }

    /// <summary>
    /// Ultra-fast line counting with IndexOf optimization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesMemoryOptimized(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

        var count = 0;
        var searchSpan = content;
        
        // Use IndexOfAny for better vectorization
        while (!searchSpan.IsEmpty)
        {
            var index = searchSpan.IndexOfAny('\n', '\r');
            if (index < 0) break;
            
            count++;
            
            // Skip the line ending character(s)
            var nextPos = index + 1;
            if (searchSpan[index] == '\r' && 
                nextPos < searchSpan.Length && 
                searchSpan[nextPos] == '\n')
            {
                nextPos++; // Skip \r\n
            }
            
            searchSpan = searchSpan.Slice(nextPos);
        }
        
        return count;
    }

    /// <summary>
    /// Specialized parser for numeric-heavy CSV data
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineNumericOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Assume no quotes for numeric data (common case)
        var delimiterCount = line.Count(options.Delimiter);
        var fieldCount = delimiterCount + 1;
        var fields = new string[fieldCount];
        
        var fieldIndex = 0;
        var start = 0;
        
        // Optimized for numeric data - no quote checking
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var fieldSpan = line.Slice(start, i - start);
                
                // Fast path for numbers - no trimming needed usually
                fields[fieldIndex++] = fieldSpan.IsEmpty ? string.Empty : fieldSpan.ToString();
                start = i + 1;
            }
        }
        
        // Final field
        if (start <= line.Length)
        {
            var fieldSpan = line.Slice(start);
            fields[fieldIndex] = fieldSpan.IsEmpty ? string.Empty : fieldSpan.ToString();
        }
        
        return fields;
    }
}