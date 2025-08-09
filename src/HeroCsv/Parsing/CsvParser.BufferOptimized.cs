using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using HeroCsv.Utilities;

namespace HeroCsv.Parsing;

public static partial class CsvParser
{
    /// <summary>
    /// Parse CSV line using ArrayPool for temporary allocations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<string> ParseLineWithArrayPool(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        if (line.IsEmpty)
            return [];

        // Check if we need quote handling
        var quoteIndex = line.IndexOf('"');
        if (quoteIndex < 0)
        {
            // Fast path - no quotes, use ArrayPool for field collection
            return ParseSimpleLineWithArrayPool(line, delimiter, stringPool);
        }
        else
        {
            // Slow path - has quotes, use StringBuilder with ArrayPool backing
            return ParseQuotedLineWithArrayPool(line, delimiter, stringPool);
        }
    }

    private static List<string> ParseSimpleLineWithArrayPool(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        // Estimate field count (typically 10-50 fields)
        var estimatedFieldCount = Math.Min(50, line.Length / 4 + 1);
        var fields = new List<string>(estimatedFieldCount);

        // Rent a buffer for temporary field storage if needed
        char[]? tempBuffer = null;
        try
        {
            var start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == delimiter)
                {
                    var fieldSpan = line.Slice(start, i - start);
                    fields.Add(stringPool.GetString(fieldSpan));
                    start = i + 1;
                }
            }

            // Add the last field
            if (start <= line.Length)
            {
                var lastField = line.Slice(start);
                fields.Add(stringPool.GetString(lastField));
            }
        }
        finally
        {
            if (tempBuffer != null)
            {
                ArrayPool<char>.Shared.Return(tempBuffer);
            }
        }

        return fields;
    }

    private static List<string> ParseQuotedLineWithArrayPool(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        var fields = new List<string>();

        // Rent buffer for building quoted fields
        var bufferSize = Math.Min(4096, line.Length * 2);
        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        var bufferPosition = 0;

        try
        {
            var inQuotes = false;
            var fieldStart = 0;
            var i = 0;

            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    if (inQuotes)
                    {
                        // Check for escaped quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            // Escaped quote - add single quote to buffer
                            if (bufferPosition < buffer.Length)
                            {
                                buffer[bufferPosition++] = '"';
                            }
                            i += 2;
                            continue;
                        }
                        else
                        {
                            // End of quoted field
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        // Start of quoted field
                        inQuotes = true;
                    }
                    i++;
                }
                else if (line[i] == delimiter && !inQuotes)
                {
                    // Field boundary
                    if (bufferPosition > 0)
                    {
                        // We have buffered content
                        var fieldValue = new string(buffer, 0, bufferPosition);
                        fields.Add(stringPool.GetString(fieldValue.AsSpan()));
                        bufferPosition = 0;
                    }
                    else
                    {
                        // Direct slice from line
                        var fieldSpan = line.Slice(fieldStart, i - fieldStart);
                        fields.Add(stringPool.GetString(TrimQuotes(fieldSpan)));
                    }

                    fieldStart = i + 1;
                    i++;
                }
                else
                {
                    // Regular character
                    if (inQuotes && bufferPosition < buffer.Length)
                    {
                        buffer[bufferPosition++] = line[i];
                    }
                    i++;
                }
            }

            // Add the last field
            if (bufferPosition > 0)
            {
                var fieldValue = new string(buffer, 0, bufferPosition);
                fields.Add(stringPool.GetString(fieldValue.AsSpan()));
            }
            else if (fieldStart < line.Length)
            {
                var lastField = line.Slice(fieldStart);
                fields.Add(stringPool.GetString(TrimQuotes(lastField)));
            }
            else
            {
                fields.Add(string.Empty);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, clearArray: false);
        }

        return fields;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> TrimQuotes(ReadOnlySpan<char> field)
    {
        if (field.Length >= 2 && field[0] == '"' && field[field.Length - 1] == '"')
        {
            return field.Slice(1, field.Length - 2);
        }
        return field;
    }

    /// <summary>
    /// Batch parse multiple lines using ArrayPool for efficiency
    /// </summary>
    public static List<List<string>> ParseLinesWithArrayPool(
        ReadOnlySpan<char> content,
        char delimiter,
        StringPool stringPool,
        int maxLines = int.MaxValue)
    {
        var results = new List<List<string>>();

        // Rent a large buffer for line processing
        var lineBuffer = ArrayPool<char>.Shared.Rent(4096);

        try
        {
            var position = 0;
            var linesProcessed = 0;

            while (position < content.Length && linesProcessed < maxLines)
            {
                // Find end of current line
                var lineEnd = FindLineEnd(content, position);
                var line = content.Slice(position, lineEnd - position);

                // Parse the line
                var fields = ParseLineWithArrayPool(line, delimiter, stringPool);
                results.Add(fields);

                // Move to next line
                position = SkipLineEnding(content, lineEnd);
                linesProcessed++;
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(lineBuffer, clearArray: false);
        }

        return results;
    }
}