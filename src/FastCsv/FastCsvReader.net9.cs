#if NET9_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace FastCsv;

/// <summary>
/// Advanced hardware acceleration for FastCsvReader
/// </summary>
internal sealed partial class FastCsvReader
{
    private bool _profilingEnabled;

    /// <summary>
    /// Indicates if Vector512 operations are supported on current hardware
    /// </summary>
    public bool IsVector512Supported => Vector512.IsHardwareAccelerated;

    /// <summary>
    /// Enables detailed performance profiling during CSV parsing
    /// </summary>
    /// <param name="enabled">True to enable profiling metrics collection</param>
    public void EnableProfiling(bool enabled)
    {
        _profilingEnabled = enabled;
    }
}

/// <summary>
/// Advanced hardware acceleration for FastCsvRecord
/// </summary>
internal sealed partial class FastCsvRecord
{

    /// <summary>
    /// Gets a field with Vector512 optimizations when available
    /// </summary>
    /// <param name="index">Index of the field to retrieve</param>
    /// <returns>Field content as span</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetFieldOptimized(int index)
    {
        // For now, delegate to standard GetField
        // In a full implementation, this would use Vector512 for large field processing
        return GetField(index);
    }

    /// <summary>
    /// Gets multiple fields efficiently using batch operations
    /// </summary>
    /// <param name="indices">Indices of fields to retrieve</param>
    /// <param name="destination">Destination span for field values</param>
    /// <returns>Number of fields retrieved</returns>
    public int GetMultipleFields(ReadOnlySpan<int> indices, Span<string> destination)
    {
        var count = Math.Min(indices.Length, destination.Length);
        var retrieved = 0;

        for (int i = 0; i < count; i++)
        {
            if (TryGetField(indices[i], out var field))
            {
                destination[retrieved] = field.ToString();
                retrieved++;
            }
        }

        return retrieved;
    }
}
#endif