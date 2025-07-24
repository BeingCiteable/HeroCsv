#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Async operations for FastCsvReader
/// </summary>
public sealed partial class FastCsvReader
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string[]> ReadRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Reset();

        // For now, use sync method since ICsvDataSource doesn't support async yet
        // TODO: Add async support to ICsvDataSource for true async streaming
        while (!cancellationToken.IsCancellationRequested && TryReadRecord(out var record))
        {
            yield return record.ToArray();
            await Task.Yield(); // Allow cancellation
        }
    }

}
#endif