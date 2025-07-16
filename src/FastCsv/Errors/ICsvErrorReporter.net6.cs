#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace FastCsv.Errors;

/// <summary>
/// .NET 6+ error reporting statistics enhancements for ICsvErrorReporter
/// </summary>
public partial interface ICsvErrorReporter
{
    /// <summary>
    /// Get error reporting statistics
    /// </summary>
    ErrorReportingStatistics GetReportingStatistics();
}

/// <summary>
/// Error reporting statistics
/// </summary>
public readonly struct ErrorReportingStatistics
{
    public int TotalErrors { get; }
    public int TotalWarnings { get; }
    public int UniqueErrors { get; }
    public double ErrorRate { get; }
    public TimeSpan ReportingDuration { get; }
    public Dictionary<int, int> ErrorsByLine { get; }
    
    public ErrorReportingStatistics(int totalErrors, int totalWarnings, int uniqueErrors, double errorRate, TimeSpan reportingDuration, Dictionary<int, int> errorsByLine)
    {
        TotalErrors = totalErrors;
        TotalWarnings = totalWarnings;
        UniqueErrors = uniqueErrors;
        ErrorRate = errorRate;
        ReportingDuration = reportingDuration;
        ErrorsByLine = errorsByLine;
    }
}
#endif