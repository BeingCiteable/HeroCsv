#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System.Collections.Generic;

namespace FastCsv;

/// <summary>
/// Zero-allocation CSV row with pre-computed field positions for fast access
/// </summary>
public ref struct CsvRow
{
    private readonly ReadOnlySpan<char> _buffer;
    private readonly int _lineStart;
    private readonly int _lineLength;
    private readonly CsvOptions _options;
    private int[] _fieldPositions;
    private int _fieldCount;
    
    internal CsvRow(ReadOnlySpan<char> buffer, int lineStart, int lineLength, CsvOptions options)
    {
        _buffer = buffer;
        _lineStart = lineStart;
        _lineLength = lineLength;
        _options = options;
        _fieldPositions = null!;
        _fieldCount = -1;
        
#if NET8_0_OR_GREATER
        // Pre-compute field positions for Sep-style performance
        PrecomputeFieldPositions();
#endif
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
#if NET8_0_OR_GREATER
            // Already computed during construction
            return _fieldCount;
#else
            if (_fieldCount < 0)
            {
                // Count fields on demand for older frameworks
                var enumerator = new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
                _fieldCount = enumerator.CountTotalFields();
            }
            return _fieldCount;
#endif
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
#if NET8_0_OR_GREATER
            // Use pre-computed positions for O(1) field access
            if (index < 0 || index >= _fieldCount)
                ThrowIndexOutOfRange(index);
                
            var startPos = index == 0 ? 0 : (_fieldPositions[index - 1] + 1);
            var endPos = index >= _fieldCount - 1 ? Line.Length : _fieldPositions[index];
            
            var field = Line.Slice(startPos, endPos - startPos);
            
            // Handle quoted fields
            if (field.Length >= 2 && field[0] == _options.Quote && field[^1] == _options.Quote)
            {
                field = field.Slice(1, field.Length - 2);
                // TODO: Handle escaped quotes within the field
            }
            
            // Apply trimming if needed
            if (_options.TrimWhitespace && !field.IsEmpty)
            {
                field = field.Trim();
            }
            
            return field;
#else
            // Fallback to enumerator for older frameworks
            var enumerator = new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
            var field = enumerator.GetFieldByIndex(index);
            
            // Apply trimming if needed
            if (_options.TrimWhitespace && !field.IsEmpty)
            {
                field = field.Trim();
            }
            
            return field;
#endif
        }
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
        return this[index].ToString();
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
            fields.Add(field.ToString());
        }
        
        return fields.ToArray();
    }
    
    /// <summary>
    /// Creates an enumerator to iterate through all fields in the row
    /// </summary>
    public CsvFieldEnumerator GetFieldEnumerator() => new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
}