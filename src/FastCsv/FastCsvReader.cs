using FastCsv.Errors;
using FastCsv.Validation;
using System.Text;

namespace FastCsv;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
internal sealed partial class FastCsvReader : ICsvReader, IDisposable
{
    private readonly Stream? _stream;
    private readonly StreamReader? _streamReader;
    private readonly string? _content;
    private readonly CsvOptions _options;
    private readonly bool _leaveOpen;

    private int _position = 0;
    private int _lineNumber = 1;
    private int _recordCount = 0;
    private bool _disposed = false;

    // For stream-based reading
    private bool _endOfStream = false;

    // Validation and error tracking handlers
    private readonly IValidationHandler _validationHandler;
    private readonly IErrorHandler _errorHandler;

    /// <summary>
    /// Creates a new FastCsvReader from string content
    /// </summary>
    public FastCsvReader(
        string content,
        CsvOptions options,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _options = options;
        _leaveOpen = false;

        // Initialize handlers
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Creates a new FastCsvReader from a stream
    /// </summary>
    public FastCsvReader(
        Stream stream,
        CsvOptions options,
        Encoding? encoding = null,
        bool leaveOpen = false,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _streamReader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: leaveOpen);
        _options = options;
        _leaveOpen = leaveOpen;

        // Initialize handlers
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    public bool HasMoreData => _content != null
        ? _position < _content.Length : !_endOfStream;

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
    public CsvOptions Options => _options;

    /// <summary>
    /// Get validation results if validation is enabled
    /// </summary>
    public CsvValidationResult ValidationResult => _errorHandler.GetValidationResult();

    /// <summary>
    /// Whether validation is enabled
    /// </summary>
    public bool IsValidationEnabled => _validationHandler.IsEnabled;

    /// <summary>
    /// Whether error tracking is enabled
    /// </summary>
    public bool IsErrorTrackingEnabled => _errorHandler.IsEnabled;


    /// <summary>
    /// Read the next CSV record
    /// </summary>
    public ICsvRecord ReadRecord()
    {
        return !TryReadRecord(out var record)
            ? throw new InvalidOperationException("No more records available")
            : record;
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

        string line;
        if (_content != null)
        {
            // String-based reading
            var lineStart = _position;
            var lineEnd = CsvParser.FindLineEnd(_content.AsSpan(), _position);

            if (lineEnd == lineStart)
            {
                // Empty line, skip it
                _position = CsvParser.SkipLineEnding(_content.AsSpan(), _position);
                _lineNumber++;
                return TryReadRecord(out record);
            }

            line = _content.Substring(lineStart, lineEnd - lineStart);
            _position = CsvParser.SkipLineEnding(_content.AsSpan(), lineEnd);
            _lineNumber++;
        }
        else
        {
            // Stream-based reading
            line = ReadLineFromStream()!;
            if (line == null)
            {
                record = null!;
                return false;
            }

            if (string.IsNullOrEmpty(line))
            {
                // Empty line, skip it
                return TryReadRecord(out record);
            }
        }

        var fields = CsvParser.ParseLine(line.AsSpan(), _options);

        // Perform validation if enabled
        if (_validationHandler.IsEnabled || _errorHandler.IsEnabled)
        {
            _validationHandler.ValidateRecord(fields, _lineNumber, _validationHandler.ExpectedFieldCount);
        }

        record = new CsvRecord(fields, _lineNumber);
        _recordCount++;

        return true;
    }

    /// <summary>
    /// Skip the next record without parsing it
    /// </summary>
    public void SkipRecord()
    {
        if (!HasMoreData) return;

        if (_content != null)
        {
            var lineEnd = CsvParser.FindLineEnd(_content.AsSpan(), _position);
            _position = CsvParser.SkipLineEnding(_content.AsSpan(), lineEnd);
            _lineNumber++;
        }
        else
        {
            ReadLineFromStream();
        }
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
        if (_stream != null && !_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot reset a non-seekable stream");
        }

        _position = 0;
        _lineNumber = 1;
        _recordCount = 0;
        _endOfStream = false;
        _validationHandler.Reset();
        _errorHandler.Reset();

        if (_stream != null)
        {
            _stream.Position = 0;
            _streamReader!.DiscardBufferedData();
        }
    }

    /// <summary>
    /// Dispose the reader and free resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
        {
            _streamReader?.Dispose();
            _stream?.Dispose();
        }

        _disposed = true;
    }

    private string? ReadLineFromStream()
    {
        if (_streamReader == null || _endOfStream)
            return null;

        var line = _streamReader.ReadLine();
        if (line == null)
        {
            _endOfStream = true;
        }
        else
        {
            _lineNumber++;
        }
        return line;
    }

    /// <inheritdoc />
    public IReadOnlyList<string[]> ReadAllRecords()
    {
        Reset();
        var records = new List<string[]>();

        while (TryReadRecord(out var record))
        {
            records.Add(record.ToArray());
        }

        return records;
    }

    /// <inheritdoc />
    public IEnumerable<string[]> GetRecords()
    {
        Reset();
        while (TryReadRecord(out var record))
        {
            yield return record.ToArray();
        }
    }

    /// <inheritdoc />
    public int CountRecords()
    {
        Reset();
        var count = 0;

        while (TryReadRecord(out _))
        {
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string[]>> ReadAllRecordsAsync(CancellationToken cancellationToken = default)
    {
        if (_stream != null && _streamReader != null)
        {
            // True async for streams
            Reset();
            var records = new List<string[]>();

            string? line;
            while (!cancellationToken.IsCancellationRequested)
            {
#if NET7_0_OR_GREATER
                line = await _streamReader.ReadLineAsync(cancellationToken);
#else
                line = await _streamReader.ReadLineAsync();
#endif
                if (string.IsNullOrEmpty(line))
                {
                    _lineNumber++;
                    continue;
                }

                var fields = CsvParser.ParseLine(line.AsSpan(), _options);
                records.Add(fields);
                _recordCount++;
                _lineNumber++;
            }

            return records;
        }
        else
        {
            // Sync wrapped in Task.Run for string content
            return await Task.Run(() => ReadAllRecords(), cancellationToken);
        }
    }

    public Task<IEnumerable<string[]>> GetRecordsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
