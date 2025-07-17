namespace FastCsv;

/// <summary>
/// Advanced result from CSV reading operations with performance optimizations
/// </summary>
public sealed partial class CsvReadResult
{
    private IReadOnlyList<string[]>? _records;
    private IReadOnlyList<string>? _validationErrors;
    private IReadOnlyDictionary<string, object>? _statistics;

    /// <summary>
    /// Parsed CSV records as arrays of field values
    /// </summary>
    public IReadOnlyList<string[]> Records
    {
        get => _records ?? [];
        set => _records = value;
    }

    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Indicates whether CSV validation passed without errors
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation errors encountered during processing
    /// </summary>
    public IReadOnlyList<string> ValidationErrors
    {
        get => _validationErrors ?? [];
        set => _validationErrors = value;
    }

    /// <summary>
    /// Time taken to process the CSV data
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Performance statistics and metrics from the parsing operation
    /// </summary>
    public IReadOnlyDictionary<string, object> Statistics
    {
        get => _statistics ?? EmptyStatistics;
        set => _statistics = value;
    }

    /// <summary>
    /// Indicates if any validation errors were encountered
    /// </summary>
    public bool HasValidationErrors => _validationErrors?.Count > 0;

    /// <summary>
    /// Count of validation errors (0 if none)
    /// </summary>
    public int ValidationErrorCount => _validationErrors?.Count ?? 0;

    /// <summary>
    /// Indicates if performance statistics are available
    /// </summary>
    public bool HasStatistics => _statistics?.Count > 0;

    /// <summary>
    /// Provides a cached, empty, read-only dictionary of statistics to avoid unnecessary allocations.
    /// </summary>
    /// <remarks>This dictionary is intended for scenarios where an empty set of statistics is required, and
    /// it ensures no additional memory allocations are performed. The dictionary is immutable and can be safely shared
    /// across multiple consumers.</remarks>
    private static readonly IReadOnlyDictionary<string, object> EmptyStatistics =
        new Dictionary<string, object>();

    /// <summary>
    /// Creates a successful result with records and processing time
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <returns>Successful CSV read result</returns>
    public static CsvReadResult Success(IReadOnlyList<string[]> records, TimeSpan processingTime)
    {
        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">List of validation errors</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <returns>Failed CSV read result</returns>
    public static CsvReadResult Failure(IReadOnlyList<string> validationErrors, TimeSpan processingTime)
    {
        return new CsvReadResult
        {
            IsValid = false,
            ValidationErrors = validationErrors,
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Creates a successful result with statistics
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="statistics">Performance statistics</param>
    /// <returns>Successful CSV read result with statistics</returns>
    public static CsvReadResult SuccessWithStatistics(
        IReadOnlyList<string[]> records,
        TimeSpan processingTime,
        IReadOnlyDictionary<string, object> statistics)
    {
        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime,
            Statistics = statistics
        };
    }
}

