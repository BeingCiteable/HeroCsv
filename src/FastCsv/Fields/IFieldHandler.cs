namespace FastCsv.Fields;

/// <summary>
/// Handler responsible for field detection and parsing within CSV records
/// Core responsibility: Parse a record into individual fields
/// </summary>
public partial interface IFieldHandler
{
    /// <summary>
    /// Parse a record into individual fields and return field count
    /// </summary>
    int ParseFields(ReadOnlySpan<char> record, CsvOptions options, Span<string> fields);

    /// <summary>
    /// Get the number of fields in a record
    /// </summary>
    int CountFields(ReadOnlySpan<char> record, CsvOptions options);

    /// <summary>
    /// Get a specific field by index from a record
    /// </summary>
    ReadOnlySpan<char> GetField(ReadOnlySpan<char> record, int fieldIndex, CsvOptions options);

    /// <summary>
    /// Try to get a specific field by index
    /// </summary>
    bool TryGetField(ReadOnlySpan<char> record, int fieldIndex, CsvOptions options, out ReadOnlySpan<char> field);
}