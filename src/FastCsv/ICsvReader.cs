namespace FastCsv;

/// <summary>
/// Core interface for CSV reading operations
/// Focused on reading records and navigation
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    bool HasMoreData { get; }

    /// <summary>
    /// Total number of records processed so far
    /// </summary>
    int RecordCount { get; }

    /// <summary>
    /// Current position in the input data
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Read the next CSV record
    /// </summary>
    ICsvRecord ReadRecord();

    /// <summary>
    /// Try to read the next record, returns false if no more data
    /// </summary>
    bool TryReadRecord(out ICsvRecord record);

    /// <summary>
    /// Skip the header row
    /// </summary>
    void SkipHeader();

    /// <summary>
    /// Skip the next record without parsing it
    /// </summary>
    void SkipRecord();

    /// <summary>
    /// Skip multiple records
    /// </summary>
    void SkipRecords(int count);

    /// <summary>
    /// Get the current CSV options being used
    /// </summary>
    CsvOptions Options { get; }

    /// <summary>
    /// Reset the reader to the beginning
    /// </summary>
    void Reset();
}