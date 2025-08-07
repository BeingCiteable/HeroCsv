using System.Runtime.CompilerServices;
using System.Text;
using HeroCsv.Core;
using HeroCsv.Models;
using HeroCsv.Utilities;
#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
#endif

namespace HeroCsv.Parsing;

/// <summary>
/// Unified CSV parser with automatic optimizations and strategy pattern support
/// </summary>
public static class CsvParser
{
        private static readonly ParsingStrategySelector DefaultStrategySelector = new();
        private static readonly StringBuilderPool DefaultStringBuilderPool = new();
        /// <summary>
        /// Finds the next line ending from the given position
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
        /// Advances position past line ending characters
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
        /// Counts lines using hardware acceleration when available
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountLines(ReadOnlySpan<char> content)
        {
            if (content.IsEmpty) return 0;

#if NET8_0_OR_GREATER
            var newlineSearchValues = System.Buffers.SearchValues.Create(['\r', '\n']);
            var count = 0;
            var position = 0;
        
            while (position < content.Length)
            {
                var remaining = content.Slice(position);
                var newlineIndex = remaining.IndexOfAny(newlineSearchValues);
            
                if (newlineIndex < 0) break;
            
                position += newlineIndex;
                var currentChar = content[position];
            
                if (currentChar == '\n')
                {
                    count++;
                    position++;
                }
                else if (currentChar == '\r')
                {
                    count++;
                    position++;
                    if (position < content.Length && content[position] == '\n')
                        position++;
                }
            }
            return count;
#else
            var count = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n') count++;
            }
            return count;
#endif
        }

        /// <summary>
        /// Parses a CSV line into fields using the most appropriate strategy
        /// </summary>
        public static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
        {
            if (line.IsEmpty) return [""];

            // Fast path for simple cases
            if (options.Delimiter == ',' && !options.TrimWhitespace && line.IndexOf('"') < 0)
            {
                return ParseSimpleCommaLine(line, options.StringPool);
            }

            // Use strategy pattern for complex cases
            bool hasQuotes = line.IndexOf(options.Quote) >= 0;

            return hasQuotes
                ? ParseQuotedLine(line, options)
                : ParseUnquotedLine(line, options);
        }
        
        /// <summary>
        /// Parses a CSV line using the strategy pattern for maximum flexibility
        /// </summary>
        public static string[] ParseLineWithStrategy(ReadOnlySpan<char> line, CsvOptions options, ParsingStrategySelector? selector = null)
        {
            selector ??= DefaultStrategySelector;
            return selector.ParseLine(line, options);
        }

        /// <summary>
        /// Optimized parsing for comma-separated lines without quotes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string[] ParseSimpleCommaLine(ReadOnlySpan<char> line, StringPool? stringPool = null)
        {
#if NET8_0_OR_GREATER
            if (Avx2.IsSupported && line.Length >= 32)
            {
                return ParseSimpleCommaLineSIMD(line, stringPool);
            }
#endif

            var commaCount = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',') commaCount++;
            }

            var fields = new string[commaCount + 1];
            var fieldIndex = 0;
            var start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',')
                {
                    fields[fieldIndex++] = CreateString(line.Slice(start, i - start), stringPool);
                    start = i + 1;
                }
            }

            // Final field
            fields[fieldIndex] = CreateString(line.Slice(start), stringPool);
            return fields;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Parses comma-separated values using SIMD operations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe string[] ParseSimpleCommaLineSIMD(ReadOnlySpan<char> line, StringPool? stringPool = null)
        {
            const char COMMA_SEPARATOR = ',';
            const int CHARS_PER_VECTOR = 16;
            const int BYTES_PER_CHAR = 2;
        
            var commaSearchVector = Vector256.Create((ushort)COMMA_SEPARATOR);
            var totalCommaCount = 0;
            var currentPosition = 0;
        
            fixed (char* linePtr = line)
            {
                while (currentPosition + CHARS_PER_VECTOR <= line.Length)
                {
                    var characterVector = Avx2.LoadVector256((ushort*)(linePtr + currentPosition));
                    var commaMatchVector = Avx2.CompareEqual(characterVector, commaSearchVector);
                    var matchBitmask = (uint)Avx2.MoveMask(commaMatchVector.AsByte());
                    totalCommaCount += System.Numerics.BitOperations.PopCount(matchBitmask) / BYTES_PER_CHAR;
                
                    currentPosition += CHARS_PER_VECTOR;
                }
            }
        
            while (currentPosition < line.Length)
            {
                if (line[currentPosition] == COMMA_SEPARATOR) 
                    totalCommaCount++;
                currentPosition++;
            }
        
            var fieldArray = new string[totalCommaCount + 1];
            var currentFieldIndex = 0;
            var fieldStartPosition = 0;
            currentPosition = 0;
        
            fixed (char* linePtr = line)
            {
                while (currentPosition + CHARS_PER_VECTOR <= line.Length)
                {
                    var characterVector = Avx2.LoadVector256((ushort*)(linePtr + currentPosition));
                    var commaMatchVector = Avx2.CompareEqual(characterVector, commaSearchVector);
                    var matchBitmask = (uint)Avx2.MoveMask(commaMatchVector.AsByte());
                
                    if (matchBitmask != 0)
                    {
                        for (int bitPosition = 0; bitPosition < 32; bitPosition += BYTES_PER_CHAR)
                        {
                            if ((matchBitmask & (1u << bitPosition)) != 0)
                            {
                                var commaCharPosition = currentPosition + (bitPosition / BYTES_PER_CHAR);
                            
                                if (commaCharPosition < line.Length && line[commaCharPosition] == COMMA_SEPARATOR)
                                {
                                    fieldArray[currentFieldIndex++] = CreateString(line.Slice(fieldStartPosition, commaCharPosition - fieldStartPosition), stringPool);
                                    fieldStartPosition = commaCharPosition + 1;
                                }
                            }
                        }
                    }
                    currentPosition += CHARS_PER_VECTOR;
                }
            }
        
            while (currentPosition < line.Length)
            {
                if (line[currentPosition] == COMMA_SEPARATOR)
                {
                    fieldArray[currentFieldIndex++] = CreateString(line.Slice(fieldStartPosition, currentPosition - fieldStartPosition), stringPool);
                    fieldStartPosition = currentPosition + 1;
                }
                currentPosition++;
            }
        
            if (currentFieldIndex < fieldArray.Length)
            {
                fieldArray[currentFieldIndex] = CreateString(line.Slice(fieldStartPosition), stringPool);
            }
        
            return fieldArray;
        }
