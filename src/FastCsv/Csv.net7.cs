#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FastCsv;

/// <summary>
/// Async operations for Csv
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Asynchronously loads and parses CSV data from a file
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static async Task<IEnumerable<string[]>> ReadFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return new CsvMemoryEnumerable(content.AsMemory(), CsvOptions.Default);
    }

    /// <summary>
    /// Asynchronously loads and parses CSV data from a file with custom formatting settings
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static async Task<IEnumerable<string[]>> ReadFileAsync(string filePath, CsvOptions options)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return new CsvMemoryEnumerable(content.AsMemory(), options);
    }

    /// <summary>
    /// Parses field value to specific type using high-performance parsing
    /// </summary>
    /// <typeparam name="T">Target type for parsing</typeparam>
    /// <param name="fieldValue">Field content as span</param>
    /// <param name="result">Parsed value output</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseField<T>(ReadOnlySpan<char> fieldValue, out T result) where T : struct
    {
        result = default;

        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(fieldValue, out var intValue))
            {
                result = (T)(object)intValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (decimal.TryParse(fieldValue, out var decimalValue))
            {
                result = (T)(object)decimalValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(double))
        {
            if (double.TryParse(fieldValue, out var doubleValue))
            {
                result = (T)(object)doubleValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (DateTime.TryParse(fieldValue, out var dateValue))
            {
                result = (T)(object)dateValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (DateTimeOffset.TryParse(fieldValue, out var dateOffsetValue))
            {
                result = (T)(object)dateOffsetValue;
                return true;
            }
        }

        return false;
    }
}
#endif