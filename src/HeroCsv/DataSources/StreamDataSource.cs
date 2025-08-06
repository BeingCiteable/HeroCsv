using System.Text;

namespace HeroCsv.DataSources;

/// <summary>
/// Data source for stream-based content using synchronous operations
/// </summary>
internal sealed class StreamDataSource(Stream stream, Encoding? encoding = null, bool leaveOpen = false) : ICsvDataSource
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly Encoding _encoding = encoding ?? Encoding.UTF8;
    private StreamReader _reader = new StreamReader(
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
    /// Counts lines in stream using synchronous read operations
    /// </summary>
    public int CountLines()
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot count lines in non-seekable stream");
        }

        // Save the current line number
        var currentLineNumber = _lineNumber;
        
        // Reset to beginning to count all lines
        _stream.Position = 0;
        _reader = new StreamReader(_stream, _encoding, true, 4096, _leaveOpen);
        _lineNumber = 1;

        var lineCount = 0;
        while (_reader.ReadLine() != null)
        {
            lineCount++;
        }
        
        // Reset to beginning and advance to where we were
        _stream.Position = 0;
        _reader = new StreamReader(_stream, _encoding, true, 4096, _leaveOpen);
        _lineNumber = 1;
        
        // Skip lines to get back to where we were
        for (int i = 1; i < currentLineNumber; i++)
        {
            _reader.ReadLine();
            _lineNumber++;
        }
        
        return lineCount;
    }

    /// <summary>
    /// Read the next line using native synchronous StreamReader methods
    /// </summary>
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

        _reader.Dispose();
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }

        _disposed = true;
    }

#if NET6_0_OR_GREATER
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _reader.Dispose();
        if (!_leaveOpen)
        {
            if (_stream is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _stream.Dispose();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Asynchronously read the next line using native async methods
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
    /// Asynchronously count lines in the stream
    /// </summary>
    public async ValueTask<int> CountLinesAsync(CancellationToken cancellationToken = default)
    {
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Cannot count lines in non-seekable stream");
        }

        // Save the current line number
        var currentLineNumber = _lineNumber;
        
        // Reset to beginning to count all lines
        _stream.Position = 0;
        _reader = new StreamReader(_stream, _encoding, true, 4096, _leaveOpen);
        _lineNumber = 1;

        var lineCount = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
#if NET7_0_OR_GREATER
            var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            var line = await _reader.ReadLineAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
#endif
            if (line == null) break;
            lineCount++;
        }
        
        // Reset to beginning and advance to where we were
        _stream.Position = 0;
        _reader = new StreamReader(_stream, _encoding, true, 4096, _leaveOpen);
        _lineNumber = 1;
        
        // Skip lines to get back to where we were
        for (int i = 1; i < currentLineNumber; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
            await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            await _reader.ReadLineAsync().ConfigureAwait(false);
#endif
            _lineNumber++;
        }
        
        return lineCount;
    }
#endif
}