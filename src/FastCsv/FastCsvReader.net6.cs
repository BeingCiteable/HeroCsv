#if NET6_0_OR_GREATER
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Hardware acceleration features for FastCsvReader
/// </summary>
internal sealed partial class FastCsvReader
{
    /// <summary>
    /// Indicates if hardware acceleration is available on current system
    /// </summary>
    public bool IsHardwareAccelerated => Vector.IsHardwareAccelerated;

    /// <summary>
    /// Configure vectorization settings for performance optimization
    /// </summary>
    /// <param name="enabled">True to enable vectorization when available</param>
    public void SetVectorizationEnabled(bool enabled)
    {
        // Implementation would configure vectorization settings
        // For now, this is a placeholder as vectorization is automatically used when available
    }

    /// <summary>
    /// Gets optimal buffer size for current hardware configuration
    /// </summary>
    /// <returns>Recommended buffer size in characters</returns>
    public int GetOptimalBufferSize()
    {
        return Vector.IsHardwareAccelerated ? 8192 : 4096;
    }
}

/// <summary>
/// Hardware acceleration features for FastCsvRecord
/// </summary>
internal sealed partial class FastCsvRecord
{
    // No additional NET6+ specific methods needed for ICsvRecord
}
#endif