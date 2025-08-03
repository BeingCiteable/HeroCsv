#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv.Core;
using HeroCsv.Models;

namespace HeroCsv;

/// <summary>
/// Async operations for Csv
/// </summary>
public static partial class Csv
{
        /// <summary>
        /// Reads CSV data from a stream asynchronously
        /// </summary>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="options">CSV parsing options</param>
        /// <param name="leaveOpen">Whether to leave the stream open after reading</param>
        /// <returns>Async enumerable of CSV records</returns>
        public static async IAsyncEnumerable<string[]> ReadStreamAsyncEnumerable(Stream stream, CsvOptions options = default, bool leaveOpen = false)
        {
#if NET6_0_OR_GREATER
            await using var reader = CreateReader(stream, options, leaveOpen: leaveOpen);
#else
            using var reader = CreateReader(stream, options, leaveOpen: leaveOpen);
#endif
            await foreach (var record in reader.ReadRecordsAsync())
            {
                yield return record;
            }
        }

        /// <summary>
        /// Reads CSV from a file asynchronously
        /// </summary>
        /// <param name="filePath">Path to CSV file</param>
        /// <param name="options">CSV parsing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Async enumerable of CSV records</returns>
        public static async IAsyncEnumerable<string[]> ReadFileAsync(
            string filePath,
            CsvOptions options = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            await foreach (var record in ReadStreamAsyncEnumerable(stream, options).WithCancellation(cancellationToken))
            {
                yield return record;
            }
        }

        /// <summary>
        /// Reads all CSV records from a stream asynchronously into memory
        /// </summary>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="options">CSV parsing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all CSV records</returns>
        public static async Task<IReadOnlyList<string[]>> ReadAllStreamAsync(
            Stream stream,
            CsvOptions options = default,
            CancellationToken cancellationToken = default)
        {
            await using var reader = CreateReader(stream, options);
            return await reader.ReadAllRecordsAsync(cancellationToken);
        }


}

#endif