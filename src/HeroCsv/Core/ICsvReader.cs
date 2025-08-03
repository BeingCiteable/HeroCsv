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
    int LineNumber { get; }

    bool HasMoreData { get; }

    int RecordCount { get; }


    ICsvRecord ReadRecord();

    /// <summary>
    /// Try to read the next record, returns false if no more data
    /// </summary>
    bool TryReadRecord(out ICsvRecord record);

    void SkipRecord();

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