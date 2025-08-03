namespace HeroCsv.Core;

/// <summary>
/// Represents a single CSV record with field access
/// </summary>
public partial interface ICsvRecord
{
    /// <summary>
    /// Line number of this record (1-based)
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Number of fields in this record
    /// </summary>
    int FieldCount { get; }

    /// <summary>
    /// Get a field by index (0-based)
    /// </summary>
    ReadOnlySpan<char> GetField(int index);

    /// <summary>
    /// Try to get a field by index
    /// </summary>
    bool TryGetField(int index, out ReadOnlySpan<char> field);

    /// <summary>
    /// Check if the field index is valid
    /// </summary>
    bool IsValidIndex(int index);

    /// <summary>
    /// Get all fields into a destination span
    /// </summary>
    int GetAllFields(Span<string> destination);
}