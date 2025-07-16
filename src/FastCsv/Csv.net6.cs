#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace FastCsv;

/// <summary>
/// Hardware acceleration enhancements for Csv
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Counts fields in a CSV line using vector operations for better performance
    /// </summary>
    /// <param name="line">CSV line to analyze</param>
    /// <param name="delimiter">Field separator character</param>
    /// <returns>Number of fields in the line</returns>
    public static int CountFields(ReadOnlySpan<char> line, char delimiter)
    {
        if (line.IsEmpty) return 0;
        
        var count = 1; // At least one field
        var inQuotes = false;
        
        if (Vector.IsHardwareAccelerated && line.Length >= Vector<ushort>.Count)
        {
            return CountFieldsVectorized(line, delimiter);
        }
        
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == delimiter && !inQuotes) count++;
        }
        
        return count;
    }
    
    private static int CountFieldsVectorized(ReadOnlySpan<char> line, char delimiter)
    {
        var count = 1;
        var delimiterVector = new Vector<ushort>(delimiter);
        var position = 0;
        var inQuotes = false;
        
        while (position <= line.Length - Vector<ushort>.Count)
        {
            var chunk = line.Slice(position, Vector<ushort>.Count);
            // Convert ReadOnlySpan<char> to ushort array for Vector constructor
            var ushortArray = new ushort[Vector<ushort>.Count];
            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                ushortArray[i] = (ushort)chunk[i];
            }
            var vector = new Vector<ushort>(ushortArray);
            var matches = Vector.Equals(vector, delimiterVector);
            
            if (Vector.EqualsAny(matches, Vector<ushort>.Zero))
            {
                // Fall back to scalar processing for this chunk
                for (int i = 0; i < Vector<ushort>.Count; i++)
                {
                    var ch = line[position + i];
                    if (ch == '"') inQuotes = !inQuotes;
                    else if (ch == delimiter && !inQuotes) count++;
                }
            }
            
            position += Vector<ushort>.Count;
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
    /// Checks if hardware acceleration is available on current system
    /// </summary>
    /// <returns>True if SIMD operations are supported</returns>
    public static bool IsHardwareAccelerated => Vector.IsHardwareAccelerated;
    
    /// <summary>
    /// Gets optimal buffer size for current hardware configuration
    /// </summary>
    /// <returns>Recommended buffer size in characters</returns>
    public static int GetOptimalBufferSize()
    {
        return Vector.IsHardwareAccelerated ? 8192 : 4096;
    }
}
#endif