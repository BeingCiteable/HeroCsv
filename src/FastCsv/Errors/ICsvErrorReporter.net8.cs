#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Collections.Frozen;

namespace FastCsv.Errors;

/// <summary>
/// .NET 8+ frozen collections enhancements for ICsvErrorReporter
/// </summary>
public partial interface ICsvErrorReporter
{
    /// <summary>
    /// Get errors grouped by severity
    /// </summary>
    FrozenDictionary<ErrorSeverity, IReadOnlyList<CsvReportedError>> GetErrorsBySeverity();
    
    /// <summary>
    /// Get unique error messages
    /// </summary>
    FrozenSet<string> GetUniqueErrorMessages();
    
    /// <summary>
    /// Get unique warning messages
    /// </summary>
    FrozenSet<string> GetUniqueWarningMessages();
}
#endif