#if NET7_0_OR_GREATER
using System.Text;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Models;

namespace HeroCsv;

/// <summary>
/// Async operations for the Csv class
/// </summary>
public static partial class Csv
{
        /// <summary>
        /// Creates an async CSV reader from a stream for true async I/O
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="options">CSV parsing options</param>
        /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
        /// <param name="leaveOpen">Whether to leave the stream open after disposal</param>
        /// <returns>Async-capable CSV reader</returns>
        public static ICsvReader CreateAsyncReader(Stream stream, CsvOptions options = default, Encoding? encoding = null, bool leaveOpen = false)
        {
            options = GetValidOptions(options);
            var dataSource = new AsyncStreamDataSource(stream, encoding, leaveOpen);
            return new HeroCsvReader(dataSource, options);
        }

        /// <summary>
        /// Creates an async CSV reader from a file path
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="options">CSV parsing options</param>
        /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
        /// <returns>Async-capable CSV reader</returns>
        public static ICsvReader CreateAsyncReaderFromFile(string filePath, CsvOptions options = default, Encoding? encoding = null)
        {
            options = GetValidOptions(options);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var dataSource = new AsyncStreamDataSource(stream, encoding, leaveOpen: false);
            return new HeroCsvReader(dataSource, options);
        }

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
            using var reader = CreateAsyncReaderFromFile(filePath, options, encoding);
            return await reader.ReadAllRecordsAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously reads records from a file
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
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var reader = CreateAsyncReaderFromFile(filePath, options, encoding);

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
            using var reader = CreateAsyncReader(stream, options, encoding, leaveOpen);
            return await reader.ReadAllRecordsAsync(cancellationToken).ConfigureAwait(false);
        }
}

#endif