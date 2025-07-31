using System;

namespace FastCsv.Mapping.Attributes;

/// <summary>
/// Specifies a custom converter for a CSV property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CsvConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Gets or sets converter-specific parameters
    /// </summary>
    public object[]? Parameters { get; set; }

    /// <summary>
    /// Initializes a new instance with the specified converter type
    /// </summary>
    /// <param name="converterType">Type that implements ICsvConverter</param>
    public CsvConverterAttribute(Type converterType)
    {
        ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
    }
}