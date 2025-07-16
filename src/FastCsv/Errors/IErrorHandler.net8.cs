#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Collections.Frozen;

namespace FastCsv.Errors;

/// <summary>
/// .NET 8+ error collection enhancements for IErrorHandler
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