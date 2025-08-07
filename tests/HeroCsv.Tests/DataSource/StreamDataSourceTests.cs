using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv.DataSources;
using Xunit;

namespace HeroCsv.Tests.DataSource;

public class StreamDataSourceTests
{
    public class ConstructorTests
    {
        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StreamDataSource(null!));
        }

        [Fact]
        public void Constructor_ValidStream_InitializesCorrectly()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            // Act
            using var source = new StreamDataSource(stream);

            // Assert
            Assert.True(source.HasMoreData);
            Assert.True(source.SupportsReset); // MemoryStream is seekable
        }

        [Fact]
        public void Constructor_NonSeekableStream_DoesNotSupportReset()
        {
            // Arrange
            using var stream = new NonSeekableMemoryStream("test");

            // Act
            using var source = new StreamDataSource(stream);

            // Assert
            Assert.False(source.SupportsReset);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_LeaveOpenParameter_RespectsFlag(bool leaveOpen)
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            // Act
            using (var source = new StreamDataSource(stream, leaveOpen: leaveOpen))
            {
                source.TryReadLine(out _, out _);
            }

            // Assert
            if (leaveOpen)
            {
                Assert.True(stream.CanRead); // Should still be open
                stream.Dispose(); // Clean up
            }
            else
            {
                Assert.False(stream.CanRead); // Should be disposed
            }
        }

        [Fact]
        public void StreamDataSource_LeaveOpen_FromDataSourceIntegrationTests()
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
    }

    public class SynchronousReadingTests
    {
        [Fact]
        public void TryReadLine_SingleLine_ReturnsCorrectData()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
            using var source = new StreamDataSource(stream);

            // Act
            var success = source.TryReadLine(out var line, out var lineNumber);

            // Assert
            Assert.True(success);
            Assert.Equal("Hello World", line.ToString());
            Assert.Equal(1, lineNumber);
        }

        [Fact]
        public void TryReadLine_MultipleLines_ReturnsEachLine()
        {
            // Arrange
            var content = "Line1\nLine2\nLine3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);
            var lines = new List<string>();

            // Act
            while (source.TryReadLine(out var line, out _))
            {
                lines.Add(line.ToString());
            }

            // Assert
            Assert.Equal(3, lines.Count);
            Assert.Equal("Line1", lines[0]);
            Assert.Equal("Line2", lines[1]);
            Assert.Equal("Line3", lines[2]);
        }

        [Fact]
        public void TryReadLine_EmptyStream_ReturnsFalse()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var source = new StreamDataSource(stream);

            // Act
            var success = source.TryReadLine(out var line, out var lineNumber);

            // Assert
            Assert.False(success);
            Assert.Equal(default, line);
            Assert.Equal(1, lineNumber);
        }

        [Theory]
        [InlineData("Line1\nLine2", 2)]
        [InlineData("Line1\r\nLine2", 2)]
        [InlineData("Line1\rLine2", 2)]
        [InlineData("\n\n", 2)]
        public void TryReadLine_VariousLineEndings_HandlesCorrectly(string content, int expectedLines)
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);
            var lineCount = 0;

            // Act
            while (source.TryReadLine(out _, out _))
            {
                lineCount++;
            }

            // Assert
            Assert.Equal(expectedLines, lineCount);
        }
    }

    public class CountLinesTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("Single line", 1)]
        [InlineData("Line1\nLine2", 2)]
        [InlineData("Line1\r\nLine2\r\n", 2)]
        [InlineData("\n\n\n", 3)]
        public void CountLines_VariousInputs_ReturnsCorrectCount(string content, int expectedCount)
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Act
            var count = source.CountLines();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        [Fact]
        public void CountLines_NonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableMemoryStream("test\ndata");
            using var source = new StreamDataSource(stream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => source.CountLines());
        }

        [Fact]
        public void CountLines_RestoresStreamPosition()
        {
            // Arrange
            var content = "Line1\nLine2\nLine3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Read first line
            source.TryReadLine(out var firstLine, out _);

            // Act
            var count = source.CountLines();

            // Assert
            Assert.Equal(3, count);
            // Should still be able to read second line
            var success = source.TryReadLine(out var secondLine, out _);
            Assert.True(success);
            Assert.Equal("Line2", secondLine.ToString());
        }
    }

    public class ResetTests
    {
        [Fact]
        public void Reset_SeekableStream_RestartsFromBeginning()
        {
            // Arrange
            var content = "Line1\nLine2\nLine3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Read some lines
            source.TryReadLine(out _, out _);
            source.TryReadLine(out _, out _);

            // Act
            source.Reset();

            // Assert
            Assert.True(source.HasMoreData);
            var success = source.TryReadLine(out var line, out var lineNumber);
            Assert.True(success);
            Assert.Equal("Line1", line.ToString());
            Assert.Equal(1, lineNumber);
        }

        [Fact]
        public void Reset_NonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableMemoryStream("test");
            using var source = new StreamDataSource(stream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => source.Reset());
        }
    }

    public class UnsupportedOperationTests
    {
        [Fact]
        public void TryGetLinePosition_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            using var source = new StreamDataSource(stream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                source.TryGetLinePosition(out _, out _, out _));
        }

        [Fact]
        public void GetBuffer_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            using var source = new StreamDataSource(stream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => source.GetBuffer());
        }
    }

    public class EncodingTests
    {
        [Fact]
        public void Constructor_CustomEncoding_ReadsCorrectly()
        {
            // Arrange
            var text = "Héllo Wörld"; // Contains non-ASCII characters
            var bytes = Encoding.UTF8.GetBytes(text);
            using var stream = new MemoryStream(bytes);

            // Act
            using var source = new StreamDataSource(stream, Encoding.UTF8);
            var success = source.TryReadLine(out var line, out _);

            // Assert
            Assert.True(success);
            Assert.Equal(text, line.ToString());
        }

        [Fact]
        public void Constructor_DefaultEncoding_UsesUTF8()
        {
            // Arrange
            var text = "Test with émojis 🚀";
            var bytes = Encoding.UTF8.GetBytes(text);
            using var stream = new MemoryStream(bytes);

            // Act
            using var source = new StreamDataSource(stream); // No encoding specified
            var success = source.TryReadLine(out var line, out _);

            // Assert
            Assert.True(success);
            Assert.Equal(text, line.ToString());
        }
    }

