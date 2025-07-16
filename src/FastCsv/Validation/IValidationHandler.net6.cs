#if NET6_0_OR_GREATER
using System;
using System.Numerics;

namespace FastCsv.Validation;

/// <summary>
/// .NET 6+ hardware acceleration enhancements for IValidationHandler
/// </summary>
public partial interface IValidationHandler
{
    /// <summary>
    /// Validate record structure with hardware acceleration
    /// </summary>
    bool IsValidRecordAccelerated(ReadOnlySpan<char> record, CsvOptions options);
    
    /// <summary>
    /// Check quote balance with hardware acceleration
    /// </summary>
    bool HasBalancedQuotesAccelerated(ReadOnlySpan<char> field, char quoteChar);
}
#endif