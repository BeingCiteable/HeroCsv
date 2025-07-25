using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Provides high-performance iteration over CSV fields without allocations
/// </summary>
public static class CsvFieldIterator
{
    /// <summary>
    /// Creates an iterator to efficiently process all fields in CSV data
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvFieldCollection IterateFields(ReadOnlySpan<char> data, CsvOptions options)
    {
        return new CsvFieldCollection(data, options);
    }
    
    /// <summary>
    /// Represents a collection of CSV fields that can be enumerated
    /// </summary>
    public ref struct CsvFieldCollection
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
    /// Reads CSV fields one by one with minimal overhead
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
        
        internal CsvFieldReader(ReadOnlySpan<char> data, CsvOptions options)
        {
            _data = data;
            _delimiter = options.Delimiter;
            _quote = options.Quote;
            _hasHeader = options.HasHeader;
            _trimWhitespace = options.TrimWhitespace;
            _position = 0;
            _rowIndex = -1;
            _fieldIndex = 0;
            
            // Skip header if needed
            if (_hasHeader && _data.Length > 0)
            {
                SkipLine();
            }
        }
        
        public bool MoveNext()
        {
            if (_position >= _data.Length)
                return false;
                
            // New row
            if (_fieldIndex == 0)
            {
                _rowIndex++;
            }
            
            return true;
        }
        
        public CsvField Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var fieldStart = _position;
                var inQuotes = false;
                
                // Handle quoted field
                if (_position < _data.Length && _data[_position] == _quote)
                {
                    inQuotes = true;
                    _position++;
                    fieldStart++;
                }
                
                // Find field end
                while (_position < _data.Length)
                {
                    var ch = _data[_position];
                    
                    if (inQuotes)
                    {
                        if (ch == _quote)
                        {
                            if (_position + 1 < _data.Length && _data[_position + 1] == _quote)
                            {
                                // Escaped quote
                                _position += 2;
                            }
                            else
                            {
                                // End of quoted field
                                var quotedField = _data.Slice(fieldStart, _position - fieldStart);
                                _position++; // Skip closing quote
                                
                                // Skip to delimiter or line end
                                while (_position < _data.Length && _data[_position] != _delimiter && _data[_position] != '\n' && _data[_position] != '\r')
                                {
                                    _position++;
                                }
                                
                                // Skip delimiter
                                if (_position < _data.Length && _data[_position] == _delimiter)
                                {
                                    _position++;
                                    _fieldIndex++;
                                }
                                else if (_position < _data.Length && (_data[_position] == '\n' || _data[_position] == '\r'))
                                {
                                    SkipLineEnding();
                                    _fieldIndex = 0;
                                }
                                else
                                {
                                    _fieldIndex = 0; // End of data
                                }
                                
                                return new CsvField(quotedField, _rowIndex, _fieldIndex - 1, _trimWhitespace);
                            }
                        }
                        else
                        {
                            _position++;
                        }
                    }
                    else
                    {
                        if (ch == _delimiter)
                        {
                            var field = _data.Slice(fieldStart, _position - fieldStart);
                            _position++;
                            _fieldIndex++;
                            return new CsvField(field, _rowIndex, _fieldIndex - 1, _trimWhitespace);
                        }
                        else if (ch == '\n' || ch == '\r')
                        {
                            var field = _data.Slice(fieldStart, _position - fieldStart);
                            SkipLineEnding();
                            var currentFieldIndex = _fieldIndex;
                            _fieldIndex = 0;
                            return new CsvField(field, _rowIndex, currentFieldIndex, _trimWhitespace);
                        }
                        else
                        {
                            _position++;
                        }
                    }
                }
                
                // Last field
                var lastField = _data.Slice(fieldStart, _position - fieldStart);
                var lastFieldIndex = _fieldIndex;
                _fieldIndex = 0;
                return new CsvField(lastField, _rowIndex, lastFieldIndex, _trimWhitespace);
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
        
        public ReadOnlySpan<char> Value => _trimWhitespace ? _value.Trim() : _value;
        
        /// <summary>
        /// Indicates if this field is the first in a new row
        /// </summary>
        public bool IsFirstFieldInRow => FieldIndex == 0;
    }
}