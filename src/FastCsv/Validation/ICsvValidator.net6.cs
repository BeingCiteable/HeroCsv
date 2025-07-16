#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;

namespace FastCsv.Validation;

/// <summary>
/// .NET 6+ vectorized validation enhancements for ICsvValidator
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
#endif