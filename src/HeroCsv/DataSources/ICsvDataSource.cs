using System.Text;
using HeroCsv.Parsing;

namespace HeroCsv.DataSources;

/// <summary>
/// Provides abstraction for CSV data sources (string, span, stream) without allocations
/// </summary>
internal interface ICsvDataSource : IDisposable
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
    int CountLinesDirectly();
}

/// <summary>
/// Data source for string-based content
/// </summary>
internal sealed class StringDataSource(string content) : ICsvDataSource
{
    private readonly string _content = content ?? throw new ArgumentNullException(nameof(content));
    private int _position = 0;
    private int _lineNumber = 1;

    public bool SupportsReset => true;

    public bool HasMoreData => _position < _content.Length;

    /// <summary>
    /// Ultra-fast line counting without parsing
    /// </summary>
    public int CountLinesDirectly()
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
            line = ReadOnlySpan<char>.Empty;
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
        _position = CsvParser.SkipLineEnding(_content.AsSpan(), lineEnd);
        _lineNumber++;
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
        // Nothing to dispose for string source
    }
}

/// <summary>
/// Data source for Memory-based content (allows safe span access)
/// </summary>
internal sealed class MemoryDataSource(ReadOnlyMemory<char> memory) : ICsvDataSource
{
    private readonly ReadOnlyMemory<char> _memory = memory;
    private int _position = 0;
    private int _lineNumber = 1;

    public bool SupportsReset => true;

    public bool HasMoreData => _position < _memory.Length;

    /// <summary>
    /// Ultra-fast line counting without parsing
    /// </summary>
    public int CountLinesDirectly()
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
}

/// <summary>
/// Data source for stream-based content
/// </summary>
internal sealed class StreamDataSource(Stream stream, Encoding? encoding = null, bool leaveOpen = false) : ICsvDataSource
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly StreamReader _reader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: leaveOpen);
    private readonly bool _leaveOpen = leaveOpen;
    private int _lineNumber = 1;
    private bool _disposed;

    public bool SupportsReset => _stream.CanSeek;

    public bool HasMoreData => !_reader.EndOfStream;

    /// <summary>
    /// Counts lines in stream by reading entire content
    /// </summary>
    public int CountLinesDirectly()
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot count lines in non-seekable stream");
        }

        var currentPosition = _stream.Position;
        _stream.Position = 0;
        _reader.DiscardBufferedData();

        try
        {
            var content = _reader.ReadToEnd();
            if (string.IsNullOrEmpty(content)) return 0;
            
            var newlineCount = CsvParser.CountLines(content.AsSpan());
            
            // If content doesn't end with newline, there's one more line
            if (content.Length > 0 && content[content.Length - 1] != '\n' && content[content.Length - 1] != '\r')
            {
                return newlineCount + 1;
            }
            
            return newlineCount;
        }
        finally
        {
            _stream.Position = currentPosition;
            _reader.DiscardBufferedData();
        }
    }

    public bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber)
    {
        lineNumber = _lineNumber;

        var lineStr = _reader.ReadLine();
        if (lineStr == null)
        {
            line = default;
            return false;
        }

        _lineNumber++;
        line = lineStr.AsSpan();
        return true;
    }

    public bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber)
    {
        // StreamDataSource can't support zero-copy
        throw new NotSupportedException("StreamDataSource does not support zero-copy line access");
    }

    public ReadOnlySpan<char> GetBuffer()
    {
        // StreamDataSource doesn't have a buffer
        throw new NotSupportedException("StreamDataSource does not have a buffer for zero-copy access");
    }

    public void Reset()
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot reset a non-seekable stream");
        }

        _stream.Position = 0;
        _reader.DiscardBufferedData();
        _lineNumber = 1;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
        {
            _reader?.Dispose();
            _stream?.Dispose();
        }

        _disposed = true;
    }
}