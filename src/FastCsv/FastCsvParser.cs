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
/// FastCsv's optimized CSV parser using SearchValues API and zero-allocation techniques
/// </summary>
public static class FastCsvParser
{
    /// <summary>
    /// Parse CSV data using hardware acceleration and zero-allocation techniques
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BufferBasedCsvEnumerable Parse(ReadOnlySpan<char> data, CsvOptions options)
    {
        return new BufferBasedCsvEnumerable(data, options);
    }
    
    public readonly ref struct BufferBasedCsvEnumerable
    {
        private readonly ReadOnlySpan<char> _data;
        private readonly CsvOptions _options;
        
        internal BufferBasedCsvEnumerable(ReadOnlySpan<char> data, CsvOptions options)
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
            private CsvRow _current;
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
            
            public CsvRow Current => _current;
            
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
                _current = new CsvRow(_data, lineStart, lineEnd - lineStart, new CsvOptions(_delimiter, _quote, false, false, false));
                
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
}