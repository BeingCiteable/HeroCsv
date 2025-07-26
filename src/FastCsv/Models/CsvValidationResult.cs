namespace FastCsv;

/// <summary>
/// Represents the result of CSV validation including any errors found
/// </summary>
public class CsvValidationResult
{
    private readonly List<CsvValidationError> _errors = [];

    /// <summary>
    /// Whether the CSV is valid (no errors found)
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// All validation errors found
    /// </summary>
    public IReadOnlyList<CsvValidationError> Errors => _errors;

    /// <summary>
    /// Total number of errors
    /// </summary>
    public int ErrorCount => _errors.Count;

    /// <summary>
    /// Add a validation error
    /// </summary>
    internal void AddError(CsvValidationError error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Clear all errors
    /// </summary>
    internal void Clear()
    {
        _errors.Clear();
    }
}

/// <summary>
/// Represents a single CSV validation error
/// </summary>
public class CsvValidationError(
    CsvErrorType errorType,
    string message,
    int lineNumber,
    int fieldIndex = -1,
    string? content = null)
{
    /// <summary>
    /// Type of validation error
    /// </summary>
    public CsvErrorType ErrorType { get; } = errorType;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Line number where error occurred (1-based)
    /// </summary>
    public int LineNumber { get; } = lineNumber;

    /// <summary>
    /// Field index where error occurred (0-based, -1 if not field-specific)
    /// </summary>
    public int FieldIndex { get; } = fieldIndex;

    /// <summary>
    /// The problematic content if available
    /// </summary>
    public string? Content { get; } = content;
}

/// <summary>
/// Types of CSV validation errors
/// </summary>
public enum CsvErrorType
{
    /// <summary>
    /// Field contains unbalanced quotes
    /// </summary>
    UnbalancedQuotes,

    /// <summary>
    /// Record has incorrect number of fields
    /// </summary>
    InconsistentFieldCount,

    /// <summary>
    /// Empty field where value is required
    /// </summary>
    EmptyRequiredField,

    /// <summary>
    /// Field exceeds maximum length
    /// </summary>
    FieldTooLong,

    /// <summary>
    /// Invalid characters in field
    /// </summary>
    InvalidCharacters,

    /// <summary>
    /// Unexpected end of file
    /// </summary>
    UnexpectedEndOfFile,

    /// <summary>
    /// General parsing error
    /// </summary>
    ParsingError
}