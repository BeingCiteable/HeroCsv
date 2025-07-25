#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace FastCsv;

/// <summary>
/// Zero-allocation CSV row that provides span-based field access
/// </summary>
public ref struct CsvRow
{
    private readonly ReadOnlySpan<char> _buffer;
    private readonly int _lineStart;
    private readonly int _lineLength;
    private readonly CsvOptions _options;
    private Span<int> _fieldPositions;
    private int _fieldCount;
    
    internal CsvRow(ReadOnlySpan<char> buffer, int lineStart, int lineLength, CsvOptions options)
    {
        _buffer = buffer;
        _lineStart = lineStart;
        _lineLength = lineLength;
        _options = options;
        _fieldPositions = default;
        _fieldCount = -1;
    }
    
    /// <summary>
    /// Gets the number of fields in this row
    /// </summary>
    public int FieldCount
    {
        get
        {
            if (_fieldCount < 0)
            {
                // Count fields on demand
                var enumerator = new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
                _fieldCount = enumerator.CountTotalFields();
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
            // Use fast enumerator to get field directly
            var enumerator = new CsvFieldEnumerator(Line, _options.Delimiter, _options.Quote);
            var field = enumerator.GetFieldByIndex(index);
            
            // Apply trimming if needed
            if (_options.TrimWhitespace && !field.IsEmpty)
            {
                field = field.Trim();
            }
            
            return field;
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
    
    private void ParseFieldPositions()
    {
        var lineSpan = Line;
        if (lineSpan.IsEmpty)
        {
            _fieldCount = 0;
            return;
        }
        
        // Fast path for simple CSV
        if (!ContainsQuote(lineSpan, _options.Quote))
        {
            ParseUnquotedPositions();
        }
        else
        {
            ParseQuotedPositions();
        }
    }
    
    private unsafe void ParseUnquotedPositions()
    {
        var lineSpan = Line;
        
        // Allocate on stack for small field counts (most CSV files have < 32 fields)
        const int MaxStackFields = 32;
        var stackBuffer = stackalloc int[MaxStackFields * 2];
        
        // Single pass to find all delimiters and positions
        var fieldIndex = 0;
        var fieldStart = 0;
        
        for (int i = 0; i < lineSpan.Length; i++)
        {
            if (lineSpan[i] == _options.Delimiter)
            {
                if (fieldIndex < MaxStackFields)
                {
                    stackBuffer[fieldIndex * 2] = fieldStart;
                    stackBuffer[fieldIndex * 2 + 1] = i - fieldStart;
                }
                fieldIndex++;
                fieldStart = i + 1;
            }
        }
        
        // Last field
        if (fieldIndex < MaxStackFields)
        {
            stackBuffer[fieldIndex * 2] = fieldStart;
            stackBuffer[fieldIndex * 2 + 1] = lineSpan.Length - fieldStart;
        }
        
        _fieldCount = fieldIndex + 1;
        
        // If we fit in stack buffer, use it
        if (_fieldCount <= MaxStackFields)
        {
            _fieldPositions = new Span<int>(stackBuffer, _fieldCount * 2);
        }
        else
        {
            // Rare case: allocate on heap and parse again
            var heapBuffer = new int[_fieldCount * 2];
            fieldIndex = 0;
            fieldStart = 0;
            
            for (int i = 0; i < lineSpan.Length; i++)
            {
                if (lineSpan[i] == _options.Delimiter)
                {
                    heapBuffer[fieldIndex * 2] = fieldStart;
                    heapBuffer[fieldIndex * 2 + 1] = i - fieldStart;
                    fieldIndex++;
                    fieldStart = i + 1;
                }
            }
            
            heapBuffer[fieldIndex * 2] = fieldStart;
            heapBuffer[fieldIndex * 2 + 1] = lineSpan.Length - fieldStart;
            
            _fieldPositions = heapBuffer;
        }
    }
    
    
    private void ParseQuotedPositions()
    {
        var lineSpan = Line;
        // For quoted fields, we need more complex parsing
        // This is simplified - production would need full quote handling
        var positions = new List<(int start, int length)>();
        var inQuotes = false;
        var fieldStart = 0;
        
        for (int i = 0; i < lineSpan.Length; i++)
        {
            var ch = lineSpan[i];
            
            if (ch == _options.Quote)
            {
                inQuotes = !inQuotes;
            }
            else if (ch == _options.Delimiter && !inQuotes)
            {
                positions.Add((fieldStart, i - fieldStart));
                fieldStart = i + 1;
            }
        }
        
        // Last field
        positions.Add((fieldStart, lineSpan.Length - fieldStart));
        
        _fieldCount = positions.Count;
        var posArray = new int[_fieldCount * 2];
        
        for (int i = 0; i < positions.Count; i++)
        {
            posArray[i * 2] = positions[i].start;
            posArray[i * 2 + 1] = positions[i].length;
        }
        
        _fieldPositions = posArray;
    }
    
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> GetFieldSpan(int index)
    {
        var posIndex = index * 2;
        var start = _fieldPositions[posIndex];
        var length = _fieldPositions[posIndex + 1];
        var field = Line.Slice(start, length);
        
        // Fast path - no trimming or quotes
        if (!_options.TrimWhitespace && (field.Length < 2 || field[0] != _options.Quote))
        {
            return field;
        }
        
        // Handle trimming
        if (_options.TrimWhitespace)
        {
            field = field.Trim();
        }
        
        // Handle quotes
        if (field.Length >= 2 && field[0] == _options.Quote && field[field.Length - 1] == _options.Quote)
        {
            field = field.Slice(1, field.Length - 2);
        }
        
        return field;
    }
    
    private static bool ContainsQuote(ReadOnlySpan<char> line, char quote)
    {
        return line.IndexOf(quote) >= 0;
    }
}