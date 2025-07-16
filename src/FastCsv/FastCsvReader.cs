using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
/// <remarks>
/// Creates a new FastCsvReader with the specified content and options
/// </remarks>
/// <param name="content">CSV content to parse</param>
/// <param name="options">Parsing options</param>
internal sealed partial class FastCsvReader(string content, CsvOptions options) : ICsvReader, IDisposable
{
    private readonly string _content = content ?? throw new ArgumentNullException(nameof(content));
    private readonly bool _hasHeader = options.HasHeader;
    private int _position = 0;
    private int _lineNumber = 1;
    private int _recordCount = 0;
    private bool _disposed = false;
    private bool _headerSkipped = false;
    private ICsvRecord? _currentRecord;

    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    public bool HasMoreData => _position < _content.Length;

    /// <summary>
    /// Total number of records processed so far
    /// </summary>
    public int RecordCount => _recordCount;

    /// <summary>
    /// Current position in the input data
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Get the current CSV options being used
    /// </summary>
    public CsvOptions Options => options;

    /// <summary>
    /// Read the next CSV record
    /// </summary>
    public ICsvRecord ReadRecord()
    {
        if (!TryReadRecord(out var record))
        {
            throw new InvalidOperationException("No more records available");
        }
        return record;
    }

    /// <summary>
    /// Try to read the next record, returns false if no more data
    /// </summary>
    public bool TryReadRecord(out ICsvRecord record)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FastCsvReader));
        }

        if (!HasMoreData)
        {
            record = null!;
            return false;
        }

        // Skip header if not already skipped
        if (_hasHeader && !_headerSkipped)
        {
            SkipHeader();
            if (!HasMoreData)
            {
                record = null!;
                return false;
            }
        }

        var lineStart = _position;
        var lineEnd = FindLineEnd();

        if (lineEnd == lineStart)
        {
            // Empty line, skip it
            SkipLineEnding();
            return TryReadRecord(out record);
        }

        var line = _content.AsSpan(lineStart, lineEnd - lineStart);
        var fields = ParseLine(line);

        record = new FastCsvRecord(fields, _lineNumber);
        _currentRecord = record;
        _recordCount++;

        SkipLineEnding();

        return true;
    }

    /// <summary>
    /// Skip the header row
    /// </summary>
    public void SkipHeader()
    {
        if (_headerSkipped) return;

        SkipRecord();
        _headerSkipped = true;
    }

    /// <summary>
    /// Skip the next record without parsing it
    /// </summary>
    public void SkipRecord()
    {
        if (!HasMoreData) return;

        FindLineEnd();
        SkipLineEnding();
    }

    /// <summary>
    /// Skip multiple records
    /// </summary>
    public void SkipRecords(int count)
    {
        for (int i = 0; i < count && HasMoreData; i++)
        {
            SkipRecord();
        }
    }

    /// <summary>
    /// Reset the reader to the beginning
    /// </summary>
    public void Reset()
    {
        _position = 0;
        _lineNumber = 1;
        _recordCount = 0;
        _headerSkipped = false;
    }

    /// <summary>
    /// Dispose the reader and free resources
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindLineEnd()
    {
        var start = _position;
        for (int i = start; i < _content.Length; i++)
        {
            var ch = _content[i];
            if (ch == '\n' || ch == '\r')
            {
                return i;
            }
        }
        return _content.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipLineEnding()
    {
        if (_position >= _content.Length) return;

        var ch = _content[_position];
        if (ch == '\r')
        {
            _position++;
            if (_position < _content.Length && _content[_position] == '\n')
            {
                _position++;
            }
        }
        else if (ch == '\n')
        {
            _position++;
        }

        _lineNumber++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string[] ParseLine(ReadOnlySpan<char> line)
    {
        if (line.IsEmpty) return Array.Empty<string>();

        var fields = new List<string>(8); // Pre-allocate for common case
        var fieldStart = 0;
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var ch = line[i];

            if (ch == options.Quote)
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    fieldStart = i + 1;
                }
                else if (i + 1 < line.Length && line[i + 1] == options.Quote)
                {
                    // Escaped quote, skip both
                    i += 2;
                    continue;
                }
                else
                {
                    inQuotes = false;
                }
            }
            else if (ch == options.Delimiter && !inQuotes)
            {
                var fieldSpan = line.Slice(fieldStart, i - fieldStart);
                var field = fieldSpan.ToString();
                if (options.TrimWhitespace)
                {
                    field = field.Trim();
                }
                fields.Add(field);
                fieldStart = i + 1;
            }

            i++;
        }

        // Add final field
        if (fieldStart <= line.Length)
        {
            var fieldSpan = line.Slice(fieldStart);
            var field = fieldSpan.ToString();
            if (options.TrimWhitespace)
            {
                field = field.Trim();
            }
            fields.Add(field);
        }

        return [.. fields];
    }
}

/// <summary>
/// High-performance CSV record implementation using spans for zero allocations
/// </summary>
internal sealed partial class FastCsvRecord : ICsvRecord
{
    private readonly string[] _fields;
    private readonly int _lineNumber;

    /// <summary>
    /// Creates a new FastCsvRecord with the specified fields and line number
    /// </summary>
    /// <param name="fields">Array of field values</param>
    /// <param name="lineNumber">Line number of this record</param>
    public FastCsvRecord(string[] fields, int lineNumber)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _lineNumber = lineNumber;
    }

    /// <summary>
    /// Line number of this record (1-based)
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Number of fields in this record
    /// </summary>
    public int FieldCount => _fields.Length;

    /// <summary>
    /// Get a field by index (0-based)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetField(int index)
    {
        if (index < 0 || index >= _fields.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return _fields[index].AsSpan();
    }

    /// <summary>
    /// Try to get a field by index
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetField(int index, out ReadOnlySpan<char> field)
    {
        if (index >= 0 && index < _fields.Length)
        {
            field = _fields[index].AsSpan();
            return true;
        }
        field = ReadOnlySpan<char>.Empty;
        return false;
    }

    /// <summary>
    /// Check if the field index is valid
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < _fields.Length;
    }

    /// <summary>
    /// Get all fields into a destination span
    /// </summary>
    public int GetAllFields(Span<string> destination)
    {
        var count = Math.Min(_fields.Length, destination.Length);
        for (int i = 0; i < count; i++)
        {
            destination[i] = _fields[i];
        }
        return count;
    }

    /// <summary>
    /// Get all fields as a string array
    /// </summary>
    public string[] ToArray()
    {
        var result = new string[_fields.Length];
        Array.Copy(_fields, result, _fields.Length);
        return result;
    }
}

/// <summary>
/// Extension methods for ICsvRecord
/// </summary>
public static class CsvRecordExtensions
{
    /// <summary>
    /// Convert CSV record to string array
    /// </summary>
    public static string[] ToArray(this ICsvRecord record)
    {
        if (record is FastCsvRecord fastRecord)
        {
            return fastRecord.ToArray();
        }

        var result = new string[record.FieldCount];
        for (int i = 0; i < record.FieldCount; i++)
        {
            result[i] = record.GetField(i).ToString();
        }
        return result;
    }
}