#if NET7_0_OR_GREATER
using System;
using System.Buffers.Text;

namespace FastCsv;

/// <summary>
/// Fast type conversion enhancements for ICsvRecord
/// </summary>
public partial interface ICsvRecord
{
    /// <summary>
    /// Parse field as specific type
    /// </summary>
    bool TryParseField<T>(int index, out T value) where T : struct;

    /// <summary>
    /// Parse field as Int32
    /// </summary>
    bool TryParseInt32(int index, out int value);

    /// <summary>
    /// Parse field as Decimal
    /// </summary>
    bool TryParseDecimal(int index, out decimal value);

    /// <summary>
    /// Parse field as DateTime
    /// </summary>
    bool TryParseDateTime(int index, out DateTime value);

    /// <summary>
    /// Get field as UTF-8 bytes
    /// </summary>
    ReadOnlySpan<byte> GetFieldAsUtf8(int index);
}
#endif