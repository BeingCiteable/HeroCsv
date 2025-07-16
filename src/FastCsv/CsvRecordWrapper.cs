#if NETSTANDARD2_0
#endif

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

#if NET7_0_OR_GREATER
using System.Buffers.Text;
#endif

#if NET8_0_OR_GREATER
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Runtime.Intrinsics;
using System.Text.Unicode;
#endif


namespace FastCsv;

/// <summary>
/// Wrapper for CsvRecord that can be used with IEnumerable
/// </summary>
public class CsvRecordWrapper
{
    private readonly string[] _fields;
    private readonly int _lineNumber;

    internal CsvRecordWrapper(CsvRecord record)
    {
        _fields = record.ToStringArray();
        _lineNumber = record.LineNumber;
    }

    /// <summary>
    /// Line number this record came from
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Get field as string by index (0-based)
    /// </summary>
    public string GetField(int index)
    {
        return index >= 0 && index < _fields.Length ? _fields[index] : string.Empty;
    }

    /// <summary>
    /// Get all fields as string array
    /// </summary>
    public string[] ToStringArray() => _fields;

    /// <summary>
    /// Number of fields in this record
    /// </summary>
    public int FieldCount => _fields.Length;

    /// <summary>
    /// Get enumerator for fields
    /// </summary>
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_fields).GetEnumerator();
}
