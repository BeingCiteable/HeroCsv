#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Frozen;

namespace FastCsv;

/// <summary>
/// .NET 8+ SearchValues and Frozen Collections enhancements for ICsvReader
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Auto-detect the CSV format from sample data
    /// </summary>
    CsvOptions DetectFormat();
    
    /// <summary>
    /// Find fields by name using header information
    /// </summary>
    bool TryGetFieldByName(string fieldName, out ReadOnlySpan<char> field);
    
    /// <summary>
    /// Get all field names from header
    /// </summary>
    FrozenSet<string> GetFieldNames();
}
#endif