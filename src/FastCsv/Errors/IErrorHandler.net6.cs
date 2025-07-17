#if NET6_0_OR_GREATER
using System;

namespace FastCsv.Errors;

/// <summary>
/// Error handling enhancements for IErrorHandler
/// </summary>
public partial interface IErrorHandler
{
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