#endif

        /// <summary>
        /// Creates string from span efficiently
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
        /// Creates string with optional pooling for deduplication
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string CreateString(ReadOnlySpan<char> span, StringPool? pool)
        {
            if (pool != null)
            {
                return pool.GetString(span);
            }

            return CreateString(span);
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
                    fields[fieldIndex++] = CreateString(field, options.StringPool);
                    fieldStart = i + 1;
                }
            }

            // Final field
            if (fieldStart <= line.Length)
            {
                var field = line.Slice(fieldStart);
                if (options.TrimWhitespace) field = field.Trim();
                fields[fieldIndex] = CreateString(field, options.StringPool);
            }

            return fields;
        }

        /// <summary>
        /// Handles parsing lines with quoted fields using pooled StringBuilder
        /// </summary>
        private static string[] ParseQuotedLine(ReadOnlySpan<char> line, CsvOptions options)
        {
            var fieldList = new List<string>(8);
            var inQuotes = false;
            var fieldBuilder = DefaultStringBuilderPool.Rent();
            var i = 0;
            var fieldStart = true; // Track if we're at the start of a field
            
            try
            {

            while (i < line.Length)
            {
                var ch = line[i];

                if (ch == options.Quote)
                {
                    if (!inQuotes && fieldStart)
                    {
                        // Only start quoted field if quote is at field start
                        inQuotes = true;
                        fieldStart = false;
                    }
                    else if (inQuotes && i + 1 < line.Length && line[i + 1] == options.Quote)
                    {
                        // Escaped quote within quoted field
                        fieldBuilder.Append(options.Quote);
                        i++; // Skip next quote
                    }
                    else if (inQuotes)
                    {
                        // End of quoted field
                        inQuotes = false;
                    }
                    else
                    {
                        // Quote in middle of unquoted field - treat as literal
                        fieldBuilder.Append(ch);
                        fieldStart = false;
                    }
                }
                else if (ch == options.Delimiter && !inQuotes)
                {
                    var field = fieldBuilder.ToString();
                    fieldList.Add(options.TrimWhitespace ? field.Trim() : field);
                    fieldBuilder.Clear();
                    fieldStart = true; // Next character starts a new field
                }
                else
                {
                    fieldBuilder.Append(ch);
                    fieldStart = false;
                }

                i++;
            }

            // Add final field
            var finalField = fieldBuilder.ToString();
            fieldList.Add(options.TrimWhitespace ? finalField.Trim() : finalField);

            return [.. fieldList];
            }
            finally
            {
                DefaultStringBuilderPool.Return(fieldBuilder);
            }
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
                // These fields are not used in non-NET8 builds
                // Remove them to fix CS0414 warnings
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
                    // Not used in non-NET8 builds
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

                public readonly CsvRow Current => _current;

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

                    // Allow empty lines (lineEnd == lineStart)
                    if (lineEnd < lineStart) return false;

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
                private readonly int FindNextLineEnd()
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