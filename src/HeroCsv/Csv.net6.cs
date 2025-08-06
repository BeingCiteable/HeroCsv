#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Text;
using HeroCsv.Models;

namespace HeroCsv;

/// <summary>
/// Async operations for the Csv class
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Asynchronously reads all records from a file
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="options">CSV parsing options</param>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All records from the file</returns>
    public static async Task<IReadOnlyList<string[]>> ReadFileAsync(
        string filePath,
        CsvOptions options = default,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        options = GetValidOptions(options);
        using var reader = CreateReaderFromFile(filePath, options, encoding);
        return await reader.ReadAllRecordsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads records from a file as an async enumerable
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="options">CSV parsing options</param>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of records</returns>
    public static async IAsyncEnumerable<string[]> ReadFileAsyncEnumerable(
        string filePath,
        CsvOptions options = default,
        Encoding? encoding = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options = GetValidOptions(options);
        using var reader = CreateReaderFromFile(filePath, options, encoding);
        
        await foreach (var record in reader.ReadRecordsAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Asynchronously reads all records from a stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="options">CSV parsing options</param>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="leaveOpen">Whether to leave the stream open</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All records from the stream</returns>
    public static async Task<IReadOnlyList<string[]>> ReadStreamAsync(
        Stream stream,
        CsvOptions options = default,
        Encoding? encoding = null,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        options = GetValidOptions(options);
        using var reader = CreateReader(stream, options, encoding, leaveOpen);
        return await reader.ReadAllRecordsAsync(cancellationToken).ConfigureAwait(false);
    }
}
#endif