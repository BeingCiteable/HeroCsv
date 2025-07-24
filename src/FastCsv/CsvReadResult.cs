namespace FastCsv;

/// <summary>
/// Contains the results of a CSV read operation including records and validation information
/// </summary>
public class CsvReadResult
{
    /// <summary>
    /// All records read from the CSV
    /// </summary>
    public IReadOnlyList<string[]> Records { get; }

    /// <summary>
    /// Validation result if validation was enabled
    /// </summary>
    public CsvValidationResult? ValidationResult { get; }

    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int RecordCount { get; }

    /// <summary>
    /// Total number of lines processed
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Whether validation was performed
    /// </summary>
    public bool ValidationPerformed { get; }

    /// <summary>
    /// Whether error tracking was enabled
    /// </summary>
    public bool ErrorTrackingEnabled { get; }

    public CsvReadResult(
        IReadOnlyList<string[]> records,
        int recordCount,
        int lineCount,
        CsvValidationResult? validationResult = null,
        bool validationPerformed = false,
        bool errorTrackingEnabled = false)
    {
        Records = records;
        RecordCount = recordCount;
        LineCount = lineCount;
        ValidationResult = validationResult;
        ValidationPerformed = validationPerformed;
        ErrorTrackingEnabled = errorTrackingEnabled;
    }
}