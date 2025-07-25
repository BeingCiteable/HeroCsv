using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Ultra-fast zero-allocation enumerable for CSV rows
/// </summary>
public readonly struct UltraFastRowEnumerable
{
    private readonly ICsvReader _reader;
    
    internal UltraFastRowEnumerable(ICsvReader reader)
    {
        _reader = reader;
    }
    
    /// <summary>
    /// Gets the enumerator
    /// </summary>
    public UltraFastRowEnumerator GetEnumerator() => new UltraFastRowEnumerator(_reader);
}

/// <summary>
/// Ultra-fast zero-allocation enumerator for CSV rows
/// </summary>
public ref struct UltraFastRowEnumerator
{
    private readonly IInternalCsvReader _reader;
    private readonly CsvOptions _options;
    private readonly ReadOnlySpan<char> _buffer;
    private int _position;
    private int _lineStart;
    private int _lineLength;
    
    internal UltraFastRowEnumerator(ICsvReader reader)
    {
        if (reader is not IInternalCsvReader internalReader)
        {
            throw new InvalidOperationException("Reader must implement IInternalCsvReader");
        }
        
        _reader = internalReader;
        _options = reader.Options;
        _buffer = internalReader.GetBuffer();
        _position = 0;
        _lineStart = 0;
        _lineLength = 0;
        
        // Skip header if needed
        if (_options.HasHeader && _buffer.Length > 0)
        {
            SkipLine();
        }
    }
    
    /// <summary>
    /// Gets the current row
    /// </summary>
    public UltraFastCsvRow Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new UltraFastCsvRow(_buffer.Slice(_lineStart, _lineLength), _options);
    }
    
    /// <summary>
    /// Moves to the next row
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (_position >= _buffer.Length)
            return false;
        
        _lineStart = _position;
        
        // Find end of line
        var remaining = _buffer.Slice(_position);
        var newlinePos = remaining.IndexOfAny('\r', '\n');
        
        if (newlinePos < 0)
        {
            // Last line without newline
            _lineLength = remaining.Length;
            _position = _buffer.Length;
        }
        else
        {
            _lineLength = newlinePos;
            _position += newlinePos;
            
            // Skip newline characters
            if (_position < _buffer.Length)
            {
                if (_buffer[_position] == '\r')
                {
                    _position++;
                    if (_position < _buffer.Length && _buffer[_position] == '\n')
                    {
                        _position++;
                    }
                }
                else if (_buffer[_position] == '\n')
                {
                    _position++;
                }
            }
        }
        
        // Skip empty lines
        if (_lineLength == 0 && _position < _buffer.Length)
        {
            return MoveNext();
        }
        
        return _lineLength > 0 || _lineStart < _buffer.Length;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipLine()
    {
        while (_position < _buffer.Length && _buffer[_position] != '\n' && _buffer[_position] != '\r')
        {
            _position++;
        }
        
        // Skip newline characters
        if (_position < _buffer.Length)
        {
            if (_buffer[_position] == '\r')
            {
                _position++;
                if (_position < _buffer.Length && _buffer[_position] == '\n')
                {
                    _position++;
                }
            }
            else if (_buffer[_position] == '\n')
            {
                _position++;
            }
        }
    }
}