using System.Runtime.CompilerServices;

namespace HeroCsv.Parsing;

/// <summary>
/// Enumerates CSV fields from a line without allocating memory
/// </summary>
public ref struct CsvFieldEnumerator
{
    private readonly ReadOnlySpan<char> _line;
    private readonly char _delimiter;
    private readonly char _quote;
    private int _position;
    private int _fieldIndex;

    internal CsvFieldEnumerator(ReadOnlySpan<char> line, char delimiter, char quote)
    {
        _line = line;
        _delimiter = delimiter;
        _quote = quote;
        _position = 0;
        _fieldIndex = 0;
    }

    /// <summary>
    /// Gets the next field without allocation
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNextField(out ReadOnlySpan<char> field)
    {
        if (_position >= _line.Length)
        {
            field = default;
            return false;
        }

        // Fast path for unquoted fields
        if (_position < _line.Length && _line[_position] != _quote)
        {
            var start = _position;
            while (_position < _line.Length && _line[_position] != _delimiter)
            {
                _position++;
            }

            field = _line.Slice(start, _position - start);
            _position++; // Skip delimiter
            _fieldIndex++;
            return true;
        }

        // Quoted field - rare path
        return TryGetQuotedField(out field);
    }

    private bool TryGetQuotedField(out ReadOnlySpan<char> field)
    {
        var start = _position;
        _position++; // Skip opening quote

        while (_position < _line.Length)
        {
            if (_line[_position] == _quote)
            {
                if (_position + 1 < _line.Length && _line[_position + 1] == _quote)
                {
                    // Escaped quote
                    _position += 2;
                }
                else
                {
                    // End of quoted field - include the quotes
                    field = _line.Slice(start, _position - start + 1);
                    _position++; // Skip closing quote

                    // Skip to next delimiter
                    while (_position < _line.Length && _line[_position] != _delimiter)
                    {
                        _position++;
                    }
                    _position++; // Skip delimiter
                    _fieldIndex++;
                    return true;
                }
            }
            else
            {
                _position++;
            }
        }

        // Unterminated quoted field - return as is
        field = _line.Slice(start);
        _fieldIndex++;
        return true;
    }

    /// <summary>
    /// Gets field by index (slower - requires parsing from start)
    /// </summary>
    public readonly ReadOnlySpan<char> GetFieldByIndex(int index)
    {
        var enumerator = new CsvFieldEnumerator(_line, _delimiter, _quote);
        ReadOnlySpan<char> field = default;

        for (int i = 0; i <= index; i++)
        {
            if (!enumerator.TryGetNextField(out field))
            {
                return default;
            }
        }

        return field;
    }

    /// <summary>
    /// Count total fields
    /// </summary>
    public readonly int CountTotalFields()
    {
        var count = 0;
        var enumerator = new CsvFieldEnumerator(_line, _delimiter, _quote);

        while (enumerator.TryGetNextField(out _))
        {
            count++;
        }

        return count;
    }
}