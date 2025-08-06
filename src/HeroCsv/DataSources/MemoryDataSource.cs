using HeroCsv.Parsing;

namespace HeroCsv.DataSources;

/// <summary>
/// Data source for Memory-based content (allows safe span access)
/// </summary>
internal sealed class MemoryDataSource(ReadOnlyMemory<char> memory) : ICsvDataSource
{
    private readonly ReadOnlyMemory<char> _memory = memory;
    private int _position;
    private int _lineNumber = 1;

    public bool SupportsReset => true;

    public bool HasMoreData => _position < _memory.Length;

    /// <summary>
    /// Ultra-fast line counting without parsing
    /// </summary>
    public int CountLines()
    {
        if (_memory.IsEmpty) return 0;

        var span = _memory.Span;
        var newlineCount = CsvParser.CountLines(span);

        // If content doesn't end with newline, there's one more line
        if (span.Length > 0 && span[span.Length - 1] != '\n' && span[span.Length - 1] != '\r')
        {
            return newlineCount + 1;
        }

        return newlineCount;
    }

    public bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber)
    {
        lineNumber = _lineNumber;

        if (_position >= _memory.Length)
        {
            line = default;
            return false;
        }

        var fullSpan = _memory.Span;
#if NET8_0_OR_GREATER
        var lineEnd = CsvParser.FindLineEndVectorized(fullSpan, _position);
#else
        var lineEnd = CsvParser.FindLineEnd(fullSpan, _position);
#endif

        if (lineEnd == _position)
        {
            // Empty line
            _position = CsvParser.SkipLineEnding(fullSpan, _position);
            _lineNumber++;
            line = [];
            return true;
        }

        line = fullSpan.Slice(_position, lineEnd - _position);
        _position = CsvParser.SkipLineEnding(fullSpan, lineEnd);
        _lineNumber++;
        return true;
    }

    public bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber)
    {
        lineNumber = _lineNumber;
        lineStart = _position;

        if (_position >= _memory.Length)
        {
            lineLength = 0;
            return false;
        }

        var span = _memory.Span;
#if NET8_0_OR_GREATER
        var lineEnd = CsvParser.FindLineEndVectorized(span, _position);
#else
        var lineEnd = CsvParser.FindLineEnd(span, _position);
#endif

        lineLength = lineEnd - lineStart;
        _position = CsvParser.SkipLineEnding(span, lineEnd);
        _lineNumber++;
        return true;
    }

    public ReadOnlySpan<char> GetBuffer() => _memory.Span;

    public void Reset()
    {
        _position = 0;
        _lineNumber = 1;
    }

    public void Dispose()
    {
        // Nothing to dispose for memory source
    }

#if NET6_0_OR_GREATER
    public ValueTask DisposeAsync()
    {
        // Nothing to dispose for memory source
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Async implementation - memory is already loaded so we return synchronously
    /// </summary>
    public ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsync(CancellationToken cancellationToken = default)
    {
        if (TryReadLine(out var line, out var lineNumber))
        {
            return ValueTask.FromResult((true, line.ToString(), lineNumber));
        }
        return ValueTask.FromResult((false, string.Empty, 0));
    }

    /// <summary>
    /// Async implementation - memory is already loaded so we return synchronously
    /// </summary>
    public ValueTask<int> CountLinesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(CountLines());
    }
#endif
}
