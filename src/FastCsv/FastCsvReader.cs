using FastCsv.Errors;
using FastCsv.Validation;
using System.Text;

namespace FastCsv;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
public sealed partial class FastCsvReader : ICsvReader, IInternalCsvReader, IDisposable
{
    private readonly ICsvDataSource _dataSource;
    private readonly CsvOptions _options;
    private int _recordCount = 0;
    private bool _disposed = false;

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
        if (content == null) throw new ArgumentNullException(nameof(content));
        _dataSource = new StringDataSource(content);
        _options = options;

        // Initialize handlers
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Creates a new FastCsvReader from data source
    /// </summary>
    internal FastCsvReader(
        ICsvDataSource dataSource,
        CsvOptions options,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _options = options;

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
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        _dataSource = new StreamDataSource(stream, encoding, leaveOpen);
        _options = options;

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
    public int LineNumber { get; private set; } = 1;

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    public bool HasMoreData => _dataSource.HasMoreData;

    /// <summary>
    /// Total number of records processed so far
    /// </summary>
    public int RecordCount => _recordCount;

    /// <summary>
    /// Current position in the input data
    /// </summary>
    public int Position => 0; // Position tracking moved to data source

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
    
    /// <inheritdoc />
    public RowEnumerable EnumerateRows() => new RowEnumerable(this);
    
    /// <inheritdoc />
    public UltraFastRowEnumerable EnumerateRowsFast() => new UltraFastRowEnumerable(this);
    
    /// <inheritdoc />
    public SepStyleParser.SepStyleEnumerable EnumerateSepStyle()
    {
        if (_dataSource is StringDataSource stringSource)
        {
            var buffer = stringSource.GetBuffer();
            return SepStyleParser.Parse(buffer, _options);
        }
        else if (_dataSource is MemoryDataSource memorySource)
        {
            var buffer = memorySource.GetBuffer();
            return SepStyleParser.Parse(buffer, _options);
        }
        else
        {
            throw new NotSupportedException("Sep-style enumeration is only supported for string and memory data sources");
        }
    }
    
    /// <inheritdoc />
    public CsvFieldIterator.CsvFieldCollection IterateFields()
    {
        // Get the entire content as a span
        if (_dataSource is StringDataSource stringSource)
        {
            var buffer = stringSource.GetBuffer();
            return CsvFieldIterator.IterateFields(buffer, _options);
        }
        else if (_dataSource is MemoryDataSource memorySource)
        {
            var buffer = memorySource.GetBuffer();
            return CsvFieldIterator.IterateFields(buffer, _options);
        }
        else
        {
            throw new NotSupportedException("Field iteration is only supported for string and memory data sources");
        }
    }
    
    /// <summary>
    /// Internal method to get the next line position
    /// </summary>
    bool IInternalCsvReader.TryGetNextLine(out int lineStart, out int lineLength, out int lineNumber)
    {
        if (_dataSource.TryGetLinePosition(out lineStart, out lineLength, out lineNumber))
        {
            _recordCount++;
            return true;
        }
        
        lineStart = 0;
        lineLength = 0;
        lineNumber = 0;
        return false;
    }
    
    /// <summary>
    /// Get the buffer for zero-copy access
    /// </summary>
    ReadOnlySpan<char> IInternalCsvReader.GetBuffer() => _dataSource.GetBuffer();


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

        if (!_dataSource.TryReadLine(out var lineSpan, out var lineNumber))
        {
            record = null!;
            return false;
        }

        LineNumber = lineNumber;

        // Skip empty lines
        if (lineSpan.IsEmpty)
        {
            return TryReadRecord(out record);
        }

        var fields = CsvParser.ParseLine(lineSpan, _options);

        // Perform validation if enabled
        if (_validationHandler.IsEnabled || _errorHandler.IsEnabled)
        {
            _validationHandler.ValidateRecord(fields, LineNumber, _validationHandler.ExpectedFieldCount);
        }

        record = new CsvRecord(fields, LineNumber);
        _recordCount++;

        return true;
    }

    /// <summary>
    /// Skip the next record without parsing it
    /// </summary>
    public void SkipRecord()
    {
        if (!HasMoreData) return;
        
        _dataSource.TryReadLine(out _, out var lineNumber);
        LineNumber = lineNumber;
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
        if (!_dataSource.SupportsReset)
        {
            throw new NotSupportedException("Data source does not support reset");
        }

        _dataSource.Reset();
        LineNumber = 1;
        _recordCount = 0;
        _validationHandler.Reset();
        _errorHandler.Reset();
    }

    /// <summary>
    /// Dispose the reader and free resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _dataSource?.Dispose();
        _disposed = true;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asynchronously dispose the reader and free resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // For now, just use sync dispose since ICsvDataSource doesn't have async dispose
        Dispose();
        await Task.CompletedTask;
    }
#endif

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
        
        // Use ultra-fast counting when validation is disabled
        if (!IsValidationEnabled && !IsErrorTrackingEnabled)
        {
            var totalLines = _dataSource.CountLinesDirectly();
            
            // Account for header if present
            if (_options.HasHeader && totalLines > 0)
            {
                return totalLines - 1;
            }
            
            return totalLines;
        }
        
        // Fall back to record-by-record counting when validation is needed
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
        // For now, use sync method wrapped in Task.Run
        // TODO: Add async support to ICsvDataSource for true async streaming
        return await Task.Run(() => ReadAllRecords(), cancellationToken);
    }

    public Task<IEnumerable<string[]>> GetRecordsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    /// <inheritdoc />
    public IEnumerable<int> EnumerateWithoutParsing()
    {
        Reset();
        int count = 0;
        
        // Ultra-fast counting without any parsing
        while (_dataSource.HasMoreData)
        {
            if (_dataSource.TryReadLine(out _, out _))
            {
                count++;
                yield return count;
            }
        }
    }
}
