namespace FastCsv;

/// <summary>
/// Configuration options for controlling what details are collected during CSV reading operations
/// </summary>
public record CsvReadDetailsOptions
{
    /// <summary>
    /// Gets or sets whether to collect basic statistics like record count and processing time
    /// </summary>
    public bool IncludeBasicStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect performance metrics like records per second
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect field-level statistics like average fields per record
    /// </summary>
    public bool IncludeFieldStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track validation errors during reading
    /// </summary>
    public bool TrackValidationErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to perform data validation during reading
    /// </summary>
    public bool ValidateData { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include hardware acceleration information in statistics
    /// </summary>
    public bool IncludeHardwareInfo { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include memory usage statistics
    /// </summary>
    public bool IncludeMemoryStatistics { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of validation errors to collect (0 = unlimited)
    /// </summary>
    public int MaxValidationErrors { get; set; } = 100;

    /// <summary>
    /// Gets a preset configuration with all details enabled
    /// </summary>
    public static CsvReadDetailsOptions Full => new()
    {
        IncludeBasicStatistics = true,
        IncludePerformanceMetrics = true,
        IncludeFieldStatistics = true,
        TrackValidationErrors = true,
        IncludeHardwareInfo = true,
        IncludeMemoryStatistics = true,
        ValidateData = true,
        MaxValidationErrors = 0
    };

    /// <summary>
    /// Gets a preset configuration with minimal details (only basic statistics)
    /// </summary>
    public static CsvReadDetailsOptions Minimal => new()
    {
        IncludeBasicStatistics = true,
        IncludePerformanceMetrics = false,
        IncludeFieldStatistics = false,
        TrackValidationErrors = false,
        IncludeHardwareInfo = false,
        IncludeMemoryStatistics = false,
        ValidateData = false,
        MaxValidationErrors = 0
    };

    /// <summary>
    /// Gets a preset configuration focused on performance analysis
    /// </summary>
    public static CsvReadDetailsOptions Performance => new()
    {
        IncludeBasicStatistics = true,
        IncludePerformanceMetrics = true,
        IncludeFieldStatistics = false,
        TrackValidationErrors = false,
        IncludeHardwareInfo = true,
        IncludeMemoryStatistics = true,
        ValidateData = false,
        MaxValidationErrors = 0
    };

    /// <summary>
    /// Gets a preset configuration focused on data validation
    /// </summary>
    public static CsvReadDetailsOptions Validation => new()
    {
        IncludeBasicStatistics = true,
        IncludePerformanceMetrics = false,
        IncludeFieldStatistics = true,
        TrackValidationErrors = true,
        IncludeHardwareInfo = false,
        IncludeMemoryStatistics = false,
        ValidateData = true,
        MaxValidationErrors = 0
    };

    /// <summary>
    /// Gets the default configuration (balanced between performance and information)
    /// </summary>
    public static CsvReadDetailsOptions Default => new();
}