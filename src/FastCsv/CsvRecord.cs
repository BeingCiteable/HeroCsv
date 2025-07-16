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
/// Represents a single CSV record with field enumeration
/// </summary>
public readonly ref struct CsvRecord
{
    private readonly ReadOnlySpan<char> _recordData;
    private readonly CsvOptions _options;
    private readonly int _lineNumber;

    public CsvRecord()
    {
        _recordData = ReadOnlySpan<char>.Empty;
        _options = CsvOptions.Default;
        _lineNumber = 0;
    }

    internal CsvRecord(ReadOnlySpan<char> recordData, CsvOptions options, int lineNumber)
    {
        _recordData = recordData;
        _options = options;
        _lineNumber = lineNumber;
    }

    /// <summary>
    /// Line number this record came from
    /// </summary>
    public readonly int LineNumber => _lineNumber;

    /// <summary>
    /// Get field enumerator
    /// </summary>
    public readonly CsvFieldEnumerator GetEnumerator() => new(_recordData, _options);

    /// <summary>
    /// Get a specific field by index (0-based)
    /// </summary>
    public readonly ReadOnlySpan<char> GetField(int index)
    {
        var fieldIndex = 0;
        foreach (var field in this)
        {
            if (fieldIndex == index)
                return field;
            fieldIndex++;
        }
        return ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Convert all fields to a string array (allocates)
    /// </summary>
    public readonly string[] ToStringArray()
    {
        var fields = new List<string>();
        foreach (var field in this)
        {
            fields.Add(field.ToString());
        }
        return fields.ToArray();
    }

    /// <summary>
    /// Count the number of fields in this record
    /// </summary>
    public readonly int FieldCount
    {
        get
        {
            var count = 0;
            foreach (var _ in this)
                count++;
            return count;
        }
    }
}
