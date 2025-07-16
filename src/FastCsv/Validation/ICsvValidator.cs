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
public readonly struct ValidationIssue
{
    public ValidationSeverity Severity { get; }
    public string Message { get; }
    public int Position { get; }
    public int LineNumber { get; }
    public int FieldIndex { get; }

    public ValidationIssue(ValidationSeverity severity, string message, int position, int lineNumber, int fieldIndex = -1)
    {
        Severity = severity;
        Message = message;
        Position = position;
        LineNumber = lineNumber;
        FieldIndex = fieldIndex;
    }
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

#if NET6_0_OR_GREATER
/// <summary>
/// Validation result for batch operations
/// </summary>
public readonly struct ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationIssue> Issues { get; }
    public int ValidRecords { get; }
    public int InvalidRecords { get; }
    public TimeSpan Duration { get; }

    public ValidationResult(bool isValid, IReadOnlyList<ValidationIssue> issues, int validRecords, int invalidRecords, TimeSpan duration)
    {
        IsValid = isValid;
        Issues = issues;
        ValidRecords = validRecords;
        InvalidRecords = invalidRecords;
        Duration = duration;
    }
}

#if NET8_0_OR_GREATER
/// <summary>
/// Validation rule for field validation
/// </summary>
public readonly struct ValidationRule
{
    public string Name { get; }
    public ValidationSeverity Severity { get; }
    public Func<string, bool> Validator { get; }
    public string ErrorMessage { get; }

    public ValidationRule(string name, ValidationSeverity severity, Func<string, bool> validator, string errorMessage)
    {
        Name = name;
        Severity = severity;
        Validator = validator;
        ErrorMessage = errorMessage;
    }
}
#endif
#endif