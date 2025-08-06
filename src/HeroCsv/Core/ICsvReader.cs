using HeroCsv.Models;

namespace HeroCsv.Core;

/// <summary>
/// Core interface for CSV reading operations
/// </summary>
#if NET6_0_OR_GREATER
public partial interface ICsvReader : IDisposable, IAsyncDisposable
#else
public partial interface ICsvReader : IDisposable
#endif
{
    /// <summary>
    /// Gets the current line number in the CSV file
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Gets a value indicating whether more data is available to read
    /// </summary>
    bool HasMoreData { get; }

    /// <summary>
    /// Gets the total number of records read so far
    /// </summary>
    int RecordCount { get; }

    /// <summary>
    /// Reads the next CSV record
    /// </summary>
    /// <returns>The next CSV record</returns>
    /// <exception cref="InvalidOperationException">Thrown when no more data is available</exception>
    ICsvRecord ReadRecord();

    /// <summary>
    /// Try to read the next record, returns false if no more data
    /// </summary>
    /// <param name="record">The CSV record if available, null otherwise</param>
    /// <returns>True if a record was read, false if no more data</returns>
    bool TryReadRecord(out ICsvRecord record);

    /// <summary>
    /// Skips the next record without processing it
    /// </summary>
    void SkipRecord();

    /// <summary>
    /// Skips the specified number of records
    /// </summary>
    /// <param name="count">Number of records to skip</param>
    void SkipRecords(int count);

    /// <summary>
    /// CSV parsing options
    /// </summary>
    CsvOptions Options { get; }

    /// <summary>
    /// Resets the reader to the beginning
    /// </summary>
    void Reset();

    /// <summary>
    /// Reads all records into a list
    /// </summary>
    /// <returns>All CSV records</returns>
    IReadOnlyList<string[]> ReadAllRecords();

    /// <summary>
    /// Reads all records asynchronously into a list
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All CSV records</returns>
    Task<IReadOnlyList<string[]>> ReadAllRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets records as enumerable for streaming
    /// </summary>
    /// <returns>CSV records</returns>
    IEnumerable<string[]> GetRecords();


    /// <summary>
    /// Counts records without parsing fields for optimal performance
    /// </summary>
    /// <returns>Record count</returns>
    int CountRecords();



    /// <summary>
    /// Validation results (empty if validation disabled)
    /// </summary>
    CsvValidationResult ValidationResult { get; }


}