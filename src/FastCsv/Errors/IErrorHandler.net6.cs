#if NET6_0_OR_GREATER
using System;

namespace FastCsv.Errors;

/// <summary>
/// Error statistics enhancements for IErrorHandler
/// </summary>
public partial interface IErrorHandler
{
    /// <summary>
    /// Get error statistics and counts
    /// </summary>
    CsvErrorStatistics GetErrorStatistics();

    /// <summary>
    /// Set error tolerance threshold
    /// </summary>
    void SetErrorThreshold(int maxErrors);

    /// <summary>
    /// Whether error threshold has been exceeded
    /// </summary>
    bool IsErrorThresholdExceeded { get; }
}

/// <summary>
/// Error statistics for CSV processing
/// </summary>
public readonly struct CsvErrorStatistics
{
    public int TotalErrors { get; }
    public int ErrorsByType(CsvErrorType type) => _errorCounts.TryGetValue(type, out var count) ? count : 0;
    public double ErrorRate { get; }
    public TimeSpan Duration { get; }

    private readonly Dictionary<CsvErrorType, int> _errorCounts;

    public CsvErrorStatistics(int totalErrors, Dictionary<CsvErrorType, int> errorCounts, double errorRate, TimeSpan duration)
    {
        TotalErrors = totalErrors;
        _errorCounts = errorCounts;
        ErrorRate = errorRate;
        Duration = duration;
    }
}
#endif