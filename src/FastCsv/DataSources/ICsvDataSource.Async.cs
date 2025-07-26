#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Async operations for CSV data sources
/// </summary>
internal interface IAsyncCsvDataSource : ICsvDataSource
{
    /// <summary>
    /// Asynchronously try to read the next line from the data source
    /// </summary>
    ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously count total lines in the data source
    /// </summary>
    ValueTask<int> CountLinesDirectlyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods to provide default async implementations
/// </summary>
internal static class CsvDataSourceAsyncExtensions
{
    /// <summary>
    /// Provides a default async implementation that wraps the sync method
    /// </summary>
    public static ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsyncDefault(
        this ICsvDataSource source,
        CancellationToken _ = default)
    {
        if (source.TryReadLine(out var line, out var lineNumber))
        {
            return new ValueTask<(bool, string, int)>((true, line.ToString(), lineNumber));
        }
        return new ValueTask<(bool, string, int)>((false, string.Empty, 0));
    }

    /// <summary>
    /// Provides a default async implementation for counting lines
    /// </summary>
    public static ValueTask<int> CountLinesDirectlyAsyncDefault(
        this ICsvDataSource source,
        CancellationToken _ = default)
    {
        return new ValueTask<int>(source.CountLinesDirectly());
    }
}
#endif