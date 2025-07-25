#if NET6_0_OR_GREATER
using System.Text;

namespace FastCsv;

/// <summary>
/// Async implementation for StreamDataSource
/// </summary>
internal sealed class AsyncStreamDataSource : IAsyncCsvDataSource
{
    private readonly Stream _stream;
    private readonly StreamReader _reader;
    private readonly bool _leaveOpen;
    private int _lineNumber;
    private bool _disposed;
    
    public AsyncStreamDataSource(Stream stream, Encoding? encoding = null, bool leaveOpen = false)
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
    
    /// <summary>
    /// Asynchronously read the next line
    /// </summary>
    public async ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsync(CancellationToken cancellationToken = default)
    {
        if (_reader.EndOfStream)
        {
            return (false, string.Empty, 0);
        }
        
#if NET7_0_OR_GREATER
        var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
        var line = await _reader.ReadLineAsync().ConfigureAwait(false);
#endif
        if (line == null)
        {
            return (false, string.Empty, 0);
        }
        
        var currentLineNumber = _lineNumber++;
        return (true, line, currentLineNumber);
    }
    
    /// <summary>
    /// Asynchronously count lines
    /// </summary>
    public async ValueTask<int> CountLinesDirectlyAsync(CancellationToken cancellationToken = default)
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot count lines in non-seekable stream");
        }
        
        var originalPosition = _stream.Position;
        _stream.Position = 0;
        
        var buffer = new char[4096];
        var lineCount = 0;
        
        using var tempReader = new StreamReader(_stream, _reader.CurrentEncoding, false, buffer.Length, true);
        
        while (!cancellationToken.IsCancellationRequested)
        {
#if NET7_0_OR_GREATER
            var line = await tempReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            var line = await tempReader.ReadLineAsync().ConfigureAwait(false);
#endif
            if (line == null) break;
            lineCount++;
        }
        
        _stream.Position = originalPosition;
        return lineCount;
    }
    
    // Sync methods (required by interface)
    public bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber)
    {
        // For async source, we need to read synchronously which isn't ideal
        var result = TryReadLineAsync().GetAwaiter().GetResult();
        if (result.success)
        {
            line = result.line.AsSpan();
            lineNumber = result.lineNumber;
            return true;
        }
        line = default;
        lineNumber = 0;
        return false;
    }
    
    public bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber)
    {
        // Not supported for stream-based source
        lineStart = 0;
        lineLength = 0;
        lineNumber = 0;
        return false;
    }
    
    public ReadOnlySpan<char> GetBuffer()
    {
        throw new NotSupportedException("AsyncStreamDataSource does not have a buffer for zero-copy access");
    }
    
    public void Reset()
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot reset non-seekable stream");
        }
        
        _stream.Position = 0;
        _reader.DiscardBufferedData();
        _lineNumber = 1;
    }
    
    public int CountLinesDirectly()
    {
        return CountLinesDirectlyAsync().GetAwaiter().GetResult();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _reader.Dispose();
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
        _disposed = true;
    }
}

/// <summary>
/// Async wrapper for string and memory data sources
/// </summary>
internal sealed class AsyncMemoryDataSource : IAsyncCsvDataSource
{
    private readonly ICsvDataSource _innerSource;
    
    public AsyncMemoryDataSource(ICsvDataSource innerSource)
    {
        _innerSource = innerSource;
    }
    
    public bool SupportsReset => _innerSource.SupportsReset;
    public bool HasMoreData => _innerSource.HasMoreData;
    
    public ValueTask<(bool success, string line, int lineNumber)> TryReadLineAsync(CancellationToken cancellationToken = default)
    {
        return _innerSource.TryReadLineAsyncDefault(cancellationToken);
    }
    
    public ValueTask<int> CountLinesDirectlyAsync(CancellationToken cancellationToken = default)
    {
        return _innerSource.CountLinesDirectlyAsyncDefault(cancellationToken);
    }
    
    // Delegate all sync methods
    public bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber) => _innerSource.TryReadLine(out line, out lineNumber);
    public bool TryGetLinePosition(out int lineStart, out int lineLength, out int lineNumber) => _innerSource.TryGetLinePosition(out lineStart, out lineLength, out lineNumber);
    public ReadOnlySpan<char> GetBuffer() => _innerSource.GetBuffer();
    public void Reset() => _innerSource.Reset();
    public int CountLinesDirectly() => _innerSource.CountLinesDirectly();
    public void Dispose() => _innerSource.Dispose();
}
#endif