using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FastCsv;

#if NETSTANDARD2_0
using System.ComponentModel;
#endif

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

#if NET7_0_OR_GREATER
using System.Buffers.Text;
#endif

#if NET8_0_OR_GREATER
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Runtime.Intrinsics;
using System.Text.Unicode;
#endif


namespace FastCsv
{
    /// <summary>
    /// Configuration for CSV parsing and writing operations
    /// </summary>
    public readonly struct CsvOptions
    {
        public readonly char Delimiter;
        public readonly char Quote;
        public readonly bool HasHeader;
        public readonly bool TrimWhitespace;
        public readonly string NewLine;

#if NET8_0_OR_GREATER
        // Precomputed SearchValues for ultra-fast character searching
        public readonly SearchValues<char> SpecialChars;
        public readonly SearchValues<char> NewLineChars;
#endif

        public CsvOptions(
            char delimiter = ',',
            char quote = '"',
            bool hasHeader = true,
            bool trimWhitespace = false,
            string newLine = "\r\n")
        {
            Delimiter = delimiter;
            Quote = quote;
            HasHeader = hasHeader;
            TrimWhitespace = trimWhitespace;
            NewLine = newLine ?? Environment.NewLine;

#if NET8_0_OR_GREATER
            // Pre-create SearchValues for maximum performance
            SpecialChars = SearchValues.Create([delimiter, quote, '\r', '\n']);
            NewLineChars = SearchValues.Create(['\r', '\n']);
#endif
        }

        public static CsvOptions Default => new(',', '"', true, false, "\r\n");
    }

