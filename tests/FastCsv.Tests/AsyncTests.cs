#if NET7_0_OR_GREATER
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace FastCsv.Tests;

public class AsyncTests
{
    private const string TestCsvWithHeader = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,SF";
    private const string TestCsvNoHeader = "John,30,NYC\nJane,25,LA\nBob,35,SF";

    [Fact]
    public async Task ReadFileAsync_WithHeader_ReturnsCorrectRecords()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, TestCsvWithHeader, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var records = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);

            // Assert
            Assert.Equal(3, records.Count); // 3 data rows (header excluded)
            Assert.Equal("John", records[0][0]);
            Assert.Equal("30", records[0][1]);
            Assert.Equal("NYC", records[0][2]);
            Assert.Equal("Jane", records[1][0]);
            Assert.Equal("Bob", records[2][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadFileAsync_WithoutHeader_ReturnsAllRecords()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, TestCsvNoHeader, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: false);

        try
        {
            // Act
            var records = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);

            // Assert
            Assert.Equal(3, records.Count); // All 3 rows included
            Assert.Equal("John", records[0][0]);
            Assert.Equal("Jane", records[1][0]);
            Assert.Equal("Bob", records[2][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadStreamAsync_WithAsyncStream_ReturnsCorrectRecords()
    {
        // Arrange
        var csvBytes = Encoding.UTF8.GetBytes(TestCsvWithHeader);
        await using var stream = new MemoryStream(csvBytes);
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = await Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);

        // Assert
        Assert.Equal(3, records.Count); // 3 data rows (header excluded)
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("Bob", records[2][0]);
    }

    [Fact]
    public async Task ReadFileAsyncEnumerable_WithHeader_StreamsCorrectRecords()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, TestCsvWithHeader, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var records = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, options, null, CancellationToken.None))
            {
                records.Add(record);
            }

            // Assert
            Assert.Equal(3, records.Count); // 3 data rows (header excluded)
            Assert.Equal("John", records[0][0]);
            Assert.Equal("Jane", records[1][0]);
            Assert.Equal("Bob", records[2][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CreateAsyncReader_WithStream_ProducesCorrectResults()
    {
        // Arrange
        var csvBytes = Encoding.UTF8.GetBytes(TestCsvWithHeader);
        await using var stream = new MemoryStream(csvBytes);
        var options = new CsvOptions(hasHeader: true);

        // Act
        using var reader = Csv.CreateAsyncReader(stream, options);
        var records = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("Bob", records[2][0]);
    }

    [Fact]
    public async Task CreateAsyncReaderFromFile_ProducesCorrectResults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, TestCsvWithHeader, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            using var reader = Csv.CreateAsyncReaderFromFile(tempFile, options);
            var records = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(3, records.Count);
            Assert.Equal("John", records[0][0]);
            Assert.Equal("Jane", records[1][0]);
            Assert.Equal("Bob", records[2][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadRecordsAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var largeCsv = "Name,Age\n" + string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Person{i},{i}"));
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, largeCsv, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act & Assert
            using var reader = Csv.CreateAsyncReaderFromFile(tempFile, options);
            using var cts = new CancellationTokenSource();

            var recordCount = 0;
            await foreach (var record in reader.ReadRecordsAsync(cts.Token))
            {
                recordCount++;
                if (recordCount == 5)
                {
                    cts.Cancel(); // Cancel after 5 records
                }
            }

            Assert.Equal(5, recordCount);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncMethods_WithEmptyFile_ReturnEmptyResults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "", TestContext.Current.CancellationToken);

        try
        {
            // Act & Assert
            var records1 = await Csv.ReadFileAsync(tempFile, CsvOptions.Default, null, CancellationToken.None);
            Assert.Empty(records1);

            var records2 = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, CsvOptions.Default, null, CancellationToken.None))
            {
                records2.Add(record);
            }
            Assert.Empty(records2);

            await using var stream = File.OpenRead(tempFile);
            var records3 = await Csv.ReadStreamAsync(stream, CsvOptions.Default, null, false, CancellationToken.None);
            Assert.Empty(records3);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncMethods_WithHeaderOnly_ReturnEmptyResults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Name,Age,City", TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act & Assert
            var records1 = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);
            Assert.Empty(records1);

            var records2 = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, options, null, CancellationToken.None))
            {
                records2.Add(record);
            }
            Assert.Empty(records2);

            await using var stream = File.OpenRead(tempFile);
            var records3 = await Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);
            Assert.Empty(records3);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncVsSyncMethods_ProduceIdenticalResults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, TestCsvWithHeader, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var asyncRecords = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);

            var syncRecords = Csv.ReadAllRecords(TestCsvWithHeader, options);

            // Assert
            Assert.Equal(syncRecords.Count, asyncRecords.Count);
            for (int i = 0; i < syncRecords.Count; i++)
            {
                Assert.Equal(syncRecords[i].Length, asyncRecords[i].Length);
                for (int j = 0; j < syncRecords[i].Length; j++)
                {
                    Assert.Equal(syncRecords[i][j], asyncRecords[i][j]);
                }
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncDataSource_HandlesDifferentEncodings()
    {
        // Arrange
        var csvContent = "Name,Age\nJohñ,30\nJané,25"; // With accented characters
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvContent, Encoding.UTF8, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var records = await Csv.ReadFileAsync(tempFile, options, Encoding.UTF8, CancellationToken.None);

            // Assert
            Assert.Equal(2, records.Count);
            Assert.Equal("Johñ", records[0][0]);
            Assert.Equal("Jané", records[1][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncStreamReader_HandlesLargeFiles()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var largeCsv = "Name,Age\n" + string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Person{i},{20 + i % 50}"));
        await File.WriteAllTextAsync(tempFile, largeCsv, TestContext.Current.CancellationToken);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var records = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);

            // Assert
            Assert.Equal(1000, records.Count);
            Assert.Equal("Person1", records[0][0]);
            Assert.Equal("21", records[0][1]);
            Assert.Equal("Person1000", records[999][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadRecordsAsync_WithAsyncStreamEnumeration_WorksCorrectly()
    {
        // Arrange
        var csvBytes = Encoding.UTF8.GetBytes(TestCsvWithHeader);
        await using var stream = new MemoryStream(csvBytes);
        var options = new CsvOptions(hasHeader: true);

        // Act
        using var reader = Csv.CreateAsyncReader(stream, options);
        var records = new List<string[]>();

        await foreach (var record in reader.ReadRecordsAsync(TestContext.Current.CancellationToken))
        {
            records.Add(record);
        }

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("Bob", records[2][0]);
    }

    [Fact]
    public async Task AsyncReader_WithStreamLeaveOpen_DoesNotDisposeStream()
    {
        // Arrange
        var csvBytes = Encoding.UTF8.GetBytes(TestCsvWithHeader);
        var stream = new MemoryStream(csvBytes);
        var options = new CsvOptions(hasHeader: true);

        // Act
        using (var reader = Csv.CreateAsyncReader(stream, options, leaveOpen: true))
        {
            await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
        } // Reader disposed here

        // Assert
        Assert.True(stream.CanRead); // Stream should still be usable
        stream.Dispose();
    }

    [Fact]
    public async Task AsyncReader_WithStreamDispose_DisposesStream()
    {
        // Arrange
        var csvBytes = Encoding.UTF8.GetBytes(TestCsvWithHeader);
        var stream = new MemoryStream(csvBytes);
        var options = new CsvOptions(hasHeader: true);

        // Act
        using (var reader = Csv.CreateAsyncReader(stream, options, leaveOpen: false))
        {
            await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
        } // Reader and stream disposed here

        // Assert
        Assert.False(stream.CanRead); // Stream should be disposed
    }
}
#endif