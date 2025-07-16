#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;

namespace FastCsv.Validation;

/// <summary>
/// Hardware acceleration validation enhancements for ICsvValidator
/// </summary>
public partial interface ICsvValidator
{
    /// <summary>
    /// Validate record structure with hardware acceleration
    /// </summary>
    bool IsValidRecordVectorized(ReadOnlySpan<char> recordData, CsvOptions options);

    /// <summary>
    /// Check quote balance using hardware acceleration
    /// </summary>
    bool HasBalancedQuotesVectorized(ReadOnlySpan<char> field, char quoteChar);

    /// <summary>
    /// Validate multiple records efficiently
    /// </summary>
    ValidationResult ValidateRecordsBatch(IReadOnlyList<string> records, CsvOptions options);
}

/// <summary>
/// Validation result for batch operations
/// </summary>
public readonly struct ValidationResult(
    bool isValid,
    IReadOnlyList<ValidationIssue> issues,
    int validRecords,
    int invalidRecords,
    TimeSpan duration)
{
    public bool IsValid { get; } = isValid;
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
    public int ValidRecords { get; } = validRecords;
    public int InvalidRecords { get; } = invalidRecords;
    public TimeSpan Duration { get; } = duration;
}
#endif