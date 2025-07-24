using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Optimized CSV parsing focused on hot-path performance
/// </summary>
internal static partial class CsvParser
{
    /// <summary>
    /// Optimized ParseLine that eliminates redundant work
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineSimpleOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Check for quotes and count delimiters in single pass
        bool hasQuotes = false;
        int delimiterCount = 0;
        
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == options.Quote)
            {
                hasQuotes = true;
            }
            else if (ch == options.Delimiter)
            {
                delimiterCount++;
            }
        }

        return hasQuotes 
            ? ParseQuotedLine(line, options) 
            : ParseUnquotedLineOptimized(line, options, delimiterCount + 1);
    }

    /// <summary>
    /// Optimized unquoted line parsing with pre-computed field count
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLineOptimized(ReadOnlySpan<char> line, CsvOptions options, int fieldCount)
    {
        var fields = new string[fieldCount];
        var fieldIndex = 0;
        var fieldStart = 0;

        // Single pass parsing without redundant delimiter counting
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var field = line.Slice(fieldStart, i - fieldStart);
                fields[fieldIndex++] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
                fieldStart = i + 1;
            }
        }

        // Add final field
        if (fieldStart <= line.Length)
        {
            var field = line.Slice(fieldStart);
            fields[fieldIndex] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
        }

        return fields;
    }

    /// <summary>
    /// Ultra-fast line end finding using simple optimizations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEndOptimized(ReadOnlySpan<char> content, int start)
    {
        // Use unsafe access for better performance
        for (int i = start; i < content.Length; i++)
        {
            char ch = content[i];
            if ((ch == '\n') | (ch == '\r')) // Use bitwise OR to avoid branching
                return i;
        }
        return content.Length;
    }

    /// <summary>
    /// Count-only optimization for record counting using ultra-fast algorithms
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLinesOptimized(ReadOnlySpan<char> content)
    {
        // Use lightning-fast counting designed to beat Sep
        return CountLinesUltraAdaptive(content);
    }
}