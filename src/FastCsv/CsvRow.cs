#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System.Collections.Generic;

namespace FastCsv;

/// <summary>
/// Zero-allocation CSV row with lazy field position caching for optimal performance
/// </summary>
public ref struct CsvRow
{
    private readonly ReadOnlySpan<char> _buffer;
    private readonly int _lineStart;
    private readonly int _lineLength;
    private readonly CsvOptions _options;
    private int[] _fieldPositions;
    private int _fieldCount;
    private bool _positionsComputed;
    
    internal CsvRow(ReadOnlySpan<char> buffer, int lineStart, int lineLength, CsvOptions options)
    {
        _buffer = buffer;
        _lineStart = lineStart;
        _lineLength = lineLength;
        _options = options;
        _fieldPositions = null!;
        _fieldCount = -1;
        _positionsComputed = false;
    }
    
#if NET8_0_OR_GREATER
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void PrecomputeFieldPositions()
    {
        var line = Line;
        var delimiter = _options.Delimiter;
        var quote = _options.Quote;
        
        // Pre-allocate for typical CSV files
        var positions = new List<int>(32);
        var pos = 0;
        
        while (pos < line.Length)
        {
            // Use SearchValues for fast delimiter search
            var remaining = line.Slice(pos);
            var nextDelim = remaining.IndexOfAny(delimiter, quote);
            
            if (nextDelim < 0)
                break;
                
            if (remaining[nextDelim] == delimiter)
            {
                // Found delimiter - mark position
                positions.Add(pos + nextDelim);
                pos += nextDelim + 1;
            }
            else if (remaining[nextDelim] == quote)
            {
                // Skip quoted section
                pos += nextDelim + 1;
                while (pos < line.Length)
                {
                    var quotePos = line.Slice(pos).IndexOf(quote);
                    if (quotePos < 0) break;
                    
                    pos += quotePos + 1;
                    if (pos >= line.Length || line[pos] != quote)
                        break;
                    pos++; // Skip escaped quote
                }
            }
        }
        
        _fieldPositions = positions.ToArray();
        _fieldCount = positions.Count + 1; // Number of fields is delimiters + 1
    }
#endif
    
    /// <summary>
    /// Gets the number of fields in this row
    /// </summary>
    public int FieldCount
    {
        get
        {
            if (!_positionsComputed)
            {
                ComputeFieldPositions();
            }
            return _fieldCount;
        }
    }
    
    /// <summary>
    /// Gets a field value as a span (zero allocation)
    /// </summary>
    public ReadOnlySpan<char> this[int index]
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        get
        {
            // Compute field positions on first access for efficient repeated access
            if (!_positionsComputed)
            {
                ComputeFieldPositions();
            }
            
            if (index < 0 || index >= _fieldCount)
            {
                ThrowIndexOutOfRange(index);
            }
            
            // Calculate field boundaries from cached positions
            var start = index == 0 ? 0 : _fieldPositions[index - 1] + 1;
            var end = index == _fieldCount - 1 ? Line.Length : _fieldPositions[index];
            
            var field = Line.Slice(start, end - start);
            
            // Apply trimming if needed
            if (_options.TrimWhitespace && !field.IsEmpty)
            {
                field = field.Trim();
            }
            
            return field;
        }
    }
    
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void ComputeFieldPositions()
    {
        if (_positionsComputed) return;
        
        var line = Line;
        var positions = new List<int>(16); // Pre-allocate for typical CSV
        var inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            
            if (ch == _options.Quote)
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == _options.Quote)
                {
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == _options.Delimiter && !inQuotes)
            {
                positions.Add(i);
            }
        }
        
        _fieldPositions = positions.ToArray();
        _fieldCount = positions.Count + 1;
        _positionsComputed = true;
    }
    
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void ThrowIndexOutOfRange(int index)
    {
        throw new IndexOutOfRangeException($"Field index {index} is out of range. Row has {_fieldCount} fields.");
    }
    
    /// <summary>
    /// Gets the raw line
    /// </summary>
    public ReadOnlySpan<char> Line => _buffer.Slice(_lineStart, _lineLength);
    
    /// <summary>
    /// Gets a field value as a string (allocates)
    /// </summary>
    public string GetString(int index)
    {
        var span = this[index];
        if (_options.StringPool != null)
        {
            return _options.StringPool.GetString(span);
        }
        return span.ToString();
    }
    
    /// <summary>
    /// Converts all fields to a string array (allocates)
    /// </summary>
    public string[] ToArray()
    {
        var fields = new System.Collections.Generic.List<string>();
        var enumerator = new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
        
        while (enumerator.TryGetNextField(out var field))
        {
            if (_options.TrimWhitespace)
            {
                field = field.Trim();
            }
            
            if (_options.StringPool != null)
            {
                fields.Add(_options.StringPool.GetString(field));
            }
            else
            {
                fields.Add(field.ToString());
            }
        }
        
        return fields.ToArray();
    }
    
    /// <summary>
    /// Creates an enumerator to iterate through all fields in the row
    /// </summary>
    public CsvFieldEnumerator GetFieldEnumerator() => new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
}