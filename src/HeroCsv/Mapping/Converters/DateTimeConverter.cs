using System;
using System.Globalization;

namespace HeroCsv.Mapping.Converters;

/// <summary>
/// Converter for DateTime and DateTimeOffset types with format support
/// </summary>
public class DateTimeConverter : ICsvConverter
{
    private readonly string _defaultFormat = "yyyy-MM-dd HH:mm:ss";

    /// <inheritdoc />
    public object? ConvertFromString(string value, Type targetType, string? format = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var actualFormat = format ?? _defaultFormat;

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            // If no specific format provided, try multiple common formats
            if (format == null)
            {
                // Try standard parsing first (handles many formats)
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                    return dateTime;

                // Try common date formats
                string[] commonFormats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy",
                    "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };

                foreach (var fmt in commonFormats)
                {
                    if (DateTime.TryParseExact(value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                        return dateTime;
                }
            }
            else
            {
                // Try format-specific parsing
                if (DateTime.TryParseExact(value, actualFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                    return dateTime;

                // If format was specified and failed, throw error
                throw new FormatException($"Unable to parse '{value}' as DateTime using format '{actualFormat}'");
            }

            // Otherwise throw generic error
            var triedFormats = new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy",
                "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
            throw new FormatException(
                $"Unable to parse '{value}' as DateTime. " +
                $"Tried standard parsing and common formats: {string.Join(", ", triedFormats.Select(f => $"'{f}'"))}. " +
                "Consider specifying a format string or adding the format to the converter.");
        }

        if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
        {
            // If no specific format provided, try multiple common formats
            if (format == null)
            {
                // Try standard parsing first
                if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
                    return dateTimeOffset;

                // Try common date formats
                string[] commonFormats = [
                    "dd/MM/yyyy",
                    "MM/dd/yyyy",
                    "yyyy-MM-dd",
                    "dd-MM-yyyy",
                    "dd/MM/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss"
                ];

                foreach (var fmt in commonFormats)
                {
                    if (DateTimeOffset.TryParseExact(value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffset))
                        return dateTimeOffset;
                }
            }
            else
            {
                // Try format-specific parsing
                if (DateTimeOffset.TryParseExact(value, actualFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
                    return dateTimeOffset;

                // If format was specified and failed, throw error
                throw new FormatException($"Unable to parse '{value}' as DateTimeOffset using format '{actualFormat}'");
            }

            // Otherwise throw generic error
            throw new FormatException($"Unable to parse '{value}' as DateTimeOffset");
        }

        throw new NotSupportedException($"Type {targetType} is not supported by DateTimeConverter");
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, string? format = null)
    {
        if (value == null)
            return string.Empty;

        var actualFormat = format ?? _defaultFormat;

        return value switch
        {
            DateTime dt => dt.ToString(actualFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(actualFormat, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}