#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace FastCsv.Errors;

/// <summary>
/// Advanced error collection enhancements for IErrorHandler
/// </summary>
public partial interface IErrorHandler
{
    /// <summary>
    /// Get errors grouped by type
    /// </summary>
    FrozenDictionary<CsvErrorType, IReadOnlyList<CsvError>> GetErrorsByType();

    /// <summary>
    /// Get unique error messages
    /// </summary>
    FrozenSet<string> GetUniqueErrorMessages();
}
#endif