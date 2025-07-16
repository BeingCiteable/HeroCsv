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
    UnbalancedQuotes,
    MalformedRecord,
    UnexpectedEndOfData,
    InvalidFieldIndex,
    InvalidConfiguration,
    ParsingError,
    ValidationError
}

/// <summary>
/// Represents a CSV processing error
/// </summary>
public readonly struct CsvError
{
    public CsvErrorType Type { get; }
    public string Message { get; }
    public int Position { get; }
    public int LineNumber { get; }
    public DateTime Timestamp { get; }

    public CsvError(CsvErrorType type, string message, int position, int lineNumber)
    {
        Type = type;
        Message = message;
        Position = position;
        LineNumber = lineNumber;
        Timestamp = DateTime.UtcNow;
    }
}