#if NET6_0_OR_GREATER
    public class AsynchronousTests
    {
        [Fact]
        public async Task TryReadLineAsync_SingleLine_ReturnsCorrectData()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Async test"));
            using var source = new StreamDataSource(stream);

            // Act
            var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Async test", result.line);
            Assert.Equal(1, result.lineNumber);
        }

        [Fact]
        public async Task TryReadLineAsync_BasicAsync_FromDataSourceIntegrationTests()
        {
            var content = "line1\nline2\nline3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            Assert.True(source.HasMoreData);

            var (success, line, lineNumber) = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
            Assert.True(success);
            Assert.Equal("line1", line);
            Assert.Equal(1, lineNumber);
        }

        [Fact]
        public async Task StreamDataSource_CountLinesAsync_FromDataSourceIntegrationTests()
        {
            var content = "line1\nline2\nline3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            var count = await source.CountLinesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(3, count);
        }

        [Fact]
        public void StreamDataSource_Reset_FromDataSourceIntegrationTests()
        {
            var content = "line1\nline2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Read first line
            source.TryReadLine(out var line, out var lineNumber);

            source.Reset();
            Assert.True(source.HasMoreData);
        }

        [Fact]
        public void StreamDataSource_NonSeekable_FromDataSourceIntegrationTests()
        {
            using var stream = new NonSeekableMemoryStream("test");
            using var source = new StreamDataSource(stream);

            Assert.False(source.SupportsReset);
            Assert.Throws<NotSupportedException>(() => source.Reset());
        }

        [Fact]
        public async Task StreamDataSource_WithCancellation_FromDataSourceIntegrationTests()
        {
            var content = "line1\nline2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);
            using var cts = new CancellationTokenSource();

            var (success, line, lineNumber) = await source.TryReadLineAsync(cts.Token);
            Assert.True(success);
            Assert.Equal("line1", line);
        }

        [Fact]
        public void StreamDataSource_LeaveOpenAsync_FromDataSourceIntegrationTests()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            using (var source = new StreamDataSource(stream, leaveOpen: true))
            {
                source.TryReadLine(out var _, out var _);
            }

            Assert.True(stream.CanRead);
            stream.Dispose();
        }

        [Fact]
        public async Task StreamDataSource_AllPaths_FromDataSourceBoundaryTests()
        {
            var content = "line1\nline2\nline3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Test counting
            var count = await source.CountLinesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(3, count);

            // Reset and read
            source.Reset();

            var (success, line, lineNumber) = await source.TryReadLineAsync(CancellationToken.None);
            Assert.True(success);
            Assert.Equal("line1", line);
        }

        [Fact]
        public async Task TryReadLineAsync_MultipleLines_ReadsSequentially()
        {
            // Arrange
            var content = "Line1\nLine2\nLine3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);
            var lines = new List<string>();

            // Act
            while (true)
            {
                var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
                if (!result.success) break;
                lines.Add(result.line);
            }

            // Assert
            Assert.Equal(3, lines.Count);
            Assert.Equal("Line1", lines[0]);
            Assert.Equal("Line2", lines[1]);
            Assert.Equal("Line3", lines[2]);
        }

        [Fact]
        public async Task TryReadLineAsync_WithCancellation_RespectsToken()
        {
            // Arrange
            var content = "Line1\nLine2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);
            using var cts = new CancellationTokenSource();

            // Act
            var firstResult = await source.TryReadLineAsync(cts.Token);
            cts.Cancel();

            // Assert
            Assert.True(firstResult.success);
            Assert.Equal("Line1", firstResult.line);

            // Second read should respect cancellation
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => source.TryReadLineAsync(cts.Token).AsTask());
        }

        [Fact]
        public async Task CountLinesAsync_ReturnsCorrectCount()
        {
            // Arrange
            var content = "Line1\nLine2\nLine3\nLine4";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var source = new StreamDataSource(stream);

            // Act
            var count = await source.CountLinesAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task CountLinesAsync_WithCancellation_CanBeCancelled()
        {
            // Arrange
            var longContent = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line{i}"));
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(longContent));
            using var source = new StreamDataSource(stream);

            // Use a pre-cancelled token to ensure cancellation
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                source.CountLinesAsync(cts.Token).AsTask());
        }

        [Fact]
        public async Task DisposeAsync_DisposesCorrectly()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            var source = new StreamDataSource(stream);

            // Act
            await source.DisposeAsync();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => source.TryReadLine(out _, out _));
        }
    }
#endif

    // Helper class for testing non-seekable streams
    private class NonSeekableMemoryStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableMemoryStream(string content)
        {
            _inner = new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false; // Non-seekable
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner?.Dispose();
            base.Dispose(disposing);
        }
    }
}