using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Numerics;
#endif

namespace FastCsv;

/// <summary>
/// Ultra-fast CSV row with pre-parsed field positions for maximum performance
/// </summary>
public ref struct UltraFastCsvRow
{
    private readonly ReadOnlySpan<char> _line;
    private readonly CsvOptions _options;
    private FieldPositions _positions;
    
    internal UltraFastCsvRow(ReadOnlySpan<char> line, CsvOptions options)
    {
        _line = line;
        _options = options;
        _positions = default;
        _positions.Parse(line, options.Delimiter, options.Quote);
    }
    
    /// <summary>
    /// Gets the number of fields in this row
    /// </summary>
    public int FieldCount => _positions.Count;
    
    /// <summary>
    /// Gets a field value as a span (zero allocation)
    /// </summary>
    public ReadOnlySpan<char> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_positions.Count)
                ThrowIndexOutOfRange(index);
            
            var (start, length) = _positions.GetPosition(index);
            var field = _line.Slice(start, length);
            
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
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowIndexOutOfRange(int index)
    {
        throw new IndexOutOfRangeException($"Field index {index} is out of range. Row has {_positions.Count} fields.");
    }
    
    /// <summary>
    /// Pre-parsed field positions for ultra-fast access
    /// </summary>
    private ref struct FieldPositions
    {
        private const int MaxStackFields = 32;
        private Span<int> _positions;
        private int _count;
        
        public int Count => _count;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int start, int length) GetPosition(int index)
        {
            var i = index * 2;
            return (_positions[i], _positions[i + 1]);
        }
        
        public unsafe void Parse(ReadOnlySpan<char> line, char delimiter, char quote)
        {
            if (line.IsEmpty)
            {
                _count = 0;
                return;
            }
            
            // Stack allocation for common case
            var stackBuffer = stackalloc int[MaxStackFields * 2];
            _positions = new Span<int>(stackBuffer, MaxStackFields * 2);
            
            // Fast path for lines without quotes
            if (line.IndexOf(quote) < 0)
            {
                ParseUnquoted(line, delimiter);
            }
            else
            {
                ParseQuoted(line, delimiter, quote);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseUnquoted(ReadOnlySpan<char> line, char delimiter)
        {
            int fieldStart = 0;
            int fieldIndex = 0;
            
            // SIMD-optimized delimiter search for .NET 8+
#if NET8_0_OR_GREATER
            if (System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated)
            {
                ParseUnquotedSimd(line, delimiter);
                return;
            }
#endif
            
            // Scalar fallback
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == delimiter)
                {
                    if (fieldIndex < MaxStackFields)
                    {
                        _positions[fieldIndex * 2] = fieldStart;
                        _positions[fieldIndex * 2 + 1] = i - fieldStart;
                    }
                    fieldIndex++;
                    fieldStart = i + 1;
                }
            }
            
            // Last field
            if (fieldIndex < MaxStackFields)
            {
                _positions[fieldIndex * 2] = fieldStart;
                _positions[fieldIndex * 2 + 1] = line.Length - fieldStart;
            }
            
            _count = fieldIndex + 1;
        }
        
#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseUnquotedSimd(ReadOnlySpan<char> line, char delimiter)
        {
            // For now, fall back to scalar implementation
            // SIMD implementation needs more work to handle the Vector128 API correctly
            ParseUnquotedScalar(line, delimiter);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseUnquotedScalar(ReadOnlySpan<char> line, char delimiter)
        {
            int fieldStart = 0;
            int fieldIndex = 0;
            
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == delimiter)
                {
                    if (fieldIndex < MaxStackFields)
                    {
                        _positions[fieldIndex * 2] = fieldStart;
                        _positions[fieldIndex * 2 + 1] = i - fieldStart;
                    }
                    fieldIndex++;
                    fieldStart = i + 1;
                }
            }
            
            // Last field
            if (fieldIndex < MaxStackFields)
            {
                _positions[fieldIndex * 2] = fieldStart;
                _positions[fieldIndex * 2 + 1] = line.Length - fieldStart;
            }
            
            _count = fieldIndex + 1;
        }
#endif
        
        private void ParseQuoted(ReadOnlySpan<char> line, char delimiter, char quote)
        {
            int fieldStart = 0;
            int fieldIndex = 0;
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                
                if (ch == quote)
                {
                    if (i + 1 < line.Length && line[i + 1] == quote)
                    {
                        i++; // Skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    if (fieldIndex < MaxStackFields)
                    {
                        _positions[fieldIndex * 2] = fieldStart;
                        _positions[fieldIndex * 2 + 1] = i - fieldStart;
                    }
                    fieldIndex++;
                    fieldStart = i + 1;
                }
            }
            
            // Last field
            if (fieldIndex < MaxStackFields)
            {
                _positions[fieldIndex * 2] = fieldStart;
                _positions[fieldIndex * 2 + 1] = line.Length - fieldStart;
            }
            
            _count = fieldIndex + 1;
        }
    }
}