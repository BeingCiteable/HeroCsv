#if NET6_0_OR_GREATER
using System;

namespace FastCsv.Errors;

/// <summary>
/// .NET 6+ error statistics enhancements for IErrorHandler
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
#endif