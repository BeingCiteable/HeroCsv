namespace FastCsv.Errors;

/// <summary>
/// Handler responsible for error management and reporting
/// Core responsibility: Track and report errors during CSV processing
/// </summary>
public partial interface IErrorHandler
{
    /// <summary>
    /// Whether any errors have been reported
    /// </summary>
    bool HasError { get; }

    /// <summary>
    /// Last error message
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Report an error
    /// </summary>
    void ReportError(CsvErrorType type, string message, int position, int lineNumber);

    /// <summary>
    /// Clear all errors
    /// </summary>
    void ClearErrors();

    /// <summary>
    /// Get all reported errors
    /// </summary>
    IReadOnlyList<CsvError> GetErrors();
}

/// <summary>
/// Types of CSV errors
/// </summary>
public enum CsvErrorType
{
    UnbalancedQuotes = 1,
    MalformedRecord = 2,
    UnexpectedEndOfData = 3,
    InvalidFieldIndex = 4,
    InvalidConfiguration = 5,
    ParsingError = 6,
    ValidationError = 7
}

/// <summary>
/// Represents a CSV processing error
/// </summary>
public readonly struct CsvError(
    CsvErrorType type,
    string message,
    int position,
    int lineNumber)
{
    public CsvErrorType Type { get; } = type;
    public string Message { get; } = message;
    public int Position { get; } = position;
    public int LineNumber { get; } = lineNumber;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}