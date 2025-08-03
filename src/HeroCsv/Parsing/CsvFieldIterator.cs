using System.Runtime.CompilerServices;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// Provides high-performance iteration over CSV fields without allocations
/// </summary>
public static class CsvFieldIterator
{
    /// <summary>
    /// Creates an iterator for field-by-field CSV processing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvFieldCollection IterateFields(ReadOnlySpan<char> data, CsvOptions options)
    {
        return new CsvFieldCollection(data, options);
    }

    /// <summary>
    /// Creates an iterator for field-by-field CSV processing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvFieldCollection IterateFields(string data, CsvOptions options)
    {
        return new CsvFieldCollection(data.AsSpan(), options);
    }

    /// <summary>
    /// Collection of CSV fields for enumeration
    /// </summary>
    public readonly ref struct CsvFieldCollection
    {
        private readonly ReadOnlySpan<char> _data;
        private readonly CsvOptions _options;

        internal CsvFieldCollection(ReadOnlySpan<char> data, CsvOptions options)
        {
            _data = data;
            _options = options;
        }

        public CsvFieldReader GetEnumerator() => new CsvFieldReader(_data, _options);
    }

    /// <summary>
    /// Zero-allocation CSV field reader
    /// </summary>
    public ref struct CsvFieldReader
    {
        private readonly ReadOnlySpan<char> _data;
        private readonly char _delimiter;
        private readonly char _quote;
        private readonly bool _hasHeader;
        private readonly bool _trimWhitespace;
        private int _position;
        private int _rowIndex;
        private int _fieldIndex;
        private int _currentFieldStart;
        private int _currentFieldLength;
        private bool _hasCurrentField;

        internal CsvFieldReader(ReadOnlySpan<char> data, CsvOptions options)
        {
            _data = data;
            _delimiter = options.Delimiter;
            _quote = options.Quote;
            _hasHeader = options.HasHeader;
            _trimWhitespace = options.TrimWhitespace;
            _position = 0;
            _rowIndex = -1; // Start at -1 so first row is 0
            _fieldIndex = -1;
            _currentFieldStart = 0;
            _currentFieldLength = 0;
            _hasCurrentField = false;
            _isNewRow = true; // Start with new row

            // Skip header if configured
            if (_hasHeader && _data.Length > 0)
            {
                SkipLine();
                _rowIndex = -1; // Reset after skipping header
            }
        }

        private bool _isNewRow;

        public bool MoveNext()
        {
            if (_position >= _data.Length)
                return false;

            // Handle field index - reset to 0 for new rows, otherwise increment
            if (_isNewRow)
            {
                _fieldIndex = 0;
                _rowIndex++;
                _isNewRow = false;
            }
            else
            {
                _fieldIndex++;
            }

            // Parse the next field
            _currentFieldStart = _position;
            var inQuotes = false;

            // Handle quoted field
            if (_position < _data.Length && _data[_position] == _quote)
            {
                inQuotes = true;
                _position++;
                _currentFieldStart++; // Skip opening quote
            }

            // Find field end
            var fieldEnd = _position;
            while (_position < _data.Length)
            {
                var ch = _data[_position];

                if (inQuotes)
                {
                    if (ch == _quote)
                    {
                        if (_position + 1 < _data.Length && _data[_position + 1] == _quote)
                        {
                            // Escaped quote - skip both
                            _position += 2;
                            fieldEnd = _position;
                        }
                        else
                        {
                            // End of quoted field
                            _currentFieldLength = fieldEnd - _currentFieldStart;
                            _position++; // Skip closing quote
                            
                            // Skip delimiter if present
                            if (_position < _data.Length && _data[_position] == _delimiter)
                            {
                                _position++;
                            }
                            else if (_position < _data.Length && (_data[_position] == '\n' || _data[_position] == '\r'))
                            {
                                SkipLineEnding();
                                _isNewRow = true;
                            }
                            
                            _hasCurrentField = true;
                            return true;
                        }
                    }
                    else
                    {
                        _position++;
                        fieldEnd = _position;
                    }
                }
                else
                {
                    if (ch == _delimiter)
                    {
                        _currentFieldLength = _position - _currentFieldStart;
                        _position++; // Skip delimiter
                        _hasCurrentField = true;
                        return true;
                    }
                    else if (ch == '\n' || ch == '\r')
                    {
                        _currentFieldLength = _position - _currentFieldStart;
                        _hasCurrentField = true;
                        
                        // Skip line ending but don't increment row yet
                        SkipLineEnding();
                        _isNewRow = true;
                        
                        return true;
                    }
                    else
                    {
                        _position++;
                    }
                }
            }
            
            // Last field in data
            if (_position > _currentFieldStart || (_position == _currentFieldStart && _position == 0))
            {
                _currentFieldLength = _position - _currentFieldStart;
                _hasCurrentField = true;
                return true;
            }
            
            return false;
        }

        public CsvField Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_hasCurrentField)
                    throw new InvalidOperationException("No current field available");
                    
                var fieldSpan = _data.Slice(_currentFieldStart, _currentFieldLength);
                return new CsvField(fieldSpan, _rowIndex, _fieldIndex, _trimWhitespace);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipLine()
        {
            while (_position < _data.Length && _data[_position] != '\n' && _data[_position] != '\r')
            {
                _position++;
            }
            SkipLineEnding();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipLineEnding()
        {
            if (_position < _data.Length)
            {
                if (_data[_position] == '\r')
                {
                    _position++;
                    if (_position < _data.Length && _data[_position] == '\n')
                    {
                        _position++;
                    }
                }
                else if (_data[_position] == '\n')
                {
                    _position++;
                }
            }
        }
    }

    /// <summary>
    /// Represents a single CSV field with its position information
    /// </summary>
    public readonly ref struct CsvField
    {
        private readonly ReadOnlySpan<char> _value;
        private readonly bool _trimWhitespace;

        public readonly int RowIndex;
        public readonly int FieldIndex;

        internal CsvField(ReadOnlySpan<char> value, int rowIndex, int fieldIndex, bool trimWhitespace)
        {
            _value = value;
            RowIndex = rowIndex;
            FieldIndex = fieldIndex;
            _trimWhitespace = trimWhitespace;
        }

        public ReadOnlySpan<char> Value 
        {
            get
            {
                var result = _value;
                if (_trimWhitespace)
                    result = result.Trim();
                    
                // CsvFieldIterator returns raw content - no escaped quote conversion
                return result;
            }
        }

        /// <summary>
        /// Indicates if this field is the first in a new row
        /// </summary>
        public bool IsFirstFieldInRow => FieldIndex == 0;
    }
}