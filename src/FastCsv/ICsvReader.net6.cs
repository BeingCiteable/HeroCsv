#if NET6_0_OR_GREATER
using System;
using System.Numerics;

namespace FastCsv;

/// <summary>
/// .NET 6+ performance enhancements for ICsvReader
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Whether hardware acceleration is available and being used
    /// </summary>
    bool IsHardwareAccelerated { get; }
    
    /// <summary>
    /// Enable or disable hardware acceleration
    /// </summary>
    void SetVectorizationEnabled(bool enabled);
    
    /// <summary>
    /// Get the optimal buffer size for the current hardware
    /// </summary>
    int GetOptimalBufferSize();
}
#endif