#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace FastCsv.Errors;

/// <summary>
/// Advanced collections enhancements for ICsvErrorReporter
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