    /// <summary>
    /// High-performance, zero-allocation CSV reader using ref struct
    /// </summary>
    public ref struct CsvReader
    {
        private readonly CsvOptions _options;
        private readonly ReadOnlySpan<char> _data;
        private int _position;
        private int _lineNumber;
        private readonly int _dataLength;

        public CsvReader(ReadOnlySpan<char> csvData, CsvOptions options = default)
        {
            _options = options.Equals(default) ? CsvOptions.Default : options;
            _data = csvData;
            _position = 0;
            _lineNumber = 1;
            _dataLength = csvData.Length;
        }

        /// <summary>
        /// Current line number (1-based)
        /// </summary>
        public readonly int LineNumber => _lineNumber;

        /// <summary>
        /// Check if there are more records to read
        /// </summary>
        public readonly bool HasMoreData => _position < _dataLength;

        /// <summary>
        /// Read the next record from the CSV
        /// </summary>
        /// <returns>Enumerator for fields in the current record</returns>
        public CsvRecord ReadRecord()
        {
            if (_position >= _dataLength)
                return new CsvRecord();

            var recordStart = _position;
            var recordEnd = FindRecordEnd();
            var recordSpan = _data.Slice(recordStart, recordEnd - recordStart);

            _position = recordEnd;
            SkipNewLine();
            _lineNumber++;

            return new CsvRecord(recordSpan, _options, _lineNumber - 1);
        }

        /// <summary>
        /// Skip the header row if present
        /// </summary>
        public void SkipHeader()
        {
            if (_options.HasHeader && _position == 0)
            {
                ReadRecord(); // Skip header record
            }
        }

        /// <summary>
        /// Read all records as an enumerable
        /// </summary>
        public readonly CsvReaderEnumerator GetEnumerator() => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRecordEnd()
        {
#if NET8_0_OR_GREATER
            return FindRecordEndOptimized();
#else
            return FindRecordEndFallback();
#endif
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRecordEndOptimized()
        {
            var pos = _position;
            var inQuotes = false;
            var quote = _options.Quote;
            var remaining = _data.Slice(pos);

            while (!remaining.IsEmpty)
            {
                // Use SearchValues for ultra-fast scanning to next special character
                var nextSpecial = remaining.IndexOfAny(_options.NewLineChars);
                if (nextSpecial == -1)
                {
                    // No more newlines found
                    return _dataLength;
                }

                // Check if we're in quotes for the section up to the newline
                var sectionToCheck = remaining.Slice(0, nextSpecial);
                var quoteCount = CountQuotes(sectionToCheck, quote);
                inQuotes = (quoteCount % 2) != 0;

                if (!inQuotes)
                {
                    return pos + nextSpecial;
                }

                // Continue past this newline
                pos += nextSpecial + 1;
                remaining = _data.Slice(pos);
            }

            return _dataLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountQuotes(ReadOnlySpan<char> span, char quote)
        {
            var count = 0;
            var searchValues = SearchValues.Create([quote]);
            var remaining = span;
            
            while (!remaining.IsEmpty)
            {
                var index = remaining.IndexOfAny(searchValues);
                if (index == -1) break;
                
                count++;
                remaining = remaining.Slice(index + 1);
            }
            
            return count;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRecordEndFallback()
        {
            var pos = _position;
            var inQuotes = false;
            var quote = _options.Quote;

            while (pos < _dataLength)
            {
                var ch = _data[pos];

                if (ch == quote)
                {
                    if (inQuotes && pos + 1 < _dataLength && _data[pos + 1] == quote)
                    {
                        // Escaped quote
                        pos += 2;
                        continue;
                    }
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && (ch == '\r' || ch == '\n'))
                {
                    return pos;
                }

                pos++;
            }

            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipNewLine()
        {
            if (_position < _dataLength)
            {
                var ch = _data[_position];
                if (ch == '\r')
                {
                    _position++;
                    if (_position < _dataLength && _data[_position] == '\n')
                        _position++;
                }
                else if (ch == '\n')
                {
                    _position++;
                }
            }
        }
    }

    /// <summary>
    /// Enumerator for CsvReader to support foreach loops
    /// </summary>
    public ref struct CsvReaderEnumerator
    {
        private CsvReader _reader;

        internal CsvReaderEnumerator(CsvReader reader)
        {
            _reader = reader;
        }

        public readonly CsvReaderEnumerator GetEnumerator() => this;

        public bool MoveNext() => _reader.HasMoreData;

        public CsvRecord Current => _reader.ReadRecord();
    }

    /// <summary>
    /// Wrapper for CsvRecord that can be used with IEnumerable
    /// </summary>
    public class CsvRecordWrapper
    {
        private readonly string[] _fields;
        private readonly int _lineNumber;

        internal CsvRecordWrapper(CsvRecord record)
        {
            _fields = record.ToStringArray();
            _lineNumber = record.LineNumber;
        }

        /// <summary>
        /// Line number this record came from
        /// </summary>
        public int LineNumber => _lineNumber;

        /// <summary>
        /// Get field as string by index (0-based)
        /// </summary>
        public string GetField(int index)
        {
            return index >= 0 && index < _fields.Length ? _fields[index] : string.Empty;
        }

        /// <summary>
        /// Get all fields as string array
        /// </summary>
        public string[] ToStringArray() => _fields;

        /// <summary>
        /// Number of fields in this record
        /// </summary>
        public int FieldCount => _fields.Length;

        /// <summary>
        /// Get enumerator for fields
        /// </summary>
        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_fields).GetEnumerator();
    }

    /// <summary>
    /// Represents a single CSV record with field enumeration
    /// </summary>
    public ref struct CsvRecord
    {
        private readonly ReadOnlySpan<char> _recordData;
        private readonly CsvOptions _options;
        private readonly int _lineNumber;

        public CsvRecord()
        {
            _recordData = ReadOnlySpan<char>.Empty;
            _options = CsvOptions.Default;
            _lineNumber = 0;
        }

        internal CsvRecord(ReadOnlySpan<char> recordData, CsvOptions options, int lineNumber)
        {
            _recordData = recordData;
            _options = options;
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// Line number this record came from
        /// </summary>
        public readonly int LineNumber => _lineNumber;

        /// <summary>
        /// Get field enumerator
        /// </summary>
        public readonly CsvFieldEnumerator GetEnumerator() => new(_recordData, _options);

        /// <summary>
        /// Get a specific field by index (0-based)
        /// </summary>
        public readonly ReadOnlySpan<char> GetField(int index)
        {
            var fieldIndex = 0;
            foreach (var field in this)
            {
                if (fieldIndex == index)
                    return field;
                fieldIndex++;
            }
            return ReadOnlySpan<char>.Empty;
        }

        /// <summary>
        /// Convert all fields to a string array (allocates)
        /// </summary>
        public readonly string[] ToStringArray()
        {
            var fields = new List<string>();
            foreach (var field in this)
            {
                fields.Add(field.ToString());
            }
            return fields.ToArray();
        }

        /// <summary>
        /// Count the number of fields in this record
        /// </summary>
        public readonly int FieldCount
        {
            get
            {
                var count = 0;
                foreach (var _ in this)
                    count++;
                return count;
            }
        }
    }

    /// <summary>
    /// Enumerator for fields within a CSV record
    /// </summary>
    public ref struct CsvFieldEnumerator
    {
        private readonly ReadOnlySpan<char> _recordData;
        private readonly CsvOptions _options;
        private int _position;
        private ReadOnlySpan<char> _current;

        internal CsvFieldEnumerator(ReadOnlySpan<char> recordData, CsvOptions options)
        {
            _recordData = recordData;
            _options = options;
            _position = 0;
            _current = ReadOnlySpan<char>.Empty;
        }

        public readonly CsvFieldEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_position >= _recordData.Length)
                return false;

            _current = ReadNextField();
            return true;
        }

        public readonly ReadOnlySpan<char> Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadNextField()
        {
            if (_position >= _recordData.Length)
                return ReadOnlySpan<char>.Empty;

            var start = _position;
            var delimiter = _options.Delimiter;
            var quote = _options.Quote;

            // Check if field is quoted
            if (_recordData[_position] == quote)
            {
                return ReadQuotedField();
            }

#if NET8_0_OR_GREATER
            // Use SearchValues for fast delimiter scanning
            var remaining = _recordData.Slice(_position);
            var delimiterSearch = SearchValues.Create([delimiter]);
            var nextDelimiter = remaining.IndexOfAny(delimiterSearch);
            
            if (nextDelimiter == -1)
            {
                // No more delimiters - this is the last field
                _position = _recordData.Length;
                var field = _recordData.Slice(start);
                return _options.TrimWhitespace ? field.Trim() : field;
            }
            
            _position = start + nextDelimiter + 1; // Move past delimiter
            var resultField = _recordData.Slice(start, nextDelimiter);
            return _options.TrimWhitespace ? resultField.Trim() : resultField;
#else
            // Read unquoted field (fallback)
            while (_position < _recordData.Length && _recordData[_position] != delimiter)
            {
                _position++;
            }

            var field = _recordData.Slice(start, _position - start);

            // Skip delimiter for next field
            if (_position < _recordData.Length && _recordData[_position] == delimiter)
                _position++;

            return _options.TrimWhitespace ? field.Trim() : field;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadQuotedField()
        {
            var quote = _options.Quote;
            var delimiter = _options.Delimiter;

            _position++; // Skip opening quote
            var start = _position;

            while (_position < _recordData.Length)
            {
                if (_recordData[_position] == quote)
                {
                    if (_position + 1 < _recordData.Length && _recordData[_position + 1] == quote)
                    {
                        // Escaped quote - we'll need to process this
                        return ReadQuotedFieldWithEscapes(start);
                    }

                    // End of quoted field
                    var field = _recordData.Slice(start, _position - start);
                    _position++; // Skip closing quote

                    // Skip delimiter
                    if (_position < _recordData.Length && _recordData[_position] == delimiter)
                        _position++;

                    return field;
                }
                _position++;
            }

            // Unterminated quote - return rest of data
            return _recordData.Slice(start);
        }

        private ReadOnlySpan<char> ReadQuotedFieldWithEscapes(int fieldStart)
        {
            // For fields with escaped quotes, we need to build the result
            // This is the only allocation-requiring path
            var result = new StringBuilder();
            var pos = fieldStart;
            var quote = _options.Quote;

            while (pos < _recordData.Length)
            {
                if (_recordData[pos] == quote)
                {
                    if (pos + 1 < _recordData.Length && _recordData[pos + 1] == quote)
                    {
                        // Escaped quote
                        result.Append(quote);
                        pos += 2;
                        continue;
                    }

                    // End of field
                    _position = pos + 1;
                    if (_position < _recordData.Length && _recordData[_position] == _options.Delimiter)
                        _position++;

                    break;
                }

                result.Append(_recordData[pos]);
                pos++;
            }

            // This allocates, but only for fields with escaped quotes
            var resultStr = result.ToString();
            return resultStr.AsSpan();
        }
    }

    /// <summary>
    /// High-performance CSV writer using ref struct and spans
    /// </summary>
    public ref struct CsvWriter
    {
        private readonly CsvOptions _options;
        private readonly IBufferWriter<char> _writer;
        private bool _isFirstField;
        private bool _isFirstRecord;

        public CsvWriter(IBufferWriter<char> writer, CsvOptions options = default)
        {
            _options = options.Equals(default) ? CsvOptions.Default : options;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _isFirstField = true;
            _isFirstRecord = true;
        }

        /// <summary>
        /// Write a field to the current record
        /// </summary>
        public void WriteField(ReadOnlySpan<char> field)
        {
            if (!_isFirstField)
            {
                WriteChar(_options.Delimiter);
            }

            if (NeedsQuoting(field))
            {
                WriteQuotedField(field);
            }
            else
            {
                WriteSpan(field);
            }

            _isFirstField = false;
        }

        /// <summary>
        /// Write a field from a string
        /// </summary>
        public void WriteField(string field)
        {
            WriteField(field.AsSpan());
        }

        /// <summary>
        /// Write multiple fields as a complete record
        /// </summary>
        public void WriteRecord(params string[] fields)
        {
            foreach (var field in fields)
            {
                WriteField(field);
            }
            EndRecord();
        }


        /// <summary>
        /// End the current record and start a new line
        /// </summary>
        public void EndRecord()
        {
            if (!_isFirstRecord)
            {
                WriteSpan(_options.NewLine.AsSpan());
            }
            _isFirstField = true;
            _isFirstRecord = false;
        }

        /// <summary>
        /// Write header fields
        /// </summary>
        public void WriteHeader(params string[] headers)
        {
            WriteRecord(headers);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool NeedsQuoting(ReadOnlySpan<char> field)
        {
            var delimiter = _options.Delimiter;
            var quote = _options.Quote;

            for (int i = 0; i < field.Length; i++)
            {
                var ch = field[i];
                if (ch == delimiter || ch == quote || ch == '\r' || ch == '\n')
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteQuotedField(ReadOnlySpan<char> field)
        {
            var quote = _options.Quote;
            WriteChar(quote);

            for (int i = 0; i < field.Length; i++)
            {
                var ch = field[i];
                if (ch == quote)
                {
                    WriteChar(quote); // Escape quote
                }
                WriteChar(ch);
            }

            WriteChar(quote);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteChar(char ch)
        {
            var span = _writer.GetSpan(1);
            span[0] = ch;
            _writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSpan(ReadOnlySpan<char> text)
        {
            var span = _writer.GetSpan(text.Length);
            text.CopyTo(span);
            _writer.Advance(text.Length);
        }
    }

    /// <summary>
    /// Memory-pooled buffer writer for CSV operations
    /// </summary>
    public sealed class PooledCsvWriter : IBufferWriter<char>, IDisposable
    {
        private char[] _buffer;
        private int _position;
        private readonly ArrayPool<char> _pool;

        public PooledCsvWriter(int initialCapacity = 4096)
        {
            _pool = ArrayPool<char>.Shared;
            _buffer = _pool.Rent(initialCapacity);
            _position = 0;
        }

        public ReadOnlySpan<char> WrittenSpan => _buffer.AsSpan(0, _position);
        public ReadOnlyMemory<char> WrittenMemory => _buffer.AsMemory(0, _position);

        public void Advance(int count)
        {
            if (count < 0 || _position + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            _position += count;
        }

        public Memory<char> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_position);
        }

        public Span<char> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_position);
        }

        private void EnsureCapacity(int sizeHint)
        {
            var needed = _position + sizeHint;
            if (needed <= _buffer.Length)
                return;

            var newSize = Math.Max(needed, _buffer.Length * 2);
            var newBuffer = _pool.Rent(newSize);

            _buffer.AsSpan(0, _position).CopyTo(newBuffer);
            _pool.Return(_buffer);
            _buffer = newBuffer;
        }

        public override string ToString() => new(_buffer, 0, _position);

        public void Dispose()
        {
            if (_buffer != null)
            {
                _pool.Return(_buffer);
                _buffer = null!;
            }
        }
    }

    /// <summary>
    /// Static utility methods for common CSV operations
    /// </summary>
    public static class CsvUtility
    {
#if NET8_0_OR_GREATER
        // Frozen collections for ultra-fast lookups (NET8+)
        private static readonly FrozenSet<char> CommonDelimiters = 
            FrozenSet.ToFrozenSet([',', ';', '\t', '|', ':']);
        
        private static readonly FrozenDictionary<string, CsvOptions> PresetOptions = 
            new Dictionary<string, CsvOptions>
            {
                ["standard"] = new(',', '"', true, false),
                ["excel"] = new(',', '"', true, false),
                ["tab"] = new('\t', '"', true, false),
                ["pipe"] = new('|', '"', true, false),
                ["semicolon"] = new(';', '"', true, false)
            }.ToFrozenDictionary();

        /// <summary>
        /// Auto-detect CSV format using vectorized analysis (NET8+)
        /// </summary>
        public static CsvOptions DetectFormat(ReadOnlySpan<char> sample)
        {
            if (sample.Length < 100)
                return CsvOptions.Default;

            // Take first few lines for analysis
            var lines = sample.Slice(0, Math.Min(1000, sample.Length));
            var delimiterCounts = new Dictionary<char, int>();

            // Count potential delimiters using SearchValues
            foreach (var delimiter in CommonDelimiters)
            {
                var searchValues = SearchValues.Create([delimiter]);
                var remaining = lines;
                var count = 0;

                while (!remaining.IsEmpty)
                {
                    var index = remaining.IndexOfAny(searchValues);
                    if (index == -1) break;
                    count++;
                    remaining = remaining.Slice(index + 1);
                }

                if (count > 0)
                    delimiterCounts[delimiter] = count;
            }

            // Find most consistent delimiter
            var bestDelimiter = delimiterCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            return new CsvOptions(bestDelimiter);
        }

        /// <summary>
        /// Get preset options by name (NET8+)
        /// </summary>
        public static CsvOptions GetPresetOptions(string presetName)
        {
            return PresetOptions.TryGetValue(presetName.ToLowerInvariant(), out var options) 
                ? options 
                : CsvOptions.Default;
        }

        /// <summary>
        /// Ultra-fast field validation using SIMD (NET8+)
        /// </summary>
        public static bool ValidateField(ReadOnlySpan<char> field, char delimiter, char quote)
        {
            if (field.IsEmpty) return true;

            // Check for unescaped quotes using vectorized search
            var quoteSearch = SearchValues.Create([quote]);
            var remaining = field;
            var inQuotes = false;

            while (!remaining.IsEmpty)
            {
                var quoteIndex = remaining.IndexOfAny(quoteSearch);
                if (quoteIndex == -1) break;

                // Check if quote is escaped
                if (quoteIndex + 1 < remaining.Length && remaining[quoteIndex + 1] == quote)
                {
                    remaining = remaining.Slice(quoteIndex + 2);
                    continue;
                }

                inQuotes = !inQuotes;
                remaining = remaining.Slice(quoteIndex + 1);
            }

            return !inQuotes; // Field is valid if we're not left in quotes
        }
#endif

        /// <summary>
        /// Read CSV from a file with streaming
        /// </summary>
        public static List<CsvRecordWrapper> ReadFile(string filePath, CsvOptions options = default)
        {
            var text = File.ReadAllText(filePath);
            var reader = new CsvReader(text.AsSpan(), options);
            var results = new List<CsvRecordWrapper>();

            if (options.HasHeader)
                reader.SkipHeader();

            while (reader.HasMoreData)
            {
                var record = reader.ReadRecord();
                results.Add(new CsvRecordWrapper(record));
            }
            
            return results;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Read CSV from file with auto-detection (NET8+)
        /// </summary>
        public static List<CsvRecordWrapper> ReadFileAutoDetect(string filePath)
        {
            var text = File.ReadAllText(filePath);
            var options = DetectFormat(text.AsSpan(0, Math.Min(1000, text.Length)));
            
            var reader = new CsvReader(text.AsSpan(), options);
            var results = new List<CsvRecordWrapper>();
            
            if (options.HasHeader)
                reader.SkipHeader();

            while (reader.HasMoreData)
            {
                var record = reader.ReadRecord();
                results.Add(new CsvRecordWrapper(record));
            }
            
            return results;
        }

        /// <summary>
        /// Asynchronous CSV reading with UTF-8 optimization (NET8+)
        /// </summary>
        public static async Task<List<CsvRecordWrapper>> ReadFileAsync(string filePath, 
            CsvOptions options = default)
        {
            var utf8Bytes = await File.ReadAllBytesAsync(filePath);
            
            // Ultra-fast UTF-8 to UTF-16 conversion
            var charCount = Encoding.UTF8.GetCharCount(utf8Bytes);
            var chars = ArrayPool<char>.Shared.Rent(charCount);
            var results = new List<CsvRecordWrapper>();
            
            try
            {
                var actualCharCount = Encoding.UTF8.GetChars(utf8Bytes, chars);
                var text = chars.AsSpan(0, actualCharCount);
                
                var reader = new CsvReader(text, options);
                if (options.HasHeader)
                    reader.SkipHeader();

                while (reader.HasMoreData)
                {
                    var record = reader.ReadRecord();
                    results.Add(new CsvRecordWrapper(record));
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
            }
            
            return results;
        }
#endif

        /// <summary>
        /// Write CSV to a file
        /// </summary>
        public static void WriteFile(string filePath, IEnumerable<string[]> records,
            string[]? headers = null, CsvOptions options = default)
        {
            using var pooledWriter = new PooledCsvWriter();
            var writer = new CsvWriter(pooledWriter, options);

            if (headers != null)
            {
                writer.WriteHeader(headers);
            }

            foreach (var record in records)
            {
                writer.WriteRecord(record);
            }

            File.WriteAllText(filePath, pooledWriter.ToString());
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Vectorized field counting for very large records (NET6+)
        /// </summary>
        public static int CountFieldsVectorized(ReadOnlySpan<char> record, char delimiter)
        {
            if (Vector.IsHardwareAccelerated && record.Length >= Vector<ushort>.Count)
            {
                return CountFieldsVectorizedImpl(record, delimiter);
            }
            
            return CountFieldsScalar(record, delimiter);
        }

        private static int CountFieldsVectorizedImpl(ReadOnlySpan<char> record, char delimiter)
        {
            var delimiterVector = new Vector<ushort>(delimiter);
            var count = 1; // Start with 1 field
            var pos = 0;
            var vectorSize = Vector<ushort>.Count;

            // Process in vector-sized chunks
            while (pos <= record.Length - vectorSize)
            {
                var span = record.Slice(pos, vectorSize);
                var vector = new Vector<ushort>(MemoryMarshal.Cast<char, ushort>(span));
                var matches = Vector.Equals(vector, delimiterVector);
                
                count += Vector.Dot(matches, Vector<ushort>.One);
                pos += vectorSize;
            }

            // Process remaining characters scalar
            for (; pos < record.Length; pos++)
            {
                if (record[pos] == delimiter)
                    count++;
            }

            return count;
        }
#endif

#if NET8_0_OR_GREATER && NET9_0_OR_GREATER
        /// <summary>
        /// Next-generation vectorized field counting using NET9 features
        /// </summary>
        public static int CountFieldsNet9(ReadOnlySpan<char> record, char delimiter)
        {
            // Use latest SIMD intrinsics and vectorization improvements in NET9
            if (Vector512.IsHardwareAccelerated && record.Length >= Vector512<ushort>.Count)
            {
                return CountFieldsVector512(record, delimiter);
            }
            
            if (Vector256.IsHardwareAccelerated && record.Length >= Vector256<ushort>.Count)
            {
                return CountFieldsVector256(record, delimiter);
            }
            
            return CountFieldsVectorized(record, delimiter);
        }

        private static int CountFieldsVector512(ReadOnlySpan<char> record, char delimiter)
        {
            var delimiterVector = Vector512.Create((ushort)delimiter);
            var count = 1;
            var pos = 0;
            var vectorSize = Vector512<ushort>.Count;

            while (pos <= record.Length - vectorSize)
            {
                var span = record.Slice(pos, vectorSize);
                var chars = MemoryMarshal.Cast<char, ushort>(span);
                var vector = Vector512.Create(chars);
                var matches = Vector512.Equals(vector, delimiterVector);
                
                count += BitOperations.PopCount(matches.ExtractMostSignificantBits());
                pos += vectorSize;
            }

            // Process remaining with smaller vectors or scalar
            for (; pos < record.Length; pos++)
            {
                if (record[pos] == delimiter)
                    count++;
            }

            return count;
        }

        private static int CountFieldsVector256(ReadOnlySpan<char> record, char delimiter)
        {
            var delimiterVector = Vector256.Create((ushort)delimiter);
            var count = 1;
            var pos = 0;
            var vectorSize = Vector256<ushort>.Count;

            while (pos <= record.Length - vectorSize)
            {
                var span = record.Slice(pos, vectorSize);
                var chars = MemoryMarshal.Cast<char, ushort>(span);
                var vector = Vector256.Create(chars);
                var matches = Vector256.Equals(vector, delimiterVector);
                
                count += BitOperations.PopCount(matches.ExtractMostSignificantBits());
                pos += vectorSize;
            }

            return count;
        }
#endif

        private static int CountFieldsScalar(ReadOnlySpan<char> record, char delimiter)
        {
            var count = 1;
            for (int i = 0; i < record.Length; i++)
            {
                if (record[i] == delimiter)
                    count++;
            }
            return count;
        }
    }
}

// Example usage
public static class CsvExample
{
    public static void Example()
    {
        // Basic reading CSV
        var csvData = "Name,Age,City\r\nJohn,25,\"New York\"\r\nJane,30,London";
        var reader = new CsvReader(csvData.AsSpan());

        reader.SkipHeader(); // Skip header if present

        foreach (var record in reader)
        {
            foreach (var field in record)
            {
                Console.Write($"'{field.ToString()}' ");
            }
            Console.WriteLine();
        }

        // Basic writing CSV
        using var pooledWriter = new FastCsv.PooledCsvWriter();
        var writer = new FastCsv.CsvWriter(pooledWriter);

        writer.WriteHeader("Name", "Age", "City");
        writer.WriteRecord("John", "25", "New York");
        writer.WriteRecord("Jane", "30", "London");

        Console.WriteLine(pooledWriter.ToString());
    }

#if NET8_0_OR_GREATER
    public static void Net8Features()
    {
        // Auto-detect CSV format
        var unknownCsv = "Name;Age;City\r\nJohn;25;Paris\r\nJane;30;Berlin";
        var detectedOptions = FastCsv.CsvUtility.DetectFormat(unknownCsv);
        Console.WriteLine($"Detected delimiter: '{detectedOptions.Delimiter}'");
        
        // Use preset configurations
        var excelOptions = FastCsv.CsvUtility.GetPresetOptions("excel");
        var tabOptions = FastCsv.CsvUtility.GetPresetOptions("tab");
        
        // Fast field validation
        var field = "Valid field";
        var isValid = FastCsv.CsvUtility.ValidateField(field, ',', '"');
        Console.WriteLine($"Field is valid: {isValid}");
        
        // Read with auto-detection
        foreach (var record in FastCsv.CsvUtility.ReadFileAutoDetect("data.csv"))
        {
            Console.WriteLine($"Record from line {record.LineNumber}");
        }
    }

    public static async Task Net8AsyncFeatures()
    {
        // Asynchronous reading with UTF-8 optimization
        var records = await FastCsv.CsvUtility.ReadFileAsync("large_data.csv");
        foreach (var record in records)
        {
            // Process each record asynchronously
            foreach (var field in record)
            {
                Console.Write($"{field} ");
            }
            Console.WriteLine();
        }
    }
#endif

#if NET9_0_OR_GREATER
    public static void Net9Features()
    {
        // Ultra-fast vectorized field counting with NET9 SIMD
        var largeRecord = "field1,field2,field3," + string.Join(",", Enumerable.Range(1, 1000));
        var fieldCount = FastCsv.CsvUtility.CountFieldsNet9(largeRecord, ',');
        Console.WriteLine($"Field count using NET9 SIMD: {fieldCount}");
        
        // Benefit from improved JIT optimizations and devirtualization
        // The ref struct enumerators will be even faster in NET9
    }
#endif
}