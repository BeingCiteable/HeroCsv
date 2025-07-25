using FastCsv.Errors;
using FastCsv.Validation;
using System.Text;

namespace FastCsv;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
public sealed partial class FastCsvReader : ICsvReader, IDisposable
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
    /// Get the current CSV options being used
    /// </summary>
    public CsvOptions Options => _options;

    /// <summary>
    /// Get validation results if validation is enabled
    /// </summary>
    public CsvValidationResult ValidationResult => _errorHandler.GetValidationResult();

    
    
    /// <summary>
    /// Internal method to get the next line position
    /// </summary>
    internal bool TryGetNextLine(out int lineStart, out int lineLength, out int lineNumber)
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
    internal ReadOnlySpan<char> GetBuffer() => _dataSource.GetBuffer();
    
    /// <summary>
    /// Check if we can use the optimized parsing path
    /// </summary>
    private bool CanUseOptimizedPath()
    {
        return _dataSource is StringDataSource || _dataSource is MemoryDataSource;
    }
    
    
    /// <summary>
    /// Get zero-allocation row enumerator for processing rows as spans
    /// </summary>
    /// <returns>Enumerator that returns CsvRow ref structs with fast field access</returns>
    public CsvParser.WholeBufferCsvEnumerable EnumerateRows()
    {
        if (_dataSource is StringDataSource stringSource)
        {
            var buffer = stringSource.GetBuffer();
            return CsvParser.ParseWholeBuffer(buffer, _options);
        }
        else if (_dataSource is MemoryDataSource memorySource)
        {
            var buffer = memorySource.GetBuffer();
            return CsvParser.ParseWholeBuffer(buffer, _options);
        }
        else
        {
            throw new NotSupportedException("Maximum performance enumeration is only supported for string and memory data sources");
        }
    }
    
    /// <summary>
    /// Provides high-performance field iteration without allocations
    /// </summary>
    /// <returns>Iterator for efficient field-by-field processing</returns>
    public CsvFieldIterator.CsvFieldCollection IterateFields()
    {
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

        // Use optimized path when possible
        if (CanUseOptimizedPath())
        {
            // Use whole-buffer parsing with pre-computed field positions for maximum performance
            foreach (var row in EnumerateRows())
            {
                var fields = new string[row.FieldCount];
                for (int i = 0; i < row.FieldCount; i++)
                {
                    fields[i] = row[i].ToString();
                }
                records.Add(fields);
            }
        }
        else
        {
            // Fallback to standard path for stream sources
            while (TryReadRecord(out var record))
            {
                records.Add(record.ToArray());
            }
        }

        return records;
    }

    /// <inheritdoc />
    public IEnumerable<string[]> GetRecords()
    {
        Reset();
        
        // Can't use optimized path with yield due to ref struct limitations
        // Users who want optimized performance should use ReadAllRecords() or EnumerateRows() directly
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
        if (!_validationHandler.IsEnabled && !_errorHandler.IsEnabled)
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

}
