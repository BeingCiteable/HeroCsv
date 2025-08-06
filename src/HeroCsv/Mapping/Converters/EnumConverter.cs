using System;
using System.Globalization;
using System.Linq;

namespace HeroCsv.Mapping.Converters;

/// <summary>
/// Converter for enum types with support for both names and numeric values
/// </summary>
public class EnumConverter : ICsvConverter
{
    /// <inheritdoc />
    public object? ConvertFromString(string value, Type targetType, string? format = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        if (!enumType.IsEnum)
            throw new ArgumentException($"Type {enumType} is not an enum type");

        value = value.Trim();
        
        // Try to parse as enum name (case-insensitive)
        try
        {
            return Enum.Parse(enumType, value, true);
        }
        catch (ArgumentException)
        {
            // Fall through to try numeric parsing
        }
            
        // Try to parse as numeric value
        if (int.TryParse(value, out var numericValue))
        {
            if (Enum.IsDefined(enumType, numericValue))
                return Enum.ToObject(enumType, numericValue);
        }
        
        // Check if this is a flags enum or format is specified as "Flags"
        bool isFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0 
                      || format?.Equals("Flags", StringComparison.OrdinalIgnoreCase) == true;
        
        if (isFlags && value.Contains('|'))
        {
            try
            {
                // Split by | and parse each part
                var parts = value.Split('|');
                int result = 0;
                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim();
                    try
                    {
                        var partValue = Enum.Parse(enumType, trimmedPart, true);
                        result |= Convert.ToInt32(partValue, CultureInfo.InvariantCulture);
                    }
                    catch (ArgumentException)
                    {
                        throw new FormatException($"Invalid flag value: {trimmedPart}");
                    }
                }
                return Enum.ToObject(enumType, result);
            }
            catch (Exception ex) when (ex is not FormatException)
            {
                // Fall through to error
            }
        }
        
        var enumNames = Enum.GetNames(enumType);
        var validValues = string.Join(", ", enumNames.Select(n => $"'{n}'"));
        var numericValues = string.Join(", ", Enum.GetValues(enumType).Cast<object>().Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture)));
        
        throw new FormatException(
            $"Unable to parse '{value}' as {enumType.Name}. " +
            $"Valid values are: {validValues} " +
            $"or numeric values: {numericValues} " +
            "(comparison is case-insensitive)");
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, string? format = null)
    {
        if (value == null)
            return string.Empty;

        // If format is specified as "Numeric", return numeric value
        if (format?.Equals("Numeric", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }
        
        return value.ToString() ?? string.Empty;
    }
}