namespace HeroCsv.Models;

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