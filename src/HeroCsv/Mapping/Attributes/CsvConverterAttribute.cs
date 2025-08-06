using System;

namespace HeroCsv.Mapping.Attributes;

/// <summary>
/// Specifies a custom converter for a CSV property
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified converter type
/// </remarks>
/// <param name="converterType">Type that implements ICsvConverter</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CsvConverterAttribute(Type converterType) : Attribute
{
    /// <summary>
    /// Gets the converter type
    /// </summary>
    public Type ConverterType { get; } = converterType ?? throw new ArgumentNullException(nameof(converterType));

    /// <summary>
    /// Gets or sets converter-specific parameters
    /// </summary>
    public object[]? Parameters { get; set; }
}