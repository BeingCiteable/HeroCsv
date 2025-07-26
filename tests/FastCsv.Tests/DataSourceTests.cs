using System.IO;
using System.Text;
using FastCsv.Core;
using FastCsv.Models;
using Xunit;

namespace FastCsv.Tests;

public class DataSourceTests
{
    [Fact]
    public void StringDataSource_SupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Act & Assert
        using var reader = Csv.CreateReader(csv, options);
        Assert.True(reader.TryReadRecord(out var record1));
        Assert.Equal("A", record1.GetField(0).ToString());

        reader.Reset();
        Assert.True(reader.TryReadRecord(out var record2));
        Assert.Equal("A", record2.GetField(0).ToString()); // Should be back at the beginning
    }

    [Fact]
    public void MemoryDataSource_SupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4".AsMemory();
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Act & Assert
        using var reader = Csv.CreateReader(csv, options);
        reader.TryReadRecord(out _); // Skip first
        reader.TryReadRecord(out var beforeReset);
        Assert.Equal("1", beforeReset.GetField(0).ToString()); // Second row

        reader.Reset();
        reader.TryReadRecord(out var afterReset);
        Assert.Equal("A", afterReset.GetField(0).ToString());
    }

    [Fact]
    public void StreamDataSource_SeekableStreamSupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var bytes = Encoding.UTF8.GetBytes(csv);
        using var stream = new MemoryStream(bytes);
        var options = new CsvOptions(',', '"', false); // hasHeader: false

        // Act & Assert
        using var reader = Csv.CreateReader(stream, options, leaveOpen: true);
        // CountRecords might fail for streams, use TryReadRecord instead
        var count1 = 0;
        while (reader.TryReadRecord(out _)) count1++;

        reader.Reset();
        var count2 = 0;
        while (reader.TryReadRecord(out _)) count2++;

        Assert.Equal(3, count1);
        Assert.Equal(3, count2);
    }

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

#if NET7_0_OR_GREATER
    [Fact]
    public async Task AsyncDataSources_ProduceIdenticalResults()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var options = new CsvOptions(',', '"', true); // hasHeader: true

        // Act
        var syncRecords = Csv.ReadAllRecords(csvContent, options);

        // Test async file reading
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvContent, TestContext.Current.CancellationToken);
        IReadOnlyList<string[]> asyncFileRecords;
        try
        {
            asyncFileRecords = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        // Test async stream reading
        IReadOnlyList<string[]> asyncStreamRecords;
        await using (var stream = new MemoryStream(bytes))
        {
            asyncStreamRecords = await Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);
        }

        // Assert
        Assert.Equal(2, syncRecords.Count); // 2 data rows (header excluded)
        Assert.Equal(2, asyncFileRecords.Count);
        Assert.Equal(2, asyncStreamRecords.Count);

        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(syncRecords[i][0], asyncFileRecords[i][0]);
            Assert.Equal(syncRecords[i][0], asyncStreamRecords[i][0]);
            Assert.Equal(syncRecords[i][1], asyncFileRecords[i][1]);
            Assert.Equal(syncRecords[i][1], asyncStreamRecords[i][1]);
            Assert.Equal(syncRecords[i][2], asyncFileRecords[i][2]);
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
        using var reader = Csv.CreateAsyncReader(stream, options);

        // Read all records first time
        var records1 = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, records1.Count);

        // Reset and read again
        reader.Reset();
        var records2 = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
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
            using var reader = Csv.CreateAsyncReader(nonSeekableStream, options);

            // Should be able to read records
            var records = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
            Assert.Equal(3, records.Count);
            Assert.Equal("A", records[0][0]);
            Assert.Equal("1", records[1][0]);
            Assert.Equal("3", records[2][0]);

            // Reset should throw for non-seekable streams
            Assert.Throws<NotSupportedException>(() => reader.Reset());
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
}