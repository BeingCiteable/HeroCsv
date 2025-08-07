using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    [RequiresUnreferencedCode("Generic CSV mapping uses reflection to map data to objects. The type T must be preserved.")]
    [RequiresDynamicCode("Generic CSV mapping may create instances of types at runtime.")]
    public static T MapTo<T>(this ICsvRecord record, string[] headers) where T : class, new()
    {
        var mapper = new CsvMapper<T>(CsvOptions.Default);
        mapper.SetHeaders(headers);
        return mapper.MapRecord(record.ToArray());
    }

    /// <summary>
    /// Maps record to object using custom mapping
    /// </summary>
    [RequiresUnreferencedCode("Generic CSV mapping uses reflection to map data to objects. The type T must be preserved.")]
    [RequiresDynamicCode("Generic CSV mapping may create instances of types at runtime.")]
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

    // ==================== AOT-Safe Type Conversion Methods ====================
    // These methods provide type-safe field access without reflection for AOT compatibility

    /// <summary>
    /// Gets a field as string (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetString(this ICsvRecord record, int index)
    {
        return record.GetField(index).ToString();
    }

    /// <summary>
    /// Gets a field as int (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInt32(this ICsvRecord record, int index)
    {
        return int.Parse(record.GetField(index).ToString(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tries to get a field as int (AOT-safe)
    /// </summary>
    public static bool TryGetInt32(this ICsvRecord record, int index, out int value)
    {
        value = 0;
        if (index < 0 || index >= record.FieldCount) return false;
        return int.TryParse(record.GetField(index).ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a field as long (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetInt64(this ICsvRecord record, int index)
    {
        return long.Parse(record.GetField(index).ToString(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tries to get a field as long (AOT-safe)
    /// </summary>
    public static bool TryGetInt64(this ICsvRecord record, int index, out long value)
    {
        value = 0;
        if (index < 0 || index >= record.FieldCount) return false;
        return long.TryParse(record.GetField(index).ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a field as double (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetDouble(this ICsvRecord record, int index)
    {
        return double.Parse(record.GetField(index).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tries to get a field as double (AOT-safe)
    /// </summary>
    public static bool TryGetDouble(this ICsvRecord record, int index, out double value)
    {
        value = 0;
        if (index < 0 || index >= record.FieldCount) return false;
        return double.TryParse(record.GetField(index).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a field as decimal (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal GetDecimal(this ICsvRecord record, int index)
    {
        return decimal.Parse(record.GetField(index).ToString(), NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tries to get a field as decimal (AOT-safe)
    /// </summary>
    public static bool TryGetDecimal(this ICsvRecord record, int index, out decimal value)
    {
        value = 0;
        if (index < 0 || index >= record.FieldCount) return false;
        return decimal.TryParse(record.GetField(index).ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Gets a field as bool (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBoolean(this ICsvRecord record, int index)
    {
        var field = record.GetField(index).ToString();
        return string.Equals(field, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(field, "1", StringComparison.Ordinal) ||
               string.Equals(field, "yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to get a field as bool (AOT-safe)
    /// </summary>
    public static bool TryGetBoolean(this ICsvRecord record, int index, out bool value)
    {
        value = false;
        if (index < 0 || index >= record.FieldCount) return false;

        var field = record.GetField(index).ToString();
        if (bool.TryParse(field, out value)) return true;

        // Also check for common boolean representations
        if (string.Equals(field, "1", StringComparison.Ordinal) ||
            string.Equals(field, "yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }
        if (string.Equals(field, "0", StringComparison.Ordinal) ||
            string.Equals(field, "no", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a field as DateTime (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetDateTime(this ICsvRecord record, int index)
    {
        return DateTime.Parse(record.GetField(index).ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    /// <summary>
    /// Gets a field as DateTime with specific format (AOT-safe)
    /// </summary>
    public static DateTime GetDateTime(this ICsvRecord record, int index, string format)
    {
        return DateTime.ParseExact(record.GetField(index).ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    /// <summary>
    /// Tries to get a field as DateTime (AOT-safe)
    /// </summary>
    public static bool TryGetDateTime(this ICsvRecord record, int index, out DateTime value)
    {
        value = default;
        if (index < 0 || index >= record.FieldCount) return false;
        return DateTime.TryParse(record.GetField(index).ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    /// <summary>
    /// Gets a field as DateTimeOffset (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset GetDateTimeOffset(this ICsvRecord record, int index)
    {
        return DateTimeOffset.Parse(record.GetField(index).ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    /// <summary>
    /// Tries to get a field as DateTimeOffset (AOT-safe)
    /// </summary>
    public static bool TryGetDateTimeOffset(this ICsvRecord record, int index, out DateTimeOffset value)
    {
        value = default;
        if (index < 0 || index >= record.FieldCount) return false;
        return DateTimeOffset.TryParse(record.GetField(index).ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    /// <summary>
    /// Gets a field as Guid (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GetGuid(this ICsvRecord record, int index)
    {
        return Guid.Parse(record.GetField(index).ToString());
    }

    /// <summary>
    /// Tries to get a field as Guid (AOT-safe)
    /// </summary>
    public static bool TryGetGuid(this ICsvRecord record, int index, out Guid value)
    {
        value = default;
        if (index < 0 || index >= record.FieldCount) return false;
        return Guid.TryParse(record.GetField(index).ToString(), out value);
    }

    /// <summary>
    /// Gets a field as an enum value (AOT-safe)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetEnum<T>(this ICsvRecord record, int index) where T : struct, Enum
    {
#if NETSTANDARD2_0
        return (T)Enum.Parse(typeof(T), record.GetField(index).ToString(), ignoreCase: true);
#else
        return Enum.Parse<T>(record.GetField(index).ToString(), ignoreCase: true);
#endif
    }

    /// <summary>
    /// Tries to get a field as an enum value (AOT-safe)
    /// </summary>
    public static bool TryGetEnum<T>(this ICsvRecord record, int index, out T value) where T : struct, Enum
    {
        value = default;
        if (index < 0 || index >= record.FieldCount) return false;
#if NETSTANDARD2_0
        try
        {
            value = (T)Enum.Parse(typeof(T), record.GetField(index).ToString(), ignoreCase: true);
            return true;
        }
        catch
        {
            return false;
        }
#else
        return Enum.TryParse<T>(record.GetField(index).ToString(), ignoreCase: true, out value);
#endif
    }

    /// <summary>
    /// Gets a field value or default if out of bounds (AOT-safe)
    /// </summary>
    public static string GetStringOrDefault(this ICsvRecord record, int index, string defaultValue = "")
    {
        if (index < 0 || index >= record.FieldCount)
            return defaultValue;
        return record.GetField(index).ToString();
    }

    /// <summary>
    /// Gets field index by header name (AOT-safe)
    /// </summary>
    public static int GetFieldIndex(this string[] headers, string fieldName)
    {
        return Array.IndexOf(headers, fieldName);
    }

    /// <summary>
    /// Gets field by header name (AOT-safe)
    /// </summary>
    public static string GetFieldByName(this ICsvRecord record, string[] headers, string fieldName)
    {
        var index = headers.GetFieldIndex(fieldName);
        if (index < 0) throw new ArgumentException($"Field '{fieldName}' not found in headers");
        return record.GetString(index);
    }
}