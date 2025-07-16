using System.Runtime.CompilerServices;
using System.Text;

#if NETSTANDARD2_0
#endif

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

#if NET7_0_OR_GREATER
using System.Buffers.Text;
#endif

#if NET8_0_OR_GREATER
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Runtime.Intrinsics;
using System.Text.Unicode;
using System.Buffers;
#endif


namespace FastCsv;

/// <summary>
/// Enumerator for fields within a CSV record
/// </summary>
public ref struct CsvFieldEnumerator
{
    private readonly ReadOnlySpan<char> _recordData;
    private readonly CsvOptions _options;
    private int _position;
    private ReadOnlySpan<char> _current;

    internal CsvFieldEnumerator(ReadOnlySpan<char> recordData, CsvOptions options)
    {
        _recordData = recordData;
        _options = options;
        _position = 0;
        _current = ReadOnlySpan<char>.Empty;
    }

    public readonly CsvFieldEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_position >= _recordData.Length)
            return false;

        _current = ReadNextField();
        return true;
    }

    public readonly ReadOnlySpan<char> Current => _current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> ReadNextField()
    {
        if (_position >= _recordData.Length)
            return ReadOnlySpan<char>.Empty;

        var start = _position;
        var delimiter = _options.Delimiter;
        var quote = _options.Quote;

        // Check if field is quoted
        if (_recordData[_position] == quote)
        {
            return ReadQuotedField();
        }

#if NET8_0_OR_GREATER
        // Use SearchValues for fast delimiter scanning
        var remaining = _recordData.Slice(_position);
        var delimiterSearch = SearchValues.Create([delimiter]);
        var nextDelimiter = remaining.IndexOfAny(delimiterSearch);
        
        if (nextDelimiter == -1)
        {
            // No more delimiters - this is the last field
            _position = _recordData.Length;
            var field = _recordData.Slice(start);
            return _options.TrimWhitespace ? field.Trim() : field;
        }
        
        _position = start + nextDelimiter + 1; // Move past delimiter
        var resultField = _recordData.Slice(start, nextDelimiter);
        return _options.TrimWhitespace ? resultField.Trim() : resultField;
#else
        // Read unquoted field (fallback)
        while (_position < _recordData.Length && _recordData[_position] != delimiter)
        {
            _position++;
        }

        var field = _recordData.Slice(start, _position - start);

        // Skip delimiter for next field
        if (_position < _recordData.Length && _recordData[_position] == delimiter)
            _position++;

        return _options.TrimWhitespace ? field.Trim() : field;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> ReadQuotedField()
    {
        var quote = _options.Quote;
        var delimiter = _options.Delimiter;

        _position++; // Skip opening quote
        var start = _position;

        while (_position < _recordData.Length)
        {
            if (_recordData[_position] == quote)
            {
                if (_position + 1 < _recordData.Length && _recordData[_position + 1] == quote)
                {
                    // Escaped quote - we'll need to process this
                    return ReadQuotedFieldWithEscapes(start);
                }

                // End of quoted field
                var field = _recordData.Slice(start, _position - start);
                _position++; // Skip closing quote

                // Skip delimiter
                if (_position < _recordData.Length && _recordData[_position] == delimiter)
                    _position++;

                return field;
            }
            _position++;
        }

        // Unterminated quote - return rest of data
        return _recordData.Slice(start);
    }

    private ReadOnlySpan<char> ReadQuotedFieldWithEscapes(int fieldStart)
    {
        // For fields with escaped quotes, we need to build the result
        // This is the only allocation-requiring path
        var result = new StringBuilder();
        var pos = fieldStart;
        var quote = _options.Quote;

        while (pos < _recordData.Length)
        {
            if (_recordData[pos] == quote)
            {
                if (pos + 1 < _recordData.Length && _recordData[pos + 1] == quote)
                {
                    // Escaped quote
                    result.Append(quote);
                    pos += 2;
                    continue;
                }

                // End of field
                _position = pos + 1;
                if (_position < _recordData.Length && _recordData[_position] == _options.Delimiter)
                    _position++;

                break;
            }

            result.Append(_recordData[pos]);
            pos++;
        }

        // This allocates, but only for fields with escaped quotes
        var resultStr = result.ToString();
        return resultStr.AsSpan();
    }
}
