using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.DataSourceTests;

/// <summary>
/// Integration tests for data source functionality across different .NET versions and data source types
/// </summary>
public class DataSourceIntegrationTests
{
    #region Cross-DataSource Consistency Tests

    [Fact]
    public void AllDataSources_HandleEmptyContent()
    {
        // String
        using var stringReader = Csv.CreateReader("");
        Assert.False(stringReader.TryReadRecord(out _));

        // Memory
        using var memoryReader = Csv.CreateReader(ReadOnlyMemory<char>.Empty);
        Assert.False(memoryReader.TryReadRecord(out _));

        // Stream
        using var stream = new MemoryStream();
        using var streamReader = Csv.CreateReader(stream);
        Assert.False(streamReader.TryReadRecord(out _));
    }

    [Fact]
    public void AllDataSources_ProduceIdenticalResults()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Act
        var stringRecords = Csv.ReadAllRecords(csvContent, options);
        var memoryRecords = Csv.ReadAllRecords(csvContent.AsMemory(), options);

        IReadOnlyList<string[]> streamRecords;
        using (var stream = new MemoryStream(bytes))
        {
            using var reader = Csv.CreateReader(stream, options);
            streamRecords = reader.ReadAllRecords();
        }

        // Assert
        Assert.Equal(3, stringRecords.Count);
        Assert.Equal(3, memoryRecords.Count);
        Assert.Equal(3, streamRecords.Count);

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(stringRecords[i][0], memoryRecords[i][0]);
            Assert.Equal(stringRecords[i][0], streamRecords[i][0]);
            Assert.Equal(stringRecords[i][1], memoryRecords[i][1]);
            Assert.Equal(stringRecords[i][1], streamRecords[i][1]);
            Assert.Equal(stringRecords[i][2], memoryRecords[i][2]);
            Assert.Equal(stringRecords[i][2], streamRecords[i][2]);
        }
    }

    [Fact]
    public void DataSources_ResetFunctionality_Comparison()
    {
        var csvContent = "A,B\n1,2\n3,4";
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // String data source - supports reset
        using (var reader = Csv.CreateReader(csvContent, options))
        {
            Assert.True(reader.TryReadRecord(out var record1));
            Assert.Equal("A", record1.GetField(0).ToString());

            reader.Reset();
            Assert.True(reader.TryReadRecord(out var record2));
            Assert.Equal("A", record2.GetField(0).ToString()); // Should be back at the beginning
        }

        // Memory data source - supports reset
        using (var reader = Csv.CreateReader(csvContent.AsMemory(), options))
        {
            reader.TryReadRecord(out _); // Skip first
            reader.TryReadRecord(out var beforeReset);
            Assert.Equal("1", beforeReset.GetField(0).ToString()); // Second row

            reader.Reset();
            reader.TryReadRecord(out var afterReset);
            Assert.Equal("A", afterReset.GetField(0).ToString());
        }

        // Stream data source - supports reset for seekable streams
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        using (var stream = new MemoryStream(bytes))
        {
            using var reader = Csv.CreateReader(stream, options, leaveOpen: true);
            // Count records might fail for streams, use TryReadRecord instead
            var count1 = 0;
            while (reader.TryReadRecord(out _)) count1++;

            reader.Reset();
            var count2 = 0;
            while (reader.TryReadRecord(out _)) count2++;

            Assert.Equal(3, count1);
            Assert.Equal(3, count2);
        }
    }

    #endregion

    #region MemoryDataSource Integration Tests

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
    public void MemoryDataSource_CountLines()
    {
        var content = "line1\nline2\nline3".AsMemory();
        using var source = new MemoryDataSource(content);
        var count = source.CountLines();
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

    [Fact]
    public void MemoryDataSource_AllScenarios_FromDataSourceBoundaryTests()
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

    #endregion

#if NET7_0_OR_GREATER
    [Fact]
    public async Task AsyncStreamDataSource_ProduceIdenticalResults()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var options = new CsvOptions(',', '"', true); // hasHeader: true

        // Act
        var syncRecords = Csv.ReadAllRecords(csvContent, options);

        // Test async stream reading
        IReadOnlyList<string[]> asyncStreamRecords;
        await using (var stream = new MemoryStream(bytes))
        {
            asyncStreamRecords = await Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);
        }

        // Assert
        Assert.Equal(2, syncRecords.Count); // 2 data rows (header excluded)
        Assert.Equal(2, asyncStreamRecords.Count);

        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(syncRecords[i][0], asyncStreamRecords[i][0]);
            Assert.Equal(syncRecords[i][1], asyncStreamRecords[i][1]);
            Assert.Equal(syncRecords[i][2], asyncStreamRecords[i][2]);
        }
    }

    [Fact]
    public async Task AsyncStreamDataSource_SupportsSeekableStreams()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var bytes = Encoding.UTF8.GetBytes(csv);
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Act & Assert
        await using var stream = new MemoryStream(bytes);
        
        // Read all records first time
        var records1 = await Csv.ReadStreamAsync(stream, options, leaveOpen: true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, records1.Count);

        // Reset stream and read again
        stream.Position = 0;
        var records2 = await Csv.ReadStreamAsync(stream, options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, records2.Count);

        // Verify content is identical
        for (int i = 0; i < records1.Count; i++)
        {
            Assert.Equal(records1[i][0], records2[i][0]);
            Assert.Equal(records1[i][1], records2[i][1]);
        }
    }

    [Fact]
    public async Task AsyncStreamDataSource_HandlesNonSeekableStreams()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var bytes = Encoding.UTF8.GetBytes(csv);
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Create a non-seekable stream (simulated with a custom stream)
        var nonSeekableStream = new NonSeekableMemoryStream(bytes);

        // Act & Assert
        await using (nonSeekableStream)
        {
            // Should be able to read records
            var records = await Csv.ReadStreamAsync(nonSeekableStream, options, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(3, records.Count);
            Assert.Equal("A", records[0][0]);
            Assert.Equal("1", records[1][0]);
            Assert.Equal("3", records[2][0]);

            // Non-seekable streams cannot be reset - this is handled internally
        }
    }

    // Helper class for testing non-seekable streams
    private class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override bool CanSeek => false;
        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException("Stream does not support seeking");
        }
    }
