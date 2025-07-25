using System.Runtime.CompilerServices;
using System.Text;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace FastCsv;

/// <summary>
/// Unified CSV parser with automatic optimizations
/// </summary>
public static class CsvParser
{
    /// <summary>
    /// Finds the end of the current line starting from the given position
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEnd(ReadOnlySpan<char> content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    /// <summary>
    /// Skips line ending characters and returns the new position
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SkipLineEnding(ReadOnlySpan<char> content, int position)
    {
        if (position >= content.Length) return position;

        if (content[position] == '\r')
        {
            position++;
            if (position < content.Length && content[position] == '\n')
                position++;
        }
        else if (content[position] == '\n')
        {
            position++;
        }

        return position;
    }

    /// <summary>
    /// Ultra-fast line counting with SIMD optimization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountLines(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return 0;

#if NET8_0_OR_GREATER
        // Use SearchValues for hardware-accelerated newline counting
        var newlineSearchValues = System.Buffers.SearchValues.Create(['\r', '\n']);
        var count = 0;
        var pos = 0;
        
        while (pos < content.Length)
        {
            var remaining = content.Slice(pos);
            var newlineIndex = remaining.IndexOfAny(newlineSearchValues);
            
            if (newlineIndex < 0) break;
            
            pos += newlineIndex;
            var ch = content[pos];
            
            if (ch == '\n')
            {
                count++;
                pos++;
            }
            else if (ch == '\r')
            {
                count++;
                pos++;
                // Skip \n if it follows \r (handle \r\n as single line break)
                if (pos < content.Length && content[pos] == '\n')
                    pos++;
            }
        }
        return count;
#else
        // Fallback for older .NET versions
        var count = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n') count++;
        }
        return count;
#endif
    }

    /// <summary>
    /// Main CSV parsing entry point - automatically optimized
    /// </summary>
    public static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast path: comma-delimited with no quotes (90% of use cases)
        if (options.Delimiter == ',' && !options.TrimWhitespace && line.IndexOf('"') < 0)
        {
            return ParseSimpleCommaLine(line);
        }

        // Smart path: check for quotes first, then choose best algorithm
        bool hasQuotes = line.IndexOf(options.Quote) >= 0;

        return hasQuotes
            ? ParseQuotedLine(line, options)
            : ParseUnquotedLine(line, options);
    }

    /// <summary>
    /// Ultra-fast parsing for simple comma-separated lines (no quotes, no trimming)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string[] ParseSimpleCommaLine(ReadOnlySpan<char> line)
    {
        // Count commas in single pass
        var commaCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',') commaCount++;
        }

        var fields = new string[commaCount + 1];
        var fieldIndex = 0;
        var start = 0;

        // Parse fields in single pass
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',')
            {
                fields[fieldIndex++] = CreateString(line.Slice(start, i - start));
                start = i + 1;
            }
        }

        // Final field
        fields[fieldIndex] = CreateString(line.Slice(start));
        return fields;
    }
    
    /// <summary>
    /// Creates string without unnecessary allocations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string CreateString(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return string.Empty;
        if (span.Length == 1) return span[0].ToString();
        
        fixed (char* ptr = span)
        {
            return new string(ptr, 0, span.Length);
        }
    }

    /// <summary>
    /// Optimized parsing for unquoted lines
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[] ParseUnquotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Count delimiters to allocate exact array size
        var delimiterCount = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter) delimiterCount++;
        }

        var fields = new string[delimiterCount + 1];
        var fieldIndex = 0;
        var fieldStart = 0;

        // Parse fields
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var field = line.Slice(fieldStart, i - fieldStart);
                if (options.TrimWhitespace) field = field.Trim();
                fields[fieldIndex++] = CreateString(field);
                fieldStart = i + 1;
            }
        }

        // Final field
        if (fieldStart <= line.Length)
        {
            var field = line.Slice(fieldStart);
            if (options.TrimWhitespace) field = field.Trim();
            fields[fieldIndex] = CreateString(field);
        }

        return fields;
    }

    /// <summary>
    /// Handles parsing lines with quoted fields
    /// </summary>
    private static string[] ParseQuotedLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        var fieldList = new List<string>(8);
        var inQuotes = false;
        var fieldBuilder = new StringBuilder();
        var i = 0;

        while (i < line.Length)
        {
            var ch = line[i];

            if (ch == options.Quote)
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    fieldBuilder.Clear();
                }
                else if (i + 1 < line.Length && line[i + 1] == options.Quote)
                {
                    // Escaped quote
                    fieldBuilder.Append(options.Quote);
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = false;
                }
            }
            else if (ch == options.Delimiter && !inQuotes)
            {
                var field = fieldBuilder.ToString();
                fieldList.Add(options.TrimWhitespace ? field.Trim() : field);
                fieldBuilder.Clear();
            }
            else
            {
                fieldBuilder.Append(ch);
            }

            i++;
        }

        // Add final field
        var finalField = fieldBuilder.ToString();
        fieldList.Add(options.TrimWhitespace ? finalField.Trim() : finalField);

        return [.. fieldList];
    }

    /// <summary>
    /// Parse entire CSV buffer and return zero-allocation row enumerator
    /// </summary>
    public static WholeBufferCsvEnumerable ParseWholeBuffer(ReadOnlySpan<char> data, CsvOptions options)
    {
        return new WholeBufferCsvEnumerable(data, options);
    }
    
    public readonly ref struct WholeBufferCsvEnumerable
    {
        private readonly ReadOnlySpan<char> _data;
        private readonly CsvOptions _options;
        
        internal WholeBufferCsvEnumerable(ReadOnlySpan<char> data, CsvOptions options)
        {
            _data = data;
            _options = options;
        }
        
        public Enumerator GetEnumerator() => new Enumerator(_data, _options);
        
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<char> _data;
            private readonly CsvOptions _options;
            private int _position;
            private CsvRow _current;
            
#if NET8_0_OR_GREATER
            private readonly SearchValues<char> _newlineSearchValues;
            private int[] _lineBreaks;
            private int _lineBreakCount;
            private int _currentLine;
#else
            private int[] _lineBreaks = null!;
            private int _lineBreakCount;
            private int _currentLine;
#endif
            
            internal Enumerator(ReadOnlySpan<char> data, CsvOptions options)
            {
                _data = data;
                _options = options;
                _position = 0;
                _current = default;
                
#if NET8_0_OR_GREATER
                _newlineSearchValues = SearchValues.Create(['\r', '\n']);
                
                // Pre-scan all line breaks for fast navigation
                var estimatedLines = Math.Min(1024, data.Length / 10);
                _lineBreaks = new int[estimatedLines];
                _lineBreakCount = 0;
                _currentLine = 0;
                
                ScanAllLineBreaks(data);
#else
                _lineBreaks = [];
                _lineBreakCount = 0;
                _currentLine = 0;
#endif
                
                // Skip header row if present
                if (options.HasHeader && data.Length > 0)
                {
                    SkipCurrentLine();
#if NET8_0_OR_GREATER
                    // Advance to next line in pre-computed breaks
                    if (_lineBreakCount > 0 && _position > _lineBreaks[0])
                    {
                        _currentLine = 1;
                    }
#endif
                }
            }
            
#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ScanAllLineBreaks(ReadOnlySpan<char> data)
            {
                var pos = 0;
                while (pos < data.Length && _lineBreakCount < _lineBreaks.Length)
                {
                    var remaining = data.Slice(pos);
                    var index = remaining.IndexOfAny(_newlineSearchValues);
                    
                    if (index < 0) break;
                        
                    pos += index;
                    _lineBreaks[_lineBreakCount++] = pos;
                    
                    // Skip newline sequence
                    if (data[pos] == '\r' && pos + 1 < data.Length && data[pos + 1] == '\n')
                        pos += 2;
                    else
                        pos++;
                }
            }
#endif
            
            public CsvRow Current => _current;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_position >= _data.Length) return false;
                
                var lineStart = _position;
                int lineEnd;
                
