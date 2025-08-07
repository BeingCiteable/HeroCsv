using System;

namespace HeroCsv.Mapping.Attributes;

/// <summary>
/// Specifies a custom column name for CSV mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class CsvColumnNameAttribute : Attribute
{
    /// <summary>
    /// Gets the column name to use for mapping
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the CsvColumnNameAttribute class
    /// </summary>
    /// <param name="name">The column name to map to this property</param>
    public CsvColumnNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}