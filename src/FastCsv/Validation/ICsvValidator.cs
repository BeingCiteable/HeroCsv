namespace FastCsv.Validation;

/// <summary>
/// Interface for CSV validation operations
/// Separated from reading for single responsibility
/// </summary>
public partial interface ICsvValidator
{
    /// <summary>
    /// Check if a record has the expected number of fields
    /// </summary>
    bool IsValidRecord(ICsvRecord record, int expectedFieldCount);

    /// <summary>
    /// Check if a record is properly formatted
    /// </summary>
    bool IsWellFormedRecord(ICsvRecord record);

    /// <summary>
    /// Validate a specific field
    /// </summary>
    bool IsValidField(ReadOnlySpan<char> field);

    /// <summary>
    /// Validate the overall CSV structure
    /// </summary>
    bool ValidateStructure(ReadOnlySpan<char> data, CsvOptions options);

    /// <summary>
    /// Check if quotes are properly balanced in a field
    /// </summary>
    bool HasBalancedQuotes(ReadOnlySpan<char> field, char quoteChar);

    /// <summary>
    /// Get validation issues for a record
    /// </summary>
    IReadOnlyList<ValidationIssue> GetValidationIssues(ICsvRecord record);
}

/// <summary>
/// Represents a validation issue
/// </summary>
public readonly struct ValidationIssue(
    ValidationSeverity severity,
    string message,
    int position,
    int lineNumber,
    int fieldIndex = -1)
{
    public ValidationSeverity Severity { get; } = severity;
    public string Message { get; } = message;
    public int Position { get; } = position;
    public int LineNumber { get; } = lineNumber;
    public int FieldIndex { get; } = fieldIndex;
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}