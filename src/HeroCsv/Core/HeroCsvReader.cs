using HeroCsv.Configuration;
using HeroCsv.DataSources;
using HeroCsv.Errors;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Validation;
using System.Runtime.CompilerServices;
using System.Text;

namespace HeroCsv.Core;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
public sealed partial class HeroCsvReader : ICsvReader, IDisposable
#if NET6_0_OR_GREATER
    , IAsyncDisposable
#endif
{
    private readonly ICsvDataSource _dataSource;
    private readonly CsvReaderConfiguration _configuration;
    private readonly CsvOptions _options; // Keep for backward compatibility
    private readonly ParsingStrategySelector _parsingSelector;
    private int _recordCount;
    private bool _disposed;

    // CA1859: These fields must remain as interfaces to support dependency injection
    // and different implementations (e.g., NullErrorHandler, ValidationHandler)
    private readonly IValidationHandler _validationHandler;
    private readonly IErrorHandler _errorHandler;

    /// <summary>
    /// Creates a new HeroCsvReader from string content
    /// </summary>
    public HeroCsvReader(
        string content,
        CsvOptions options,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        if (content == null) throw new ArgumentNullException(nameof(content),
            "CSV content cannot be null. Provide valid CSV string data or use empty string for no data.");
        
        _dataSource = new StringDataSource(content);
        _options = options; // Keep for backward compatibility
        _configuration = new CsvReaderConfiguration
        {
            Options = options,
            EnableValidation = validateData,
            TrackErrors = trackErrors,
            ErrorCallback = errorCallback,
            StringPool = options.StringPool,
            StringBuilderPool = new StringBuilderPool()
        };
        
        _parsingSelector = new ParsingStrategySelector(_configuration.StringBuilderPool);
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Creates a new HeroCsvReader from data source
    /// </summary>
    internal HeroCsvReader(
        ICsvDataSource dataSource,
        CsvOptions options,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _options = options; // Keep for backward compatibility
        _configuration = new CsvReaderConfiguration
        {
            Options = options,
            EnableValidation = validateData,
            TrackErrors = trackErrors,
            ErrorCallback = errorCallback,
            StringPool = options.StringPool,
            StringBuilderPool = new StringBuilderPool()
        };
        
        _parsingSelector = new ParsingStrategySelector(_configuration.StringBuilderPool);
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Creates a new HeroCsvReader from a stream
    /// </summary>
    public HeroCsvReader(
        Stream stream,
        CsvOptions options,
        Encoding? encoding = null,
        bool leaveOpen = false,
        bool validateData = false,
        bool trackErrors = false,
        Action<CsvValidationError>? errorCallback = null)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream),
            "Stream cannot be null. Provide a valid stream containing CSV data.");
        
        _dataSource = new StreamDataSource(stream, encoding, leaveOpen);
        _options = options; // Keep for backward compatibility
        _configuration = new CsvReaderConfiguration
        {
            Options = options,
            EnableValidation = validateData,
            TrackErrors = trackErrors,
            ErrorCallback = errorCallback,
            StringPool = options.StringPool,
            StringBuilderPool = new StringBuilderPool()
        };
        
        _parsingSelector = new ParsingStrategySelector(_configuration.StringBuilderPool);
        _errorHandler = trackErrors ? new ErrorHandler(true) : new NullErrorHandler();
        if (errorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += errorCallback;
        }
        _validationHandler = new ValidationHandler(options, _errorHandler, validateData);
    }

    /// <summary>
    /// Creates a new HeroCsvReader with configuration object for maximum flexibility
    /// </summary>
    public HeroCsvReader(ICsvDataSource dataSource, CsvReaderConfiguration configuration)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = configuration.Options; // Keep for backward compatibility
        
        _parsingSelector = new ParsingStrategySelector(_configuration.StringBuilderPool);
        _errorHandler = _configuration.ErrorHandler ?? (_configuration.TrackErrors ? new ErrorHandler(true) : new NullErrorHandler());
        if (_configuration.ErrorCallback != null && _errorHandler is ErrorHandler handler)
        {
            handler.ErrorOccurred += _configuration.ErrorCallback;
        }
        _validationHandler = _configuration.ValidationHandler ?? new ValidationHandler(_configuration.Options, _errorHandler, _configuration.EnableValidation);
    }

    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    public int LineNumber { get; private set; } = 1;

    /// <summary>
    /// Indicates whether more data is available to read
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
    public CsvFieldIterator.CsvFieldEnumerable IterateFields()
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
#if NET7_0_OR_GREATER
        ThrowIfDisposed();
#else
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeroCsvReader));
        }
#endif

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

        var fields = _parsingSelector.ParseLine(lineSpan, _options);

        // Skip empty records (empty line parsed as empty array or single empty field)
        if (fields.Length == 0 || (fields.Length == 1 && string.IsNullOrEmpty(fields[0])))
        {
            return TryReadRecord(out record);
        }

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
        LineNumber = lineNumber + 1; // Increment to reflect the next line to be read
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

        if (_dataSource != null)
        {
            await _dataSource.DisposeAsync().ConfigureAwait(false);
        }
        
        _disposed = true;
    }
