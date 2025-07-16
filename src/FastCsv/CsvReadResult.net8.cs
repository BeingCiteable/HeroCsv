#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace FastCsv;

/// <summary>
/// NET8+ optimizations for CsvReadResult
/// </summary>
public sealed partial class CsvReadResult
{
    /// <summary>
    /// Creates a successful result with frozen statistics for maximum performance
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="statistics">Performance statistics</param>
    /// <returns>Successful CSV read result with frozen statistics</returns>
    public static CsvReadResult SuccessWithFrozenStatistics(
        IReadOnlyList<string[]> records,
        TimeSpan processingTime,
        IDictionary<string, object> statistics)
    {
        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime,
            Statistics = statistics.ToFrozenDictionary()
        };
    }

    /// <summary>
    /// Creates a failed result with frozen validation errors for optimal memory usage
    /// </summary>
    /// <param name="validationErrors">List of validation errors</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <returns>Failed CSV read result with frozen errors</returns>
    public static CsvReadResult FailureWithFrozenErrors(
        IEnumerable<string> validationErrors,
        TimeSpan processingTime)
    {
        return new CsvReadResult
        {
            IsValid = false,
            ValidationErrors = validationErrors.ToFrozenSet().AsEnumerable().ToArray(),
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Gets statistics optimized for repeated access using frozen collections
    /// </summary>
    /// <returns>Frozen dictionary of statistics</returns>
    public FrozenDictionary<string, object> GetFrozenStatistics()
    {
        return _statistics switch
        {
            FrozenDictionary<string, object> frozen => frozen,
            null => FrozenDictionary<string, object>.Empty,
            _ => _statistics.ToFrozenDictionary()
        };
    }

    /// <summary>
    /// Gets validation errors as a frozen set for efficient lookups
    /// </summary>
    /// <returns>Frozen set of validation errors</returns>
    public FrozenSet<string> GetFrozenValidationErrors()
    {
        return _validationErrors switch
        {
            null => FrozenSet<string>.Empty,
            _ => _validationErrors.ToFrozenSet()
        };
    }
}
#endif