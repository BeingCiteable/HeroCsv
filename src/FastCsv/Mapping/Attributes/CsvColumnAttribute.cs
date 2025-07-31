using System;

namespace FastCsv.Mapping.Attributes;

/// <summary>
/// Specifies the CSV column mapping for a property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class CsvColumnAttribute : Attribute
{
    /// <summary>
    /// Gets the column name
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets or sets the column index (0-based)
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets whether the index was explicitly set
    /// </summary>
    public bool HasIndex { get; }

    /// <summary>
    /// Gets or sets the default value when the field is empty or missing
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// Gets or sets the format string for parsing (e.g., date formats)
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore this property during mapping
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Maps property to a column by name
    /// </summary>
    /// <param name="name">The CSV column name</param>
    public CsvColumnAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Index = -1;
        HasIndex = false;
    }

    /// <summary>
    /// Maps property to a column by index
    /// </summary>
    /// <param name="index">The CSV column index (0-based)</param>
    public CsvColumnAttribute(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index must be non-negative");
        
        Index = index;
        HasIndex = true;
    }

    /// <summary>
    /// Maps property to a column with both name and index
    /// </summary>
    public CsvColumnAttribute()
    {
        Index = -1;
        HasIndex = false;
    }
}