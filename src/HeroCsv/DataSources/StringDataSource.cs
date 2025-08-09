using HeroCsv.Parsing;

namespace HeroCsv.DataSources;

/// <summary>
/// Data source for string-based content
/// </summary>
internal sealed class StringDataSource(string content) : ICsvDataSource
{
    private readonly string _content = content ?? throw new ArgumentNullException(nameof(content));
    private int _position;
    private int _lineNumber = 1;
    private bool _disposed;

    public bool SupportsReset => true;

    public bool HasMoreData => _position < _content.Length;

    /// <summary>
    /// Ultra-fast line counting without parsing
    /// </summary>
    public int CountLines()
    {
        if (string.IsNullOrEmpty(_content)) return 0;

        var newlineCount = CsvParser.CountLines(_content.AsSpan());

        // If content doesn't end with newline, there's one more line
        if (_content.Length > 0 && _content[_content.Length - 1] != '\n' && _content[_content.Length - 1] != '\r')
        {
            return newlineCount + 1;
        }

        return newlineCount;
    }

    public bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber)
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) throw new ObjectDisposedException(nameof(StringDataSource));
#endif
        lineNumber = _lineNumber;

        if (_position >= _content.Length)
        {
            line = default;
            return false;
        }

        var lineStart = _position;
#if NET8_0_OR_GREATER
        var lineEnd = CsvParser.FindLineEndVectorized(_content.AsSpan(), _position);
#else
        var lineEnd = CsvParser.FindLineEnd(_content.AsSpan(), _position);
#endif

        if (lineEnd == lineStart)
        {
            // Empty line
            _position = CsvParser.SkipLineEnding(_content.AsSpan(), _position);
            _lineNumber++;
            line = [];
            return true;
        }

        line = _content.AsSpan(lineStart, lineEnd - lineStart);
        _position = CsvParser.SkipLineEnding(_content.AsSpan(), lineEnd);
        _lineNumber++;
        return true;
    }

    public bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber)
    {
        lineNumber = _lineNumber;
        lineStart = _position;

        if (_position >= _content.Length)
        {
            lineLength = 0;
            return false;
        }

#if NET8_0_OR_GREATER
        var lineEnd = CsvParser.FindLineEndVectorized(_content.AsSpan(), _position);
#else
        var lineEnd = CsvParser.FindLineEnd(_content.AsSpan(), _position);
#endif

        lineLength = lineEnd - lineStart;
        // Don't advance position - this method just returns info about the next line
        return true;
    }

    public ReadOnlySpan<char> GetBuffer() => _content.AsSpan();

    public void Reset()
    {
        _position = 0;
        _lineNumber = 1;
    }

    public void Dispose()
    {
        _disposed = true;
    }

#if NET6_0_OR_GREATER
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Async implementation - string is already in memory so we return synchronously
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
    /// Async implementation - string is already in memory so we return synchronously
    /// </summary>
    public ValueTask<int> CountLinesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(CountLines());
    }
#endif
}
