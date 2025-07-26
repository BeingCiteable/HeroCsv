using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Provides string deduplication to reduce memory usage when reading CSV files with repeated values
/// </summary>
/// <remarks>
/// Creates a new string pool for deduplicating repeated string values
/// </remarks>
/// <param name="maxStringLength">Maximum length of strings to pool (longer strings won't be pooled)</param>
public sealed class StringPool(int maxStringLength = 100)
{
    private readonly ConcurrentDictionary<string, string> _pool = new(StringComparer.Ordinal);
    private readonly int _maxStringLength = maxStringLength;

    /// <summary>
    /// Gets or adds a string to the pool, returning the pooled instance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > _maxStringLength)
            return value;

        return _pool.GetOrAdd(value, value);
    }

    /// <summary>
    /// Gets or adds a string to the pool from a character span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe string GetString(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return string.Empty;
        if (span.Length > _maxStringLength)
        {
            // Create string directly without pooling
            fixed (char* ptr = span)
            {
                return new string(ptr, 0, span.Length);
            }
        }

        // For pooling, we need to create the string first
        string value;
        fixed (char* ptr = span)
        {
            value = new string(ptr, 0, span.Length);
        }

        return _pool.GetOrAdd(value, value);
    }

    /// <summary>
    /// Clears all entries from the pool
    /// </summary>
    public void Clear()
    {
        _pool.Clear();
    }

    /// <summary>
    /// Gets the current number of unique strings in the pool
    /// </summary>
    public int Count => _pool.Count;
}