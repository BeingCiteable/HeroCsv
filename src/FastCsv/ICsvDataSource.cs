using System.Text;

namespace FastCsv;

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
}

/// <summary>
/// Data source for string-based content
/// </summary>
internal sealed class StringDataSource : ICsvDataSource
{
    private readonly string _content;
    private int _position;
    private int _lineNumber;
    
    public StringDataSource(string content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _position = 0;
        _lineNumber = 1;
    }
    
    public bool SupportsReset => true;
    
    public bool HasMoreData => _position < _content.Length;
    
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
internal sealed class MemoryDataSource : ICsvDataSource
{
    private readonly ReadOnlyMemory<char> _memory;
    private int _position;
    private int _lineNumber;
    
    public MemoryDataSource(ReadOnlyMemory<char> memory)
    {
        _memory = memory;
        _position = 0;
        _lineNumber = 1;
    }
    
    public bool SupportsReset => true;
    
    public bool HasMoreData => _position < _memory.Length;
    
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
            line = ReadOnlySpan<char>.Empty;
            return true;
        }
        
        line = fullSpan.Slice(_position, lineEnd - _position);
        _position = CsvParser.SkipLineEnding(fullSpan, lineEnd);
        _lineNumber++;
        return true;
    }
    
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
internal sealed class StreamDataSource : ICsvDataSource
{
    private readonly Stream _stream;
    private readonly StreamReader _reader;
    private readonly bool _leaveOpen;
    private int _lineNumber;
    private bool _disposed;
    
    public StreamDataSource(Stream stream, Encoding? encoding = null, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _reader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: leaveOpen);
        _leaveOpen = leaveOpen;
        _lineNumber = 1;
    }
    
    public bool SupportsReset => _stream.CanSeek;
    
    public bool HasMoreData => !_reader.EndOfStream;
    
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