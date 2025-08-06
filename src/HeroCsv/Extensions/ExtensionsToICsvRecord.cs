using System.Globalization;
using HeroCsv.Core;
using HeroCsv.Mapping;
using HeroCsv.Models;

namespace HeroCsv;

/// <summary>
/// Extension methods for CSV record operations
/// </summary>
public static class ExtensionsToICsvRecord
{
    public static string[] ToArray(this ICsvRecord record)
    {
        if (record is CsvRecord fastRecord)
        {
            return fastRecord.ToArray();
        }

        var result = new string[record.FieldCount];
        for (int i = 0; i < record.FieldCount; i++)
        {
            result[i] = record.GetField(i).ToString();
        }
        return result;
    }

    public static Dictionary<string, string> ToDictionary(this ICsvRecord record, string[] headers)
    {
        var result = new Dictionary<string, string>(Math.Min(headers.Length, record.FieldCount));

        for (int i = 0; i < Math.Min(headers.Length, record.FieldCount); i++)
        {
            result[headers[i]] = record.GetField(i).ToString();
        }

        return result;
    }

    /// <summary>
    /// Maps record to object using auto mapping
    /// </summary>
    public static T MapTo<T>(this ICsvRecord record, string[] headers) where T : class, new()
    {
        var mapper = new CsvMapper<T>(CsvOptions.Default);
        mapper.SetHeaders(headers);
        return mapper.MapRecord(record.ToArray());
    }

    /// <summary>
    /// Maps record to object using custom mapping
    /// </summary>
    public static T MapTo<T>(this ICsvRecord record, CsvMapping<T> mapping) where T : class, new()
    {
        var mapper = new CsvMapper<T>(mapping);
        return mapper.MapRecord(record.ToArray());
    }

    /// <summary>
    /// Gets field value as specific type
    /// </summary>
    public static T GetField<T>(this ICsvRecord record, int index)
    {
        var field = record.GetField(index);
        var value = field.ToString();

        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to get field value as specific type
    /// </summary>
    public static bool TryGetField<T>(this ICsvRecord record, int index, out T? value)
    {
        value = default;

        if (!record.TryGetField(index, out var field))
        {
            return false;
        }

        try
        {
            var fieldValue = field.ToString();
            if (typeof(T) == typeof(string))
            {
                value = (T)(object)fieldValue;
                return true;
            }

            value = (T)Convert.ChangeType(fieldValue, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a field at the given index is empty or null
    /// </summary>
    /// <param name="record">CSV record</param>
    /// <param name="index">Field index</param>
    /// <returns>True if field is empty or null</returns>
    public static bool IsFieldEmpty(this ICsvRecord record, int index)
    {
        if (!record.TryGetField(index, out var field))
        {
            return true;
        }

        return field.IsEmpty || field.IsWhiteSpace();
    }

    /// <summary>
    /// Gets all non-empty fields from the record
    /// </summary>
    /// <param name="record">CSV record</param>
    /// <returns>Array of non-empty field values</returns>
    public static string[] GetNonEmptyFields(this ICsvRecord record)
    {
        var result = new List<string>();

        for (int i = 0; i < record.FieldCount; i++)
        {
            if (!record.IsFieldEmpty(i))
            {
                result.Add(record.GetField(i).ToString());
            }
        }

        return [.. result];
    }

    /// <summary>
    /// Validates that all required fields are present and not empty
    /// </summary>
    /// <param name="record">CSV record</param>
    /// <param name="requiredIndexes">Indexes of required fields</param>
    /// <returns>True if all required fields are present</returns>
    public static bool HasRequiredFields(this ICsvRecord record, params int[] requiredIndexes)
    {
        foreach (var index in requiredIndexes)
        {
            if (record.IsFieldEmpty(index))
            {
                return false;
            }
        }
        return true;
    }
}