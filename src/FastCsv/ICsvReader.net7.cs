#if NET7_0_OR_GREATER
using System;
using System.Buffers.Text;

namespace FastCsv;

/// <summary>
/// .NET 7+ fast parsing enhancements for ICsvReader
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Parse field as specific type
    /// </summary>
    bool TryParseField<T>(int fieldIndex, out T value) where T : struct;
    
    /// <summary>
    /// Parse field as Int32
    /// </summary>
    bool TryParseInt32(int fieldIndex, out int value);
    
    /// <summary>
    /// Parse field as Decimal
    /// </summary>
    bool TryParseDecimal(int fieldIndex, out decimal value);
    
    /// <summary>
    /// Get field as UTF-8 bytes
    /// </summary>
    ReadOnlySpan<byte> GetFieldAsUtf8(int fieldIndex);
}
#endif