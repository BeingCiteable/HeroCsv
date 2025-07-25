using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace FastCsv;

/// <summary>
/// Highly optimized Sep-style parser targeting the final 37% performance gap
/// </summary>
public static class OptimizedSepParser
{
    /// <summary>
    /// Ultra-fast field positions storage using unsafe fixed arrays
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UltraFastPositions
    {
        private const int MaxFields = 32;
        private fixed int _starts[MaxFields];
        private fixed int _lengths[MaxFields];
        private int _count;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddField(int start, int length)
        {
            if (_count < MaxFields)
            {
                _starts[_count] = start;
                _lengths[_count] = length;
            }
            _count++;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int start, int length) GetField(int index)
        {
            if ((uint)index >= (uint)_count)
                return (0, 0);
            
            return (_starts[index], _lengths[index]);
        }
        
        public int Count => _count;
    }
    
    /// <summary>
    /// Parse CSV line with maximum optimization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe UltraFastRow ParseLine(ReadOnlySpan<char> line, char delimiter, char quote)
    {
        var positions = new UltraFastPositions();
        
#if NET8_0_OR_GREATER
        // Use SearchValues for .NET 8+ optimization
        var specialChars = SearchValues.Create([delimiter, quote]);
        int fieldStart = 0;
        int position = 0;
        bool inQuotes = false;
        
        while (position < line.Length)
        {
            var remaining = line.Slice(position);
            var nextIndex = remaining.IndexOfAny(specialChars);
            
            if (nextIndex < 0)
                break;
            
            var actualPos = position + nextIndex;
            var ch = line[actualPos];
            
            if (ch == quote)
            {
                inQuotes = !inQuotes;
            }
            else if (ch == delimiter && !inQuotes)
            {
                positions.AddField(fieldStart, actualPos - fieldStart);
                fieldStart = actualPos + 1;
            }
            
            position = actualPos + 1;
        }
        
        // Add final field
        positions.AddField(fieldStart, line.Length - fieldStart);
#else
        // Fallback for older .NET versions
        int fieldStart = 0;
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == quote)
            {
                inQuotes = !inQuotes;
            }
            else if (ch == delimiter && !inQuotes)
            {
                positions.AddField(fieldStart, i - fieldStart);
                fieldStart = i + 1;
            }
        }
        
        positions.AddField(fieldStart, line.Length - fieldStart);
#endif
        
        return new UltraFastRow(line, positions, quote);
    }
    
    /// <summary>
    /// Ultra-optimized row structure
    /// </summary>
    public readonly ref struct UltraFastRow
    {
        private readonly ReadOnlySpan<char> _line;
        private readonly UltraFastPositions _positions;
        private readonly char _quote;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UltraFastRow(ReadOnlySpan<char> line, UltraFastPositions positions, char quote)
        {
            _line = line;
            _positions = positions;
            _quote = quote;
        }
        
        public int FieldCount => _positions.Count;
        
        public ReadOnlySpan<char> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var (start, length) = _positions.GetField(index);
                if (length == 0) return ReadOnlySpan<char>.Empty;
                
                var field = _line.Slice(start, length);
                
                // Fast quote trimming
                if (length >= 2 && field[0] == _quote && field[length - 1] == _quote)
                {
                    return field.Slice(1, length - 2);
                }
                
                return field;
            }
        }
    }
}