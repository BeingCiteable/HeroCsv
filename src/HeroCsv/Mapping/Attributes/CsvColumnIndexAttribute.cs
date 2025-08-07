using System;

namespace HeroCsv.Mapping.Attributes;

/// <summary>
/// Specifies the column index for CSV mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class CsvColumnIndexAttribute : Attribute
{
    /// <summary>
    /// Gets the column index (0-based)
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new instance of the CsvColumnIndexAttribute class
    /// </summary>
    /// <param name="index">The 0-based column index to map to this property</param>
    public CsvColumnIndexAttribute(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index must be non-negative");
        
        Index = index;
    }
}