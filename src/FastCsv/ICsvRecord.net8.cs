#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;

namespace FastCsv;

/// <summary>
/// .NET 8+ named field access enhancements for ICsvRecord
/// </summary>
public partial interface ICsvRecord
{
    /// <summary>
    /// Get field by name using header information
    /// </summary>
    bool TryGetFieldByName(string fieldName, out ReadOnlySpan<char> field);
    
    /// <summary>
    /// Get all field names from header
    /// </summary>
    FrozenSet<string> GetFieldNames();
    
    /// <summary>
    /// Get field index by name
    /// </summary>
    bool TryGetFieldIndex(string fieldName, out int index);
}
#endif