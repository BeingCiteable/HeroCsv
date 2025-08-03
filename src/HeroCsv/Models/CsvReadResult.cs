namespace HeroCsv.Models;

/// <summary>
/// Contains the results of a CSV read operation including records and validation information
/// </summary>
public class CsvReadResult(
    IReadOnlyList<string[]> records,
    int recordCount,
    int lineCount,
    CsvValidationResult? validationResult = null,
    bool validationPerformed = false,
    bool errorTrackingEnabled = false)
{
    /// <summary>
    /// All records read from the CSV
    /// </summary>
    public IReadOnlyList<string[]> Records { get; } = records;

    /// <summary>
    /// Validation result if validation was enabled
    /// </summary>
    public CsvValidationResult? ValidationResult { get; } = validationResult;

    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int RecordCount { get; } = recordCount;

    /// <summary>
    /// Total number of lines processed
    /// </summary>
    public int LineCount { get; } = lineCount;

    /// <summary>
    /// Whether validation was performed
    /// </summary>
    public bool ValidationPerformed { get; } = validationPerformed;

    /// <summary>
    /// Whether error tracking was enabled
    /// </summary>
    public bool ErrorTrackingEnabled { get; } = errorTrackingEnabled;
}