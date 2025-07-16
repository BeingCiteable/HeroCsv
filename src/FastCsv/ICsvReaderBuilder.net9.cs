#if NET9_0_OR_GREATER
using System;

namespace FastCsv;

/// <summary>
/// Performance monitoring options for ICsvReaderBuilder
/// </summary>
public partial interface ICsvReaderBuilder
{
    /// <summary>
    /// Enables detailed timing and throughput measurements during parsing
    /// </summary>
    /// <param name="enabled">True to collect performance metrics</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithProfiling(bool enabled = true);
}
#endif