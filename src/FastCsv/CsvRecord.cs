using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// High-performance CSV row implementation using spans for zero allocations
/// </summary>
/// <remarks>
/// Creates a new CsvRecord with the specified fields and line number
/// </remarks>
/// <param name="fields">Array of field values</param>
/// <param name="lineNumber">Line number of this record</param>
internal sealed partial class CsvRecord(string[] fields, int lineNumber) : ICsvRecord
{
    private readonly string[] _fields = fields ?? throw new ArgumentNullException(nameof(fields));

    /// <summary>
    /// Line number of this record (1-based)
    /// </summary>
    public int LineNumber => lineNumber;

    /// <summary>
    /// Number of fields in this record
    /// </summary>
    public int FieldCount => _fields.Length;

    /// <summary>
    /// Get a field by index (0-based)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetField(int index)
    {
        if (index < 0 || index >= _fields.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return _fields[index].AsSpan();
    }

    /// <summary>
    /// Try to get a field by index
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetField(int index, out ReadOnlySpan<char> field)
    {
        if (index >= 0 && index < _fields.Length)
        {
            field = _fields[index].AsSpan();
            return true;
        }
        field = ReadOnlySpan<char>.Empty;
        return false;
    }

    /// <summary>
    /// Check if the field index is valid
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < _fields.Length;
    }

    /// <summary>
    /// Get all fields into a destination span
    /// </summary>
    public int GetAllFields(Span<string> destination)
    {
        var count = Math.Min(_fields.Length, destination.Length);
        for (int i = 0; i < count; i++)
        {
            destination[i] = _fields[i];
        }
        return count;
    }

    /// <summary>
    /// Get all fields as a string array
    /// </summary>
    public string[] ToArray()
    {
        var result = new string[_fields.Length];
        Array.Copy(_fields, result, _fields.Length);
        return result;
    }

}
