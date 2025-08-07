using System;
using System.Diagnostics.CodeAnalysis;

namespace HeroCsv.Mapping.Converters;

/// <summary>
/// Interface for custom CSV value converters
/// </summary>
public interface ICsvConverter
{
    /// <summary>
    /// Converts a CSV string value to the target type
    /// </summary>
    /// <param name="value">The CSV string value</param>
    /// <param name="targetType">The target property type</param>
    /// <param name="format">Optional format string</param>
    /// <returns>Converted value</returns>
    [RequiresDynamicCode("Type conversion may create instances of types at runtime.")]
    object? ConvertFromString(string value, Type targetType, string? format = null);

    /// <summary>
    /// Converts an object to CSV string representation
    /// </summary>
    /// <param name="value">The object value</param>
    /// <param name="format">Optional format string</param>
    /// <returns>CSV string representation</returns>
    string ConvertToString(object? value, string? format = null);
}