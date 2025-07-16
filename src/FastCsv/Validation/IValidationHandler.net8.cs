#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace FastCsv.Validation;

/// <summary>
/// .NET 8+ fast character detection enhancements for IValidationHandler
/// </summary>
public partial interface IValidationHandler
{
    /// <summary>
    /// Validate CSV structure with optimized character detection
    /// </summary>
    bool IsValidCsvStructureOptimized(ReadOnlySpan<char> data, SearchValues<char> invalidChars);
    
    /// <summary>
    /// Validate field with optimized special character detection
    /// </summary>
    bool IsValidFieldOptimized(ReadOnlySpan<char> field, SearchValues<char> specialChars);
}
#endif