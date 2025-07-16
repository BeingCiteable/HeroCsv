#if NET7_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Advanced parsing features for FastCsvReader
/// </summary>
internal sealed partial class FastCsvReader
{

    /// <summary>
    /// Attempts to parse a field to the specified type using high-performance parsing
    /// </summary>
    /// <typeparam name="T">Target type for parsing</typeparam>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed value output</param>
    /// <returns>True if parsing succeeded</returns>
    public bool TryParseField<T>(int fieldIndex, out T value) where T : struct
    {
        value = default;

        if (_currentRecord == null || !_currentRecord.TryGetField(fieldIndex, out var fieldValue))
        {
            return false;
        }

        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(fieldValue, out var intValue))
            {
                value = (T)(object)intValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (decimal.TryParse(fieldValue, out var decimalValue))
            {
                value = (T)(object)decimalValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(double))
        {
            if (double.TryParse(fieldValue, out var doubleValue))
            {
                value = (T)(object)doubleValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (DateTime.TryParse(fieldValue, out var dateValue))
            {
                value = (T)(object)dateValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (DateTimeOffset.TryParse(fieldValue, out var dateOffsetValue))
            {
                value = (T)(object)dateOffsetValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a field as an integer
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed integer value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseInt32(int fieldIndex, out int value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Attempts to parse a field as a decimal
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed decimal value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDecimal(int fieldIndex, out decimal value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Gets field content as UTF-8 encoded bytes for efficient processing
    /// </summary>
    /// <param name="fieldIndex">Index of the field to retrieve</param>
    /// <returns>UTF-8 encoded field content</returns>
    public ReadOnlySpan<byte> GetFieldAsUtf8(int fieldIndex)
    {
        if (_currentRecord == null || !_currentRecord.TryGetField(fieldIndex, out var fieldValue))
        {
            return ReadOnlySpan<byte>.Empty;
        }

        // Convert to UTF-8 bytes - use array for return compatibility
        var byteCount = Encoding.UTF8.GetByteCount(fieldValue);
        if (byteCount == 0) return ReadOnlySpan<byte>.Empty;

        var bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(fieldValue, bytes);
        return bytes;
    }
}

/// <summary>
/// Advanced parsing features for FastCsvRecord
/// </summary>
internal sealed partial class FastCsvRecord
{
    /// <summary>
    /// Attempts to parse a field to the specified type using high-performance parsing
    /// </summary>
    /// <typeparam name="T">Target type for parsing</typeparam>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed value output</param>
    /// <returns>True if parsing succeeded</returns>
    public bool TryParseField<T>(int fieldIndex, out T value) where T : struct
    {
        value = default;

        if (!TryGetField(fieldIndex, out var fieldValue))
        {
            return false;
        }

        return TryParseFieldValue(fieldValue, out value);
    }

    /// <summary>
    /// Attempts to parse a field as an integer
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed integer value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseInt32(int fieldIndex, out int value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Attempts to parse a field as a decimal
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed decimal value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDecimal(int fieldIndex, out decimal value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Attempts to parse a field as a DateTime
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed DateTime value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDateTime(int fieldIndex, out DateTime value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Attempts to parse a field as a DateTimeOffset
    /// </summary>
    /// <param name="fieldIndex">Index of the field to parse</param>
    /// <param name="value">Parsed DateTimeOffset value</param>
    /// <returns>True if parsing succeeded</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDateTimeOffset(int fieldIndex, out DateTimeOffset value)
    {
        return TryParseField(fieldIndex, out value);
    }

    /// <summary>
    /// Gets field content as UTF-8 encoded bytes for efficient processing
    /// </summary>
    /// <param name="fieldIndex">Index of the field to retrieve</param>
    /// <returns>UTF-8 encoded field content</returns>
    public ReadOnlySpan<byte> GetFieldAsUtf8(int fieldIndex)
    {
        if (!TryGetField(fieldIndex, out var fieldValue))
        {
            return ReadOnlySpan<byte>.Empty;
        }

        // Convert to UTF-8 bytes - use array for return compatibility
        var byteCount = Encoding.UTF8.GetByteCount(fieldValue);
        if (byteCount == 0) return ReadOnlySpan<byte>.Empty;

        var bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(fieldValue, bytes);
        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseFieldValue<T>(ReadOnlySpan<char> fieldValue, out T value) where T : struct
    {
        value = default;

        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(fieldValue, out var intValue))
            {
                value = (T)(object)intValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (decimal.TryParse(fieldValue, out var decimalValue))
            {
                value = (T)(object)decimalValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(double))
        {
            if (double.TryParse(fieldValue, out var doubleValue))
            {
                value = (T)(object)doubleValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (DateTime.TryParse(fieldValue, out var dateValue))
            {
                value = (T)(object)dateValue;
                return true;
            }
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (DateTimeOffset.TryParse(fieldValue, out var dateOffsetValue))
            {
                value = (T)(object)dateOffsetValue;
                return true;
            }
        }

        return false;
    }
}
#endif