#if NET8_0_OR_GREATER
                // Use pre-computed line breaks for maximum performance
                if (_currentLine < _lineBreakCount)
                {
                    lineEnd = _lineBreaks[_currentLine];
                    _currentLine++;
                }
                else
                {
                    // Fallback if we exceed pre-computed breaks
                    lineEnd = FindNextLineEndFallback();
                }
#else
                lineEnd = FindNextLineEnd();
#endif
                
                if (lineEnd <= lineStart) return false;
                
                // Create row with pre-computed field positions for fast access
                _current = new CsvRow(_data, lineStart, lineEnd - lineStart, _options);
                
                // Move to start of next line
                _position = lineEnd;
                SkipLineEndingCharacters();
                
                return true;
            }
            
#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FindNextLineEndFallback()
            {
                var remaining = _data.Slice(_position);
                var index = remaining.IndexOfAny(_newlineSearchValues);
                return index < 0 ? _data.Length : _position + index;
            }
#endif
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FindNextLineEnd()
            {
#if NET8_0_OR_GREATER
                var remaining = _data.Slice(_position);
                var newlineIndex = remaining.IndexOfAny(_newlineSearchValues);
                return newlineIndex >= 0 ? _position + newlineIndex : _data.Length;
#else
                var pos = _position;
                while (pos < _data.Length)
                {
                    var ch = _data[pos];
                    if (ch == '\r' || ch == '\n') break;
                    pos++;
                }
                return pos;
#endif
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SkipCurrentLine()
            {
                while (_position < _data.Length && _data[_position] != '\r' && _data[_position] != '\n')
                    _position++;
                SkipLineEndingCharacters();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]  
            private void SkipLineEndingCharacters()
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

#if NET8_0_OR_GREATER
    /// <summary>
    /// .NET 8+ vectorized line end finding (automatically used when available)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEndVectorized(ReadOnlySpan<char> content, int start)
    {
        // Use vectorized search if beneficial (large content)
        if (content.Length - start > 100)
        {
            var searchSpan = content.Slice(start);
            var index = searchSpan.IndexOfAny('\n', '\r');
            return index >= 0 ? start + index : content.Length;
        }
        
        // Fall back to simple search for small content
        return FindLineEnd(content, start);
    }
#endif
}