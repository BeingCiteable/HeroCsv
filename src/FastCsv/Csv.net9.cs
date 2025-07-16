#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace FastCsv;

/// <summary>
/// Vector512 optimizations for Csv
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Ultra-fast field counting using Vector512 operations
    /// </summary>
    /// <param name="line">CSV line to analyze</param>
    /// <param name="delimiter">Field separator character</param>
    /// <returns>Number of fields in the line</returns>
    public static int CountFieldsVector512(ReadOnlySpan<char> line, char delimiter)
    {
        if (line.IsEmpty) return 0;

        if (Vector512.IsHardwareAccelerated && line.Length >= Vector512<ushort>.Count)
        {
            return CountFieldsVector512Internal(line, delimiter);
        }

        // Fall back to Vector256 or scalar
        return CountFields(line, delimiter);
    }

    private static int CountFieldsVector512Internal(ReadOnlySpan<char> line, char delimiter)
    {
        var count = 1;
        var delimiterVector = Vector512.Create((ushort)delimiter);
        var position = 0;
        var inQuotes = false;

        while (position <= line.Length - Vector512<ushort>.Count)
        {
            var chunk = line.Slice(position, Vector512<ushort>.Count);
            // Convert ReadOnlySpan<char> to ushort array for Vector512 constructor
            var ushortArray = new ushort[Vector512<ushort>.Count];
            for (int i = 0; i < Vector512<ushort>.Count; i++)
            {
                ushortArray[i] = (ushort)chunk[i];
            }
            var vector = Vector512.Create(ushortArray);
            var matches = Vector512.Equals(vector, delimiterVector);

            if (!Vector512.EqualsAll(matches, Vector512<ushort>.Zero))
            {
                // Process matches in this chunk
                for (int i = 0; i < Vector512<ushort>.Count; i++)
                {
                    var ch = line[position + i];
                    if (ch == '"') inQuotes = !inQuotes;
                    else if (ch == delimiter && !inQuotes) count++;
                }
            }

            position += Vector512<ushort>.Count;
        }

        // Process remaining characters
        for (int i = position; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == delimiter && !inQuotes) count++;
        }

        return count;
    }

    /// <summary>
    /// Checks if Vector512 operations are supported on current hardware
    /// </summary>
    /// <returns>True if Vector512 SIMD operations are available</returns>
    public static bool IsVector512Supported => Vector512.IsHardwareAccelerated;

    /// <summary>
    /// Gets optimal buffer size for Vector512 operations
    /// </summary>
    /// <returns>Recommended buffer size optimized for Vector512</returns>
    public static int GetOptimalVector512BufferSize()
    {
        return Vector512.IsHardwareAccelerated ? 16384 : GetOptimalBufferSize();
    }

    /// <summary>
    /// Advanced CSV parsing with Vector512 optimization and profiling
    /// </summary>
    /// <param name="csvContent">CSV content as memory span</param>
    /// <param name="options">Parsing configuration</param>
    /// <param name="enableProfiling">Enable detailed performance monitoring</param>
    /// <returns>Parsed records with performance metrics</returns>
    public static (IEnumerable<string[]> records, Dictionary<string, object> metrics) ReadWithProfiling(
        ReadOnlySpan<char> csvContent,
        CsvOptions options,
        bool enableProfiling = true)
    {
        var metrics = new Dictionary<string, object>();

        if (enableProfiling)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var records = ReadInternal(csvContent, options);
            stopwatch.Stop();

            metrics["ProcessingTime"] = stopwatch.Elapsed;
            metrics["Vector512Supported"] = Vector512.IsHardwareAccelerated;
            metrics["ContentLength"] = csvContent.Length;
            metrics["BufferSize"] = GetOptimalVector512BufferSize();

            return (records, metrics);
        }

        return (ReadInternal(csvContent, options), metrics);
    }
}
#endif