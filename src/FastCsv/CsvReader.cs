using System.Runtime.CompilerServices;

#if NETSTANDARD2_0
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
using System.Buffers;
#endif


namespace FastCsv;

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
