namespace FastCsv;

/// <summary>
/// Core interface for CSV reading operations
/// Focused on reading records and navigation
/// </summary>
#if NET6_0_OR_GREATER
public partial interface ICsvReader : IDisposable, IAsyncDisposable
#else
public partial interface ICsvReader : IDisposable
#endif
{
    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    bool HasMoreData { get; }

    /// <summary>
    /// Total number of records processed so far
    /// </summary>
    int RecordCount { get; }

    /// <summary>
    /// Current position in the input data
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Read the next CSV record
    /// </summary>
    ICsvRecord ReadRecord();

    /// <summary>
    /// Try to read the next record, returns false if no more data
    /// </summary>
    bool TryReadRecord(out ICsvRecord record);

    /// <summary>
    /// Skip the next record without parsing it
    /// </summary>
    void SkipRecord();

    /// <summary>
    /// Skip multiple records
    /// </summary>
    void SkipRecords(int count);

    /// <summary>
    /// Get the current CSV options being used
    /// </summary>
    CsvOptions Options { get; }

    /// <summary>
    /// Reset the reader to the beginning
    /// </summary>
    void Reset();

    /// <summary>
    /// Read all records into a list
    /// </summary>
    /// <returns>List of all CSV records as string arrays</returns>
    IReadOnlyList<string[]> ReadAllRecords();

    /// <summary>
    /// Read all records asynchronously into a list
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all CSV records as string arrays</returns>
    Task<IReadOnlyList<string[]>> ReadAllRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get records as enumerable
    /// </summary>
    /// <returns>Enumerable of CSV records as string arrays</returns>
    IEnumerable<string[]> GetRecords();

    Task<IEnumerable<string[]>> GetRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Count the total number of records without parsing fields
    /// </summary>
    /// <returns>Total number of records</returns>
    int CountRecords();
    
    /// <summary>
    /// Enumerate records without allocating field arrays (for counting/validation only)
    /// </summary>
    /// <returns>Enumerable that only tracks line numbers without parsing</returns>
    IEnumerable<int> EnumerateWithoutParsing();
    
    /// <summary>
    /// Get zero-allocation enumerator that returns ref struct records
    /// </summary>
    /// <returns>Enumerator that returns CsvRow ref structs with span-based field access</returns>
    RowEnumerable EnumerateRows();
    
    /// <summary>
    /// Provides high-performance field iteration without allocations
    /// </summary>
    /// <returns>Iterator for efficient field-by-field processing</returns>
    CsvFieldIterator.CsvFieldCollection IterateFields();
    
    /// <summary>
    /// Get ultra-fast zero-allocation enumerator with pre-parsed field positions
    /// </summary>
    /// <returns>Enumerator that returns UltraFastCsvRow ref structs with optimized field access</returns>
    UltraFastRowEnumerable EnumerateRowsFast();
    
    /// <summary>
    /// Get Sep-style enumerator with always-SIMD, never-scalar optimizations
    /// </summary>
    /// <returns>Enumerator using Sep's optimization techniques for maximum performance</returns>
    SepStyleParser.SepStyleEnumerable EnumerateSepStyle();

    /// <summary>
    /// Get validation results if validation is enabled
    /// </summary>
    CsvValidationResult ValidationResult { get; }

    /// <summary>
    /// Whether validation is enabled
    /// </summary>
    bool IsValidationEnabled { get; }

    /// <summary>
    /// Whether error tracking is enabled
    /// </summary>
    bool IsErrorTrackingEnabled { get; }
}