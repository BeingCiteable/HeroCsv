using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Central utility class for CSV parsing operations
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
    /// Parses a CSV line into fields using the optimized span-based approach
    /// </summary>
    public static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Check if line contains quotes for optimization
        bool hasQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Quote)
            {
                hasQuotes = true;
                break;
            }
        }

        return hasQuotes ? ParseQuotedLine(line, options) : ParseUnquotedLine(line, options);
    }

    /// <summary>
    /// Fast path for parsing lines without quotes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Count fields first for exact allocation
        var fieldCount = 1;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter) fieldCount++;
        }

        var fields = new string[fieldCount];
        var fieldIndex = 0;
        var fieldStart = 0;

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
}