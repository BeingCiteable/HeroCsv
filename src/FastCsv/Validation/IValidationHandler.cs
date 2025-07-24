namespace FastCsv.Validation;

/// <summary>
/// Handles CSV validation operations and rules
/// </summary>
public interface IValidationHandler
{
    /// <summary>
    /// Whether validation is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Validates a CSV record
    /// </summary>
    /// <param name="fields">The fields in the record</param>
    /// <param name="lineNumber">Current line number</param>
    /// <param name="expectedFieldCount">Expected number of fields (null for first record)</param>
    void ValidateRecord(string[] fields, int lineNumber, int? expectedFieldCount);

    /// <summary>
    /// Gets the expected field count after processing first record
    /// </summary>
    int? ExpectedFieldCount { get; }

    /// <summary>
    /// Gets all validation errors found
    /// </summary>
    IReadOnlyList<CsvValidationError> GetErrors();

    /// <summary>
    /// Clears all validation errors
    /// </summary>
    void Reset();
}