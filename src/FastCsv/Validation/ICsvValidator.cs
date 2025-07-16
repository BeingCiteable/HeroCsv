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

#if NET6_0_OR_GREATER
/// <summary>
/// Validation result for batch operations
/// </summary>
public readonly struct ValidationResult(
    bool isValid,
    IReadOnlyList<ValidationIssue> issues,
    int validRecords,
    int invalidRecords,
    TimeSpan duration)
{
    public bool IsValid { get; } = isValid;
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
    public int ValidRecords { get; } = validRecords;
    public int InvalidRecords { get; } = invalidRecords;
    public TimeSpan Duration { get; } = duration;
}
#endif

#if NET8_0_OR_GREATER
/// <summary>
/// Validation rule for field validation
/// </summary>
public readonly struct ValidationRule(
    string name,
    ValidationSeverity severity,
    Func<string, bool> validator,
    string errorMessage)
{
    public string Name { get; } = name;
    public ValidationSeverity Severity { get; } = severity;
    public Func<string, bool> Validator { get; } = validator;
    public string ErrorMessage { get; } = errorMessage;
}
#endif