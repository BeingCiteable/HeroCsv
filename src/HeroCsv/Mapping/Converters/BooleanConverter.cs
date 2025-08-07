using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HeroCsv.Mapping.Converters;

/// <summary>
/// Converter for boolean values with customizable true/false values
/// </summary>
public class BooleanConverter : ICsvConverter
{
    private static readonly HashSet<string> DefaultTrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "yes", "y", "1", "on", "enabled"
    };
    
    private static readonly HashSet<string> DefaultFalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "false", "no", "n", "0", "off", "disabled"
    };

    private readonly HashSet<string> _trueValues;
    private readonly HashSet<string> _falseValues;

    /// <summary>
    /// Creates a boolean converter with default true/false values
    /// </summary>
    public BooleanConverter()
    {
        _trueValues = DefaultTrueValues;
        _falseValues = DefaultFalseValues;
    }

    /// <summary>
    /// Creates a boolean converter with custom true/false values
    /// </summary>
    /// <param name="trueValues">Values that represent true</param>
    /// <param name="falseValues">Values that represent false</param>
    public BooleanConverter(string[] trueValues, string[] falseValues)
    {
        _trueValues = new HashSet<string>(trueValues, StringComparer.OrdinalIgnoreCase);
        _falseValues = new HashSet<string>(falseValues, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    [RequiresDynamicCode("Type conversion may create instances of types at runtime.")]
    public object? ConvertFromString(string value, Type targetType, string? format = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        
        if (_trueValues.Contains(value))
            return true;
            
        if (_falseValues.Contains(value))
            return false;
            
        throw new FormatException(
            $"Unable to parse '{value}' as boolean. " +
            $"Expected values for true: {FormatValues(_trueValues)} " +
            $"Expected values for false: {FormatValues(_falseValues)} " +
            $"(comparison is case-insensitive)");
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, string? format = null)
    {
        if (value == null)
            return string.Empty;

        if (value is bool boolValue)
            return boolValue ? "true" : "false";
            
        return value.ToString() ?? string.Empty;
    }
    
    /// <summary>
    /// Formats a set of values for error messages
    /// </summary>
    private static string FormatValues(HashSet<string> values)
    {
        var sorted = new List<string>(values);
        sorted.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", sorted.Select(v => $"'{v}'"));
    }
}