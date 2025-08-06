using System.Text;
using HeroCsv.Parsing;

namespace HeroCsv.DataSources;

/// <summary>
/// Provides abstraction for CSV data sources supporting both sync and async operations
/// </summary>
internal interface ICsvDataSource : IDisposable
#if NET6_0_OR_GREATER
    , IAsyncDisposable
#endif
{
    /// <summary>
    /// Try to read the next line from the data source
    /// </summary>
    bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber);

    /// <summary>
    /// Try to get the next line position without allocating
    /// </summary>
    bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber);

    /// <summary>
    /// Get the entire buffer for zero-copy access
    /// </summary>
    ReadOnlySpan<char> GetBuffer();

    /// <summary>
    /// Reset the data source to the beginning if supported
    /// </summary>
    void Reset();

    /// <summary>
    /// Whether the data source supports reset operation
    /// </summary>
    bool SupportsReset { get; }

    /// <summary>
    /// Whether there is more data to read
    /// </summary>
    bool HasMoreData { get; }

    /// <summary>
    /// Counts total lines in the data source for performance optimization
    /// </summary>
    int CountLines();

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asynchronously try to read the next line from the data source
    /// </summary>
    ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously count total lines in the data source
    /// </summary>
    ValueTask<int> CountLinesAsync(CancellationToken cancellationToken = default);
#endif
}
