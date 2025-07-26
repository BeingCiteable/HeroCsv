using FastCsv.DataSources;
using FastCsv.Errors;
using FastCsv.Models;
using FastCsv.Parsing;
using FastCsv.Validation;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv.Core;

/// <summary>
/// High-performance CSV reader implementation optimized for zero allocations
/// </summary>
public sealed partial class FastCsvReader : ICsvReader, IDisposable
{
    private readonly ICsvDataSource _dataSource;
    private readonly CsvOptions _options;
    private int _recordCount = 0;
    private bool _disposed = false;

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
    private int EstimateRowCount(ReadOnlySpan<char> buffer)
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
                    var fields = CsvParser.ParseLine(lineSpan, _options);
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
                var buffer = GetBuffer();
                var newlineCount = _dataSource.CountLinesDirectly();
                var totalLines = newlineCount;

                // Check if last character is not a newline (indicates one more line)
                if (buffer.Length > 0 && buffer[buffer.Length - 1] != '\n' && buffer[buffer.Length - 1] != '\r')
                {
                    totalLines++; // Add one for the final line without newline
                }

                // Account for header if present
                if (_options.HasHeader && totalLines > 0)
                {
                    return totalLines - 1;
                }

                return totalLines;
            }
            else
            {
                // For stream-based sources, use CountLinesDirectly as-is
                var totalLines = _dataSource.CountLinesDirectly();

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
        // Only reset if the data source supports it
        if (_dataSource.SupportsReset)
        {
            Reset();
        }

        // Use async data source if available
        if (_dataSource is IAsyncCsvDataSource asyncSource)
        {
            var records = new List<string[]>();
            bool headerSkipped = !_options.HasHeader; // If no header, consider it already skipped

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await asyncSource.TryReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (!result.success) break;

                // Skip empty lines
                if (string.IsNullOrEmpty(result.line)) continue;

                // Skip header if configured and not yet skipped
                if (_options.HasHeader && !headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                var fields = CsvParser.ParseLine(result.line.AsSpan(), _options);

                // Perform validation if enabled
                if (_validationHandler.IsEnabled || _errorHandler.IsEnabled)
                {
                    _validationHandler.ValidateRecord(fields, result.lineNumber, _validationHandler.ExpectedFieldCount);
                }

                records.Add(fields);
                _recordCount++;
            }

            return records;
        }
#endif
        // Fallback to sync method wrapped in Task.Run for non-async sources
        return await Task.Run(() => ReadAllRecords(), cancellationToken).ConfigureAwait(false);
    }

}