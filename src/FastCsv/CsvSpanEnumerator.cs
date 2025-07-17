using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Zero-allocation enumerator that works directly with spans
/// </summary>
internal struct CsvSpanEnumerator(
    ReadOnlyMemory<char> content,
    CsvOptions options) : IEnumerator<string[]>
{
    private int _position = 0;
    private string[]? _current = null;

    public readonly string[] Current => _current ?? throw new InvalidOperationException();

    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        var span = content.Span;
        
        if (_position >= span.Length)
        {
            _current = null;
            return false;
        }

        // Find line end
        var lineEnd = FindLineEnd(span, _position);
        var lineSpan = span.Slice(_position, lineEnd - _position);

        // Skip to next line
        _position = SkipLineEnding(span, lineEnd);

        // Parse line if not empty
        if (lineSpan.Length > 0)
        {
            var fields = ParseLine(lineSpan, options);
            if (fields.Length > 0)
            {
                _current = fields;
                return true;
            }
            else
            {
                // Empty fields, try next line
                return MoveNext();
            }
        }
        else
        {
            // Empty line, try next
            return MoveNext();
        }
    }

    public void Reset()
    {
        _position = 0;
        _current = null;
    }

    public readonly void Dispose()
    {
        // Nothing to dispose
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int FindLineEnd(ReadOnlySpan<char> content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SkipLineEnding(ReadOnlySpan<char> content, int position)
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

    internal static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Check if line contains quotes
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Count fields
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

/// <summary>
/// Zero-allocation enumerable that works with ReadOnlyMemory
/// </summary>
internal readonly struct CsvMemoryEnumerable(
    ReadOnlyMemory<char> content,
    CsvOptions options) : IEnumerable<string[]>
{
    public IEnumerator<string[]> GetEnumerator()
    {
        return new CsvSpanEnumerator(content, options);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