#endif

    #region Stream Integration Tests

    [Fact]
    public void Csv_ReadStream_Basic()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var records = Csv.ReadStream(stream).ToList();
        Assert.Equal(2, records.Count); // Data records only, header excluded
    }

    [Fact]
    public void Csv_ReadStream_WithOptions()
    {
        var csv = "A|B\n1|2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new CsvOptions(delimiter: '|');

        var records = Csv.ReadStream(stream, options);
        Assert.Single(records);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task Csv_ReadStreamAsync_Basic()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var records = await Csv.ReadStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, records.Count);
    }
#endif

    #endregion

    #region Edge Case Integration Tests

    [Fact]
    public void DataSources_EdgeCases_Comparison()
    {
        // Test with empty string
        using var reader1 = new HeroCsvReader("", CsvOptions.Default);
        Assert.False(reader1.HasMoreData);
        Assert.False(reader1.TryReadRecord(out _));

        // Test with single line without newline
        using var reader2 = new HeroCsvReader("single line", new CsvOptions(hasHeader: false));
        Assert.True(reader2.TryReadRecord(out var record));
        Assert.Single(record.ToArray());
        Assert.Equal("single line", record.ToArray()[0]);
        Assert.False(reader2.TryReadRecord(out _));
    }

    [Fact]
    public void DataSources_NonSeekableStreamHandling()
    {
        using var stream = new NonSeekableMemoryStream(Encoding.UTF8.GetBytes("A,B\n1,2"));
        using var reader = new HeroCsvReader(stream, new CsvOptions(hasHeader: false));

        // Cannot reset non-seekable stream
        Assert.Throws<NotSupportedException>(() => reader.Reset());

        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal(2, record.FieldCount);
        Assert.Equal("A", record.ToArray()[0]); // First record should be "A,B"
    }

    [Fact]
    public void DataSources_LeaveOpenBehavior()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        using (var reader = new HeroCsvReader(stream, new CsvOptions(hasHeader: false), leaveOpen: true))
        {
            reader.TryReadRecord(out _);
        }

        // Stream should still be open
        Assert.True(stream.CanRead);
        stream.Dispose();
    }


    #endregion
}