#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace FastCsv.Fields;

/// <summary>
/// Optimized character detection enhancements for IFieldHandler
/// </summary>
public partial interface IFieldHandler
{
    /// <summary>
    /// Get optimized delimiters for fast detection
    /// </summary>
    SearchValues<char> GetOptimizedDelimiters(CsvOptions options);

    /// <summary>
    /// Create optimized character set for field parsing with the given delimiters
    /// </summary>
    SearchValues<char> CreateSearchValues(ReadOnlySpan<char> delimiters);
}
#endif