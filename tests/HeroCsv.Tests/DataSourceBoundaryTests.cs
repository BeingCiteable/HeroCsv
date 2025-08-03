using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv.DataSources;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests boundary conditions for StringDataSource and MemoryDataSource implementations
/// </summary>
public class DataSourceBoundaryTests
{
    [Fact]
    public void StringDataSource_AllScenarios()
    {
        // Test counting lines with various endings
        using (var source = new StringDataSource("line1\r\nline2\nline3\rline4"))
        {
            var count = source.CountLinesDirectly();
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
    public void MemoryDataSource_AllScenarios()
    {
        // Test with different line endings
        var content = "line1\r\nline2\nline3".AsMemory();
        using (var source = new MemoryDataSource(content))
        {
            Assert.True(source.TryReadLine(out var line, out var lineNum));
            Assert.Equal("line1", line.ToString());
            Assert.Equal(1, lineNum);

            Assert.True(source.TryReadLine(out line, out lineNum));
            Assert.Equal("line2", line.ToString());
            Assert.Equal(2, lineNum);
        }

        // Test reset functionality
        using (var source = new MemoryDataSource("test".AsMemory()))
        {
            source.TryReadLine(out _, out _);
            Assert.False(source.HasMoreData);

            source.Reset();
            Assert.True(source.HasMoreData);
        }
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncStreamDataSource_AllPaths()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        // Test counting
        var count = await source.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, count);

        // Reset and read
        source.Reset();

        var result = await source.TryReadLineAsync(CancellationToken.None);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);

        // Test sync CountLinesDirectly
        source.Reset();
        count = source.CountLinesDirectly();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AsyncMemoryDataSource_EdgeCases()
    {
        // Test with MemoryDataSource
        var memSource = new MemoryDataSource("test\ndata".AsMemory());
        using var asyncSource = new AsyncMemoryDataSource(memSource);

        var count = await asyncSource.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, count);
    }
#endif
}