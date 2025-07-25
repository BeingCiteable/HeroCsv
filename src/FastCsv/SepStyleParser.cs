using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
#endif

namespace FastCsv;

/// <summary>
/// Sep-style CSV parser with always-SIMD, never-scalar optimizations
/// </summary>
public static class SepStyleParser
{
    /// <summary>
    /// Parse CSV data using Sep's optimization techniques
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SepStyleEnumerable Parse(ReadOnlySpan<char> data, CsvOptions options)
    {
        return new SepStyleEnumerable(data, options);
    }
    
    public readonly ref struct SepStyleEnumerable
    {
        private readonly ReadOnlySpan<char> _data;
        private readonly CsvOptions _options;
        
        internal SepStyleEnumerable(ReadOnlySpan<char> data, CsvOptions options)
        {
            _data = data;
            _options = options;
        }
        
        public Enumerator GetEnumerator() => new Enumerator(_data, _options);
        
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<char> _data;
            private readonly char _delimiter;
            private readonly char _quote;
            private int _position;
            private SepRow _current;
#if NET8_0_OR_GREATER
            private static readonly SearchValues<char> SpecialChars = SearchValues.Create([',', '"', ';', '\t', '\r', '\n']);
#endif
            
            internal Enumerator(ReadOnlySpan<char> data, CsvOptions options)
            {
                _data = data;
                _delimiter = options.Delimiter;
                _quote = options.Quote;
                _position = 0;
                _current = default;
                
                // Skip header if needed
                if (options.HasHeader && data.Length > 0)
                {
                    SkipLine();
                }
            }
            
            public SepRow Current => _current;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_position >= _data.Length)
                    return false;
                
                var lineStart = _position;
                var lineEnd = FindLineEnd();
                
                if (lineEnd <= lineStart)
                    return false;
                
                var lineSpan = _data.Slice(lineStart, lineEnd - lineStart);
                _current = new SepRow(lineSpan, _delimiter, _quote);
                
                // Move past the line
                _position = lineEnd;
                SkipNewlines();
                
