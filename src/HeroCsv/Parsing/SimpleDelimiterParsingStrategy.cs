using System;
using System.Runtime.CompilerServices;
using HeroCsv.Core;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// High-performance strategy for simple delimiter-separated values without quotes
/// </summary>
public sealed class SimpleDelimiterParsingStrategy : IParsingStrategy
{
    public int Priority => 100; // High priority for common case

    public bool IsAvailable => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanHandle(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Fast path: any delimiter, no quotes, no trimming
        return !options.TrimWhitespace && line.IndexOf('"') < 0;
    }

    public string[] Parse(ReadOnlySpan<char> line, CsvOptions options)
    {
        return ParseSimpleDelimiterLine(line, options.Delimiter, options.StringPool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseSimpleDelimiterLine(ReadOnlySpan<char> line, char delimiter, Utilities.StringPool? stringPool)
    {
        if (line.IsEmpty)
            return [];

        // Count delimiters to determine array size
        int delimiterCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == delimiter)
                delimiterCount++;
        }

        var fields = new string[delimiterCount + 1];
        int fieldIndex = 0;
        int startIndex = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == delimiter)
            {
                var fieldSpan = line.Slice(startIndex, i - startIndex);
                fields[fieldIndex++] = stringPool?.GetString(fieldSpan) ?? fieldSpan.ToString();
                startIndex = i + 1;
            }
        }

        // Add the last field
        if (fieldIndex < fields.Length)
        {
            var lastFieldSpan = line.Slice(startIndex);
            fields[fieldIndex] = stringPool?.GetString(lastFieldSpan) ?? lastFieldSpan.ToString();
        }

        return fields;
    }
}