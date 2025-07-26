#if NET6_0_OR_GREATER
using System;

namespace FastCsv.Builder;

/// <summary>
/// Optimization options for ICsvReaderBuilder
/// </summary>
public partial interface ICsvReaderBuilder
{
    /// <summary>
    /// Enables CPU vector instructions for faster parsing of large files
    /// </summary>
    /// <param name="enabled">True to use SIMD operations when available</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithHardwareAcceleration(bool enabled = true);
}

#endif