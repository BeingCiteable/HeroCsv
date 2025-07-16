#if NET9_0_OR_GREATER
using System;

namespace FastCsv;

/// <summary>
/// .NET 9+ advanced field operations enhancements for ICsvRecord
/// </summary>
public partial interface ICsvRecord
{
    /// <summary>
    /// Get field with advanced optimization for large fields
    /// </summary>
    ReadOnlySpan<char> GetFieldOptimized(int index);
    
    /// <summary>
    /// Get multiple fields efficiently
    /// </summary>
    int GetMultipleFields(ReadOnlySpan<int> indices, Span<string> fields);
}
#endif