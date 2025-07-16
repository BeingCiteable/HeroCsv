using System;
using System.Collections.Generic;

namespace FastCsv.Errors;

/// <summary>
/// Interface for CSV error reporting and management
/// Separated from reading for single responsibility
/// </summary>
public partial interface ICsvErrorReporter
{
    /// <summary>
    /// Whether any errors have been reported
    /// </summary>
    bool HasErrors { get; }
    
    /// <summary>
    /// Last error message
    /// </summary>
    string? LastError { get; }
    
    /// <summary>
    /// Clear all reported errors
    /// </summary>
    void ClearErrors();
    
    /// <summary>
    /// Report an error
    /// </summary>
    void ReportError(string message, int position, int lineNumber);
    
    /// <summary>
    /// Report a warning
    /// </summary>
    void ReportWarning(string message, int position, int lineNumber);
    
    /// <summary>
    /// Get all reported errors
    /// </summary>
    IReadOnlyList<CsvReportedError> GetErrors();
    
    /// <summary>
    /// Get all reported warnings
    /// </summary>
    IReadOnlyList<CsvReportedError> GetWarnings();
}

/// <summary>
/// Represents a reported CSV error or warning
/// </summary>
public readonly struct CsvReportedError
{
    public string Message { get; }
    public int Position { get; }
    public int LineNumber { get; }
    public ErrorSeverity Severity { get; }
    public DateTime Timestamp { get; }
    
    public CsvReportedError(string message, int position, int lineNumber, ErrorSeverity severity)
    {
        Message = message;
        Position = position;
        LineNumber = lineNumber;
        Severity = severity;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    Warning,
    Error,
    Critical
}