#endif


    /// <inheritdoc />
    public IReadOnlyList<string[]> ReadAllRecords()
    {
        Reset();

        // Automatically select optimal parsing strategy based on data source
        if (_dataSource is StringDataSource stringSource)
        {
            var buffer = stringSource.GetBuffer();

            // Pre-allocate list capacity for better performance
            var estimatedRows = EstimateRowCount(buffer);
            var records = new List<string[]>(estimatedRows);

            ParseBufferDirectly(buffer, records);
            return records;
        }
        else if (_dataSource is MemoryDataSource memorySource)
        {
            var buffer = memorySource.GetBuffer();

            // Pre-allocate list capacity for better performance
            var estimatedRows = EstimateRowCount(buffer);
            var records = new List<string[]>(estimatedRows);

            ParseBufferDirectly(buffer, records);
            return records;
        }
        else
        {
            // Stream-based sources require incremental parsing
            var records = new List<string[]>();

            // Skip header if configured
            if (_options.HasHeader && HasMoreData)
            {
                SkipRecord();
            }

            while (TryReadRecord(out var record))
            {
                records.Add(record.ToArray());
            }
            return records;
        }
    }

    /// <summary>
    /// Estimates row count for pre-allocation to reduce list resizing overhead
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateRowCount(ReadOnlySpan<char> buffer)
    {
        if (buffer.IsEmpty) return 0;

        // Quick estimation: count newlines for rough row count
        var newlineCount = 0;
        for (int i = 0; i < Math.Min(buffer.Length, 1000); i++)
        {
            if (buffer[i] == '\n') newlineCount++;
        }

        // Extrapolate if we only sampled part of the buffer
        if (buffer.Length > 1000)
        {
            newlineCount = (int)((long)newlineCount * buffer.Length / 1000);
        }

        return Math.Max(16, newlineCount + 1);
    }

    /// <summary>
    /// Parses CSV content using the fastest available method for the current platform
    /// </summary>
    private unsafe void ParseBufferDirectly(ReadOnlySpan<char> buffer, List<string[]> records)
    {
        // For maximum performance, work directly with the buffer like Sep/Sylvan
        var position = 0;
        var length = buffer.Length;

        // Skip header row if configured
        if (_options.HasHeader && length > 0)
        {
            // Find end of header line
            while (position < length && buffer[position] != '\r' && buffer[position] != '\n')
                position++;
            // Skip newline characters
            if (position < length && buffer[position] == '\r') position++;
            if (position < length && buffer[position] == '\n') position++;
        }

        // Process all data rows with optimized parsing
        fixed (char* bufferPtr = buffer)
        {
            while (position < length)
            {
                var lineStart = position;

                // Find end of current line
                while (position < length && buffer[position] != '\r' && buffer[position] != '\n')
                    position++;

                var lineLength = position - lineStart;
                if (lineLength > 0)
                {
                    // Create span for current line without allocation
                    var lineSpan = buffer.Slice(lineStart, lineLength);

                    // Parse line with optimized parser (SIMD-enabled for common cases)
                    var fields = _parsingSelector.ParseLine(lineSpan, _options);
                    records.Add(fields);
                }

                // Skip newline sequence (\r, \n, or \r\n)
                if (position < length && buffer[position] == '\r') position++;
                if (position < length && buffer[position] == '\n') position++;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<string[]> GetRecords()
    {
        Reset();

        // Skip header if configured
        if (_options.HasHeader && HasMoreData)
        {
            SkipRecord();
        }

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
            // Only use buffer optimization for data sources that support it
            if (_dataSource is StringDataSource || _dataSource is MemoryDataSource)
            {
                var totalLines = _dataSource.CountLines();

                // Account for header if present
                if (_options.HasHeader && totalLines > 0)
                {
                    return totalLines - 1;
                }

                return totalLines;
            }
            else
            {
                // For stream-based sources, use CountLines as-is
                var totalLines = _dataSource.CountLines();

                // Account for header if present
                if (_options.HasHeader && totalLines > 0)
                {
                    return totalLines - 1;
                }

                return totalLines;
            }
        }

        // Fall back to record-by-record counting when validation is needed
        var count = 0;

        // Skip header on first read if configured
        if (_options.HasHeader && HasMoreData)
        {
            _dataSource.TryReadLine(out _, out _); // Skip header line
        }

        while (TryReadRecord(out _))
        {
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string[]>> ReadAllRecordsAsync(CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        // Yield to make this truly async
        await Task.Yield();
        
        // Only reset if the data source supports it
        if (_dataSource.SupportsReset)
        {
            Reset();
        }

        // Skip header if configured
        if (_options.HasHeader && HasMoreData)
        {
            SkipRecord();
        }

        // Fallback to sync method - async operations require async data source
        var records = new List<string[]>();
        while (!cancellationToken.IsCancellationRequested && TryReadRecord(out var record))
        {
            records.Add(record.ToArray());
        }
        return records;
#else
        // Check for cancellation before starting work
        if (cancellationToken.IsCancellationRequested)
        {
            return []; // Return empty results when cancelled
        }

        // Fallback to sync method wrapped in Task.Run for non-async sources
        return await Task.Run(() => ReadAllRecords(), cancellationToken).ConfigureAwait(false);
#endif
    }

#if NET7_0_OR_GREATER
    partial void ThrowIfDisposed();
#endif
}