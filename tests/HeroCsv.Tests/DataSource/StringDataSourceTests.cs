using HeroCsv.DataSources;
using Xunit;

namespace HeroCsv.Tests.DataSource;

public class StringDataSourceTests
{
    public class ConstructorTests
    {
        [Fact]
        public void Constructor_NullContent_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StringDataSource(null!));
        }

        [Fact]
        public void Constructor_EmptyContent_CreatesValidSource()
        {
            // Act
            using var source = new StringDataSource("");

            // Assert
            Assert.False(source.HasMoreData);
            Assert.True(source.SupportsReset);
        }

        [Fact]
        public void Constructor_ValidContent_InitializesCorrectly()
        {
            // Act
            using var source = new StringDataSource("test content");

            // Assert
            Assert.True(source.HasMoreData);
            Assert.True(source.SupportsReset);
        }
    }

    public class ReadingTests
    {
        [Fact]
        public void TryReadLine_SingleLine_ReturnsCorrectData()
        {
            // Arrange
            using var source = new StringDataSource("Hello World");

            // Act
            var success = source.TryReadLine(out var line, out var lineNumber);

            // Assert
            Assert.True(success);
            Assert.Equal("Hello World", line.ToString());
            Assert.Equal(1, lineNumber);
            Assert.False(source.HasMoreData);
        }

        [Fact]
        public void StringDataSource_Basic_FromDataSourceIntegrationTests()
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
        public void StringDataSource_SingleLineNoNewline_FromDataSourceIntegrationTests()
        {
            using var source = new StringDataSource("single line");
            Assert.True(source.HasMoreData);
            Assert.True(source.TryReadLine(out var line, out var lineNum));
            Assert.Equal("single line", line.ToString());
            Assert.Equal(1, lineNum);
            Assert.False(source.TryReadLine(out _, out _));
        }

        [Fact]
        public void StringDataSource_WindowsLineEndings_FromDataSourceIntegrationTests()
        {
            using var source = new StringDataSource("line1\r\nline2\r\nline3");

            Assert.True(source.TryReadLine(out var line, out _));
            Assert.Equal("line1", line.ToString());

            Assert.True(source.TryReadLine(out line, out _));
            Assert.Equal("line2", line.ToString());

            Assert.True(source.TryReadLine(out line, out _));
            Assert.Equal("line3", line.ToString());
        }

        [Fact]
        public void StringDataSource_EmptyString_FromDataSourceIntegrationTests()
        {
            using var source = new StringDataSource("");
            Assert.False(source.HasMoreData);
            Assert.False(source.TryReadLine(out _, out _));
            Assert.Equal(0, source.CountLines());
        }

        [Fact]
        public void TryReadLine_MultipleLines_ReturnsEachLine()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2\nLine3");
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

        [Theory]
        [InlineData("Line1\nLine2", 2)]
        [InlineData("Line1\r\nLine2", 2)]
        [InlineData("Line1\rLine2", 2)]
        [InlineData("Line1\n\nLine3", 3)]
        [InlineData("\n\n", 2)]
        [InlineData("", 0)]
        public void TryReadLine_VariousLineEndings_HandlesCorrectly(string content, int expectedLines)
        {
            // Arrange
            using var source = new StringDataSource(content);
            var lineCount = 0;

            // Act
            while (source.TryReadLine(out _, out _))
            {
                lineCount++;
            }

            // Assert
            Assert.Equal(expectedLines, lineCount);
        }

        [Fact]
        public void TryReadLine_TrackLineNumbers_IncrementsCorrectly()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2\nLine3");
            var lineNumbers = new List<int>();

            // Act
            while (source.TryReadLine(out _, out var lineNumber))
            {
                lineNumbers.Add(lineNumber);
            }

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, lineNumbers);
        }
    }

    public class CountLinesTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("One line", 1)]
        [InlineData("Line1\nLine2", 2)]
        [InlineData("Line1\r\nLine2\r\n", 2)]
        [InlineData("Line1\rLine2\r", 2)]
        [InlineData("\n\n\n", 3)]
        [InlineData("NoNewlineAtEnd", 1)]
        [InlineData("WithNewlineAtEnd\n", 1)]
        public void CountLines_VariousInputs_ReturnsCorrectCount(string content, int expectedCount)
        {
            // Arrange
            using var source = new StringDataSource(content);

            // Act
            var count = source.CountLines();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        [Fact]
        public void StringDataSource_CountLines_FromDataSourceIntegrationTests()
        {
            using var source = new StringDataSource("line1\nline2\nline3");
            var count = source.CountLines();
            Assert.Equal(3, count); // 3 lines
        }

        [Fact]
        public void CountLines_DoesNotAffectReadPosition()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2");

            // Act
            var count = source.CountLines();
            var success = source.TryReadLine(out var line, out _);

            // Assert
            Assert.Equal(2, count);
            Assert.True(success);
            Assert.Equal("Line1", line.ToString());
        }
    }

    public class ResetTests
    {
        [Fact]
        public void Reset_AfterPartialRead_RestartsFromBeginning()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2\nLine3");
            source.TryReadLine(out _, out _); // Read first line
            source.TryReadLine(out _, out _); // Read second line

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
        public void StringDataSource_Reset_FromDataSourceIntegrationTests()
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
        public void Reset_AfterFullRead_AllowsReReading()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2");
            while (source.TryReadLine(out _, out _)) { } // Read all

            // Act
            source.Reset();

            // Assert
            Assert.True(source.HasMoreData);
            var lines = new List<string>();
            while (source.TryReadLine(out var line, out _))
            {
                lines.Add(line.ToString());
            }
            Assert.Equal(2, lines.Count);
        }
    }

    public class BufferAccessTests
    {
        [Fact]
        public void GetBuffer_ReturnsFullContent()
        {
            // Arrange
            var content = "This is the full content\nWith multiple lines";
            using var source = new StringDataSource(content);

            // Act
            var buffer = source.GetBuffer();

            // Assert
            Assert.Equal(content, buffer.ToString());
        }

        [Fact]
        public void StringDataSource_GetBuffer_FromDataSourceIntegrationTests()
        {
            var content = "test content";
            using var source = new StringDataSource(content);
            var buffer = source.GetBuffer();
            Assert.Equal(content, buffer.ToString());
        }

        [Fact]
        public void TryGetLinePosition_ReturnsCorrectPositions()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2\nLine3");

            // Act & Assert
            Assert.True(source.TryGetLinePosition(out var start, out var length, out var lineNum));
            Assert.Equal(0, start);
            Assert.Equal(5, length);
            Assert.Equal(1, lineNum);

            source.TryReadLine(out _, out _); // Move to next line

            Assert.True(source.TryGetLinePosition(out start, out length, out lineNum));
            Assert.Equal(6, start);
            Assert.Equal(5, length);
            Assert.Equal(2, lineNum);
        }

        [Fact]
        public void TryGetLinePosition_AtEnd_ReturnsFalse()
        {
            // Arrange
            using var source = new StringDataSource("Single line");
            source.TryReadLine(out _, out _); // Read the line

            // Act
            var result = source.TryGetLinePosition(out _, out _, out _);

            // Assert
            Assert.False(result);
        }
    }

    public class EdgeCaseTests
    {
        [Fact]
        public void TryReadLine_VeryLongLine_HandlesCorrectly()
        {
            // Arrange
            var longLine = new string('x', 10000);
            using var source = new StringDataSource(longLine);

            // Act
            var success = source.TryReadLine(out var line, out _);

            // Assert
            Assert.True(success);
            Assert.Equal(longLine, line.ToString());
        }

        [Fact]
        public void StringDataSource_AllScenarios_FromDataSourceBoundaryTests()
        {
            // Test counting lines with various endings
            using (var source = new StringDataSource("line1\r\nline2\nline3\rline4"))
            {
                var count = source.CountLines();
                Assert.Equal(4, count); // 4 lines: line1, line2, line3, line4
            }

            // Test empty lines
            using (var source = new StringDataSource("\n\n\n"))
            {
                Assert.True(source.TryReadLine(out var line, out _));
                Assert.Equal("", line.ToString());

                Assert.True(source.TryReadLine(out line, out _));
                Assert.Equal("", line.ToString());
            }

            // Test line ending at buffer end
            using (var source = new StringDataSource("no newline at end"))
            {
                Assert.True(source.TryReadLine(out var line, out _));
                Assert.Equal("no newline at end", line.ToString());
                Assert.False(source.TryReadLine(out _, out _));
            }
        }

        [Fact]
        public void TryReadLine_MixedLineEndings_ParsesCorrectly()
        {
            // Arrange
            using var source = new StringDataSource("Unix\nWindows\r\nOldMac\rLast");
            var lines = new List<string>();

            // Act
            while (source.TryReadLine(out var line, out _))
            {
                lines.Add(line.ToString());
            }

            // Assert
            Assert.Equal(4, lines.Count);
            Assert.Equal("Unix", lines[0]);
            Assert.Equal("Windows", lines[1]);
            Assert.Equal("OldMac", lines[2]);
            Assert.Equal("Last", lines[3]);
        }

        [Fact]
        public void TryReadLine_ConsecutiveNewlines_ReturnsEmptyLines()
        {
            // Arrange
            using var source = new StringDataSource("Line1\n\n\nLine4");
            var lines = new List<string>();

            // Act
            while (source.TryReadLine(out var line, out _))
            {
                lines.Add(line.ToString());
            }

            // Assert
            Assert.Equal(4, lines.Count);
            Assert.Equal("Line1", lines[0]);
            Assert.Equal("", lines[1]);
            Assert.Equal("", lines[2]);
            Assert.Equal("Line4", lines[3]);
        }
    }

#if NET6_0_OR_GREATER
    public class AsyncTests
    {
        [Fact]
        public async Task TryReadLineAsync_ReturnsCompletedTask()
        {
            // Arrange
            using var source = new StringDataSource("Test line");

            // Act
            var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Test line", result.line);
            Assert.Equal(1, result.lineNumber);
        }

        [Fact]
        public async Task CountLinesAsync_ReturnsCompletedTask()
        {
            // Arrange
            using var source = new StringDataSource("Line1\nLine2\nLine3");

            // Act
            var count = await source.CountLinesAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task DisposeAsync_DisposesCorrectly()
        {
            // Arrange
            var source = new StringDataSource("Test");

            // Act
            await source.DisposeAsync();

            // Assert - should not throw
            Assert.Throws<ObjectDisposedException>(() => source.TryReadLine(out _, out _));
        }
    }
#endif
}