                return true;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FindLineEnd()
            {
                var start = _position;
                
#if NET8_0_OR_GREATER
                // Use SearchValues for hardware-optimized search
                var remaining = _data.Slice(_position);
                var newlineIndex = remaining.IndexOfAny('\r', '\n');
                
                if (newlineIndex >= 0)
                {
                    return _position + newlineIndex;
                }
                else
                {
                    return _data.Length;
                }
#else
                // Fallback for older .NET versions
                while (_position < _data.Length)
                {
                    var ch = _data[_position];
                    if (ch == '\r' || ch == '\n')
                        break;
                    _position++;
                }
                return _position;
#endif
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SkipLine()
            {
                while (_position < _data.Length && _data[_position] != '\r' && _data[_position] != '\n')
                    _position++;
                SkipNewlines();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]  
            private void SkipNewlines()
            {
                while (_position < _data.Length)
                {
                    var ch = _data[_position];
                    if (ch == '\r')
                    {
                        _position++;
                        if (_position < _data.Length && _data[_position] == '\n')
                            _position++;
                        break;
                    }
                    else if (ch == '\n')
                    {
                        _position++;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Sep-style row that processes entire row with SIMD
    /// </summary>
    public readonly ref struct SepRow
    {
        private readonly ReadOnlySpan<char> _line;
        private readonly char _delimiter;
        private readonly char _quote;
        private readonly DelimiterPositions _positions;
        private readonly int _fieldCount;
        
        internal SepRow(ReadOnlySpan<char> line, char delimiter, char quote)
        {
            _line = line;
            _delimiter = delimiter;
            _quote = quote;
            _positions = default;
            
            // Parse all delimiter positions with inline field storage
            var positions = new DelimiterPositions();
            _fieldCount = ParseDelimiterPositions(line, delimiter, quote, ref positions);
            _positions = positions;
        }
        
        public int FieldCount => _fieldCount;
        
        public ReadOnlySpan<char> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_fieldCount)
                    ThrowIndexOutOfRange(index);
                
                var (start, length) = _positions.GetField(index);
                var field = _line.Slice(start, length);
                
                // Handle quotes
                if (field.Length >= 2 && field[0] == _quote && field[field.Length - 1] == _quote)
                {
                    return field.Slice(1, field.Length - 2);
                }
                
                return field;
            }
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange(int index)
        {
            throw new IndexOutOfRangeException($"Field index {index} is out of range");
        }
        
        /// <summary>
        /// Parse all delimiter positions using SIMD (Sep's key optimization)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ParseDelimiterPositions(ReadOnlySpan<char> line, char delimiter, char quote, ref DelimiterPositions positions)
        {
            if (line.IsEmpty)
                return 0;
            
#if NET8_0_OR_GREATER
            // Use SIMD for .NET 8+
            return ParseWithSimd(line, delimiter, quote, ref positions);
#else
            // Scalar fallback
            return ParseScalar(line, delimiter, quote, ref positions);
#endif
        }
        
#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseWithSimd(ReadOnlySpan<char> line, char delimiter, char quote, ref DelimiterPositions positions)
        {
            // Use SearchValues for hardware-optimized character searching (Sep also uses this approach)
            // positions already initialized, no need to clear
            
            int fieldStart = 0;
            int fieldCount = 0;
            bool inQuotes = false;
            
            // Sep-style: Create SearchValues for special characters
            var specialChars = SearchValues.Create([delimiter, quote]);
            int position = 0;
            
            while (position < line.Length)
            {
                var remaining = line.Slice(position);
                var nextSpecialIndex = remaining.IndexOfAny(specialChars);
                
                if (nextSpecialIndex < 0)
                {
                    // No more special characters
                    break;
                }
                
                var actualIndex = position + nextSpecialIndex;
                var ch = line[actualIndex];
                
                if (ch == quote)
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == delimiter && !inQuotes)
                {
                    positions.AddField(fieldStart, actualIndex - fieldStart);
                    fieldCount++;
                    fieldStart = actualIndex + 1;
                }
                
                position = actualIndex + 1;
            }
            
            // Add final field
            positions.AddField(fieldStart, line.Length - fieldStart);
            fieldCount++;
            
            return fieldCount;
        }
#endif
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseScalar(ReadOnlySpan<char> line, char delimiter, char quote, ref DelimiterPositions positions)
        {
            // positions already initialized, no need to clear
            
            int fieldStart = 0;
            int fieldCount = 0;
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
                    fieldCount++;
                    fieldStart = i + 1;
                }
            }
            
            // Add final field
            positions.AddField(fieldStart, line.Length - fieldStart);
            fieldCount++;
            
            return fieldCount;
        }
    }
    
    /// <summary>
    /// Simple inline field storage - focus on correctness over maximum optimization
    /// </summary>
    private struct FieldInfo
    {
        public int Start;
        public int Length;
    }
    
    private struct DelimiterPositions
    {
        private const int MaxFields = 32; // Smaller for simplicity
        private FieldInfo _field0, _field1, _field2, _field3, _field4, _field5, _field6, _field7;
        private FieldInfo _field8, _field9, _field10, _field11, _field12, _field13, _field14, _field15;
        private FieldInfo _field16, _field17, _field18, _field19, _field20, _field21, _field22, _field23;
        private FieldInfo _field24, _field25, _field26, _field27, _field28, _field29, _field30, _field31;
        private int _count;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddField(int start, int length)
        {
            if (_count < MaxFields)
            {
                SetField(_count, start, length);
            }
            _count++;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetField(int index, int start, int length)
        {
            switch (index)
            {
                case 0: _field0 = new FieldInfo { Start = start, Length = length }; break;
                case 1: _field1 = new FieldInfo { Start = start, Length = length }; break;
                case 2: _field2 = new FieldInfo { Start = start, Length = length }; break;
                case 3: _field3 = new FieldInfo { Start = start, Length = length }; break;
                case 4: _field4 = new FieldInfo { Start = start, Length = length }; break;
                case 5: _field5 = new FieldInfo { Start = start, Length = length }; break;
                case 6: _field6 = new FieldInfo { Start = start, Length = length }; break;
                case 7: _field7 = new FieldInfo { Start = start, Length = length }; break;
                case 8: _field8 = new FieldInfo { Start = start, Length = length }; break;
                case 9: _field9 = new FieldInfo { Start = start, Length = length }; break;
                case 10: _field10 = new FieldInfo { Start = start, Length = length }; break;
                case 11: _field11 = new FieldInfo { Start = start, Length = length }; break;
                case 12: _field12 = new FieldInfo { Start = start, Length = length }; break;
                case 13: _field13 = new FieldInfo { Start = start, Length = length }; break;
                case 14: _field14 = new FieldInfo { Start = start, Length = length }; break;
                case 15: _field15 = new FieldInfo { Start = start, Length = length }; break;
                case 16: _field16 = new FieldInfo { Start = start, Length = length }; break;
                case 17: _field17 = new FieldInfo { Start = start, Length = length }; break;
                case 18: _field18 = new FieldInfo { Start = start, Length = length }; break;
                case 19: _field19 = new FieldInfo { Start = start, Length = length }; break;
                case 20: _field20 = new FieldInfo { Start = start, Length = length }; break;
                case 21: _field21 = new FieldInfo { Start = start, Length = length }; break;
                case 22: _field22 = new FieldInfo { Start = start, Length = length }; break;
                case 23: _field23 = new FieldInfo { Start = start, Length = length }; break;
                case 24: _field24 = new FieldInfo { Start = start, Length = length }; break;
                case 25: _field25 = new FieldInfo { Start = start, Length = length }; break;
                case 26: _field26 = new FieldInfo { Start = start, Length = length }; break;
                case 27: _field27 = new FieldInfo { Start = start, Length = length }; break;
                case 28: _field28 = new FieldInfo { Start = start, Length = length }; break;
                case 29: _field29 = new FieldInfo { Start = start, Length = length }; break;
                case 30: _field30 = new FieldInfo { Start = start, Length = length }; break;
                case 31: _field31 = new FieldInfo { Start = start, Length = length }; break;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int start, int length) GetField(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                return (0, 0);
            }
            
            var field = GetFieldValue(index);
            return (field.Start, field.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FieldInfo GetFieldValue(int index)
        {
            return index switch
            {
                0 => _field0, 1 => _field1, 2 => _field2, 3 => _field3,
                4 => _field4, 5 => _field5, 6 => _field6, 7 => _field7,
                8 => _field8, 9 => _field9, 10 => _field10, 11 => _field11,
                12 => _field12, 13 => _field13, 14 => _field14, 15 => _field15,
                16 => _field16, 17 => _field17, 18 => _field18, 19 => _field19,
                20 => _field20, 21 => _field21, 22 => _field22, 23 => _field23,
                24 => _field24, 25 => _field25, 26 => _field26, 27 => _field27,
                28 => _field28, 29 => _field29, 30 => _field30, 31 => _field31,
                _ => default
            };
        }
        
        public int Count => _count;
    }
}