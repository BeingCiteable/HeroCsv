using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastCsv.DataSources;
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Integration tests for data source functionality across different .NET versions
/// </summary>
public class DataSourceIntegrationTests
{
    #region StringDataSource Tests

    [Fact]
    public void StringDataSource_Basic()
    {
        using var source = new StringDataSource("line1\nline2\nline3");

        Assert.True(source.HasMoreData);
        Assert.True(source.SupportsReset);

        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNum);

        Assert.True(source.TryReadLine(out line, out lineNum));
        Assert.Equal("line2", line.ToString());
        Assert.Equal(2, lineNum);

        Assert.True(source.TryReadLine(out line, out lineNum));
        Assert.Equal("line3", line.ToString());
        Assert.Equal(3, lineNum);

        Assert.False(source.TryReadLine(out _, out _));
        Assert.False(source.HasMoreData);
    }

    [Fact]
    public void StringDataSource_Reset()
    {
        using var source = new StringDataSource("line1\nline2");

        source.TryReadLine(out _, out _);
        source.TryReadLine(out _, out _);

        source.Reset();

        Assert.True(source.HasMoreData);
        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNum);
    }

    [Fact]
    public void StringDataSource_CountLinesDirectly()
    {
        using var source = new StringDataSource("line1\nline2\nline3");
        var count = source.CountLinesDirectly();
        Assert.Equal(3, count); // 3 lines
    }

    [Fact]
    public void StringDataSource_GetBuffer()
    {
        var content = "test content";
        using var source = new StringDataSource(content);
        var buffer = source.GetBuffer();
        Assert.Equal(content, buffer.ToString());
    }

    [Fact]
    public void StringDataSource_EmptyString()
    {
        using var source = new StringDataSource("");
        Assert.False(source.HasMoreData);
        Assert.False(source.TryReadLine(out _, out _));
        Assert.Equal(0, source.CountLinesDirectly());
    }

    [Fact]
    public void StringDataSource_SingleLineNoNewline()
    {
        using var source = new StringDataSource("single line");
        Assert.True(source.HasMoreData);
        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("single line", line.ToString());
        Assert.Equal(1, lineNum);
        Assert.False(source.TryReadLine(out _, out _));
    }

    [Fact]
    public void StringDataSource_WindowsLineEndings()
    {
        using var source = new StringDataSource("line1\r\nline2\r\nline3");

        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());

        Assert.True(source.TryReadLine(out line, out _));
        Assert.Equal("line2", line.ToString());

        Assert.True(source.TryReadLine(out line, out _));
        Assert.Equal("line3", line.ToString());
    }

    #endregion

    #region MemoryDataSource Tests

    [Fact]
    public void MemoryDataSource_Basic()
    {
        var content = "line1\nline2\nline3".AsMemory();
        using var source = new MemoryDataSource(content);

        Assert.True(source.HasMoreData);
        Assert.True(source.SupportsReset);

        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNum);
    }

    [Fact]
    public void MemoryDataSource_Reset()
    {
        var content = "line1\nline2".AsMemory();
        using var source = new MemoryDataSource(content);

        source.TryReadLine(out _, out _);
        source.TryReadLine(out _, out _);

        source.Reset();

        Assert.True(source.HasMoreData);
        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());
    }

    [Fact]
    public void MemoryDataSource_CountLinesDirectly()
    {
        var content = "line1\nline2\nline3".AsMemory();
        using var source = new MemoryDataSource(content);
        var count = source.CountLinesDirectly();
        Assert.Equal(3, count);
    }

    [Fact]
    public void MemoryDataSource_GetBuffer()
    {
        var content = "test content".AsMemory();
        using var source = new MemoryDataSource(content);
        var buffer = source.GetBuffer();
        Assert.Equal("test content", buffer.ToString());
    }

    [Fact]
    public void MemoryDataSource_EmptyMemory()
    {
        var content = ReadOnlyMemory<char>.Empty;
        using var source = new MemoryDataSource(content);
        Assert.False(source.HasMoreData);
        Assert.False(source.TryReadLine(out _, out _));
    }

    #endregion

    #region StreamDataSource Tests

    [Fact]
    public void StreamDataSource_Basic()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new StreamDataSource(stream);

        Assert.True(source.HasMoreData);
        Assert.True(source.SupportsReset);

        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNum);
    }

    [Fact]
    public void StreamDataSource_WithEncoding()
    {
        var content = "line1\nline2";
        using var stream = new MemoryStream(Encoding.UTF32.GetBytes(content));
        using var source = new StreamDataSource(stream, Encoding.UTF32);

        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());
    }

    [Fact]
    public void StreamDataSource_LeaveOpen()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        using (var source = new StreamDataSource(stream, leaveOpen: true))
        {
            source.TryReadLine(out _, out _);
        }

        // Stream should still be open
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public void StreamDataSource_NonSeekableStream()
    {
        using var stream = new NonSeekableStream();
        using var source = new StreamDataSource(stream);

        Assert.False(source.SupportsReset);
        Assert.Throws<NotSupportedException>(() => source.Reset());
    }

    [Fact]
    public void StreamDataSource_Reset_SeekableStream()
    {
        var content = "line1\nline2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new StreamDataSource(stream);

        source.TryReadLine(out _, out _);
        source.TryReadLine(out _, out _);

        source.Reset();

        Assert.True(source.HasMoreData);
        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());
    }

    [Fact]
    public void StreamDataSource_CountLinesDirectly_Seekable()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new StreamDataSource(stream);

        var count = source.CountLinesDirectly();
        Assert.Equal(3, count); // Returns line count, not newline count

        // Should still be able to read after counting
        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());
    }

    [Fact]
    public void StreamDataSource_CountLinesDirectly_NonSeekable()
    {
        using var stream = new NonSeekableStream();
        using var source = new StreamDataSource(stream);

        Assert.Throws<NotSupportedException>(() => source.CountLinesDirectly());
    }

    [Fact]
    public void StreamDataSource_GetBuffer_NotSupported()
    {
        using var stream = new MemoryStream();
        using var source = new StreamDataSource(stream);

        Assert.Throws<NotSupportedException>(() => source.GetBuffer());
    }

    [Fact]
    public void StreamDataSource_EmptyStream()
    {
        using var stream = new MemoryStream();
        using var source = new StreamDataSource(stream);

        Assert.False(source.HasMoreData);
        Assert.False(source.TryReadLine(out _, out _));
    }

    #endregion

    #region AsyncStreamDataSource Tests

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncStreamDataSource_Basic()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        Assert.True(source.HasMoreData);

        var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);
        Assert.Equal(1, result.lineNumber);
    }

    [Fact]
    public async Task AsyncStreamDataSource_CountLinesAsync()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        var count = await source.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, count);
    }

    [Fact]
    public void AsyncStreamDataSource_SyncMethods()
    {
        var content = "line1\nline2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        Assert.True(source.TryReadLine(out var line, out var lineNum));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNum);

        source.Reset();
        Assert.True(source.HasMoreData);
    }

    [Fact]
    public void AsyncStreamDataSource_NonSeekable()
    {
        using var stream = new NonSeekableStream();
        using var source = new AsyncStreamDataSource(stream);

        Assert.False(source.SupportsReset);
        Assert.Throws<NotSupportedException>(() => source.Reset());
    }

    [Fact]
    public async Task AsyncStreamDataSource_WithCancellation()
    {
        var content = "line1\nline2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);
        using var cts = new CancellationTokenSource();

        var result = await source.TryReadLineAsync(cts.Token);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);
    }

    [Fact]
    public void AsyncStreamDataSource_LeaveOpen()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        using (var source = new AsyncStreamDataSource(stream, leaveOpen: true))
        {
            source.TryReadLine(out _, out _);
        }

        Assert.True(stream.CanRead);
        stream.Dispose();
    }
#endif

    #endregion

    #region AsyncMemoryDataSource Tests

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncMemoryDataSource_Basic()
    {
        var innerSource = new StringDataSource("line1\nline2");
        using var source = new AsyncMemoryDataSource(innerSource);

        var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);

        result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result.success);
        Assert.Equal("line2", result.line);

        result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.False(result.success);
    }

    [Fact]
    public async Task AsyncMemoryDataSource_CountLines()
    {
        var innerSource = new StringDataSource("line1\nline2\nline3");
        using var source = new AsyncMemoryDataSource(innerSource);

        var count = await source.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, count);
    }

    [Fact]
    public void AsyncMemoryDataSource_GetBuffer()
    {
        var innerSource = new StringDataSource("test content");
        using var source = new AsyncMemoryDataSource(innerSource);

        var buffer = source.GetBuffer();
        Assert.Equal("test content", buffer.ToString());
    }

    [Fact]
    public void AsyncMemoryDataSource_Reset()
    {
        var innerSource = new StringDataSource("line1\nline2");
        using var source = new AsyncMemoryDataSource(innerSource);

        source.TryReadLine(out _, out _);
        source.TryReadLine(out _, out _);

        source.Reset();

        Assert.True(source.HasMoreData);
        Assert.True(source.TryReadLine(out var line, out _));
        Assert.Equal("line1", line.ToString());
    }

    [Fact]
    public async Task AsyncMemoryDataSource_WithMemoryDataSource()
    {
        var innerSource = new MemoryDataSource("line1\nline2".AsMemory());
        using var source = new AsyncMemoryDataSource(innerSource);

        var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);
    }
#endif

    #endregion

    #region Helper Classes

    private class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner = new MemoryStream(Encoding.UTF8.GetBytes("test\ndata"));

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    #endregion
}