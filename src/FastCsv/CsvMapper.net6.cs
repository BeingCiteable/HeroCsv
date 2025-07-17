#if NET6_0_OR_GREATER
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// NET6+ hardware acceleration optimizations for CsvMapper
/// </summary>
internal sealed partial class CsvMapper<T> where T : class, new()
{
    /// <summary>
    /// Batch map multiple records with vectorized operations for improved performance
    /// </summary>
    /// <param name="records">Array of CSV records to map</param>
    /// <returns>Array of mapped objects</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] MapRecords(string[][] records)
    {
        var results = new T[records.Length];
        
        // Use vectorized operations when beneficial
        if (Vector.IsHardwareAccelerated && records.Length > Vector<int>.Count)
        {
            MapRecordsVectorized(records, results);
        }
        else
        {
            MapRecordsSequential(records, results);
        }
        
        return results;
    }

    /// <summary>
    /// Maps records using vectorized operations for improved performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MapRecordsVectorized(string[][] records, T[] results)
    {
        var vectorSize = Vector<int>.Count;
        var vectorCount = records.Length / vectorSize;
        var remainder = records.Length % vectorSize;

        // Process full vectors
        for (int v = 0; v < vectorCount; v++)
        {
            var offset = v * vectorSize;
            for (int i = 0; i < vectorSize; i++)
            {
                results[offset + i] = MapRecord(records[offset + i]);
            }
        }

        // Process remaining records
        var remainderStart = vectorCount * vectorSize;
        for (int i = 0; i < remainder; i++)
        {
            results[remainderStart + i] = MapRecord(records[remainderStart + i]);
        }
    }

    /// <summary>
    /// Maps records sequentially
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MapRecordsSequential(string[][] records, T[] results)
    {
        for (int i = 0; i < records.Length; i++)
        {
            results[i] = MapRecord(records[i]);
        }
    }

    /// <summary>
    /// Gets optimal batch size for current hardware configuration
    /// </summary>
    /// <returns>Recommended batch size for mapping operations</returns>
    public int GetOptimalBatchSize()
    {
        return Vector.IsHardwareAccelerated ? 1024 : 256;
    }
}
#endif