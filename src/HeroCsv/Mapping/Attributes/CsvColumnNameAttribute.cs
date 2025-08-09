using System;

namespace HeroCsv.Mapping.Attributes;

/// <summary>
/// Specifies a custom column name for CSV mapping
/// </summary>
/// <remarks>
/// Initializes a new instance of the CsvColumnNameAttribute class
/// </remarks>
/// <param name="name">The column name to map to this property</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class CsvColumnNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the column name to use for mapping
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}