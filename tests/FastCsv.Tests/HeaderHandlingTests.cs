using System.IO;
using System.Text;
using Xunit;

namespace FastCsv.Tests;

public class HeaderHandlingTests
{
    [Fact]
    public void SyncMethods_WithHeader_SkipHeaderCorrectly()
    {
        // Arrange
        var csvWithHeader = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(csvWithHeader, options);

        // Assert
        Assert.Equal(2, records.Count); // Should exclude header
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }

    [Fact]
    public void SyncMethods_WithoutHeader_IncludeAllRows()
    {
        // Arrange
        var csvWithoutHeader = "John,30,NYC\nJane,25,LA";
        var options = new CsvOptions(hasHeader: false);

        // Act
        var records = Csv.ReadAllRecords(csvWithoutHeader, options);

        // Assert
        Assert.Equal(2, records.Count); // Should include all rows
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task AsyncMethods_WithHeader_SkipHeaderCorrectly()
    {
        // Arrange
        var csvWithHeader = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvWithHeader);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Test ReadFileAsync
            var records1 = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);
            Assert.Equal(2, records1.Count);
            Assert.Equal("John", records1[0][0]);
            Assert.Equal("Jane", records1[1][0]);

            // Test ReadFileAsyncEnumerable
            var records2 = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, options, null, CancellationToken.None))
            {
                records2.Add(record);
            }
            Assert.Equal(2, records2.Count);
            Assert.Equal("John", records2[0][0]);
            Assert.Equal("Jane", records2[1][0]);

            // Test ReadStreamAsync
            await using var stream = File.OpenRead(tempFile);
            var records3 = await Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);
            Assert.Equal(2, records3.Count);
            Assert.Equal("John", records3[0][0]);
            Assert.Equal("Jane", records3[1][0]);

            // Test async reader directly
            await using var stream2 = File.OpenRead(tempFile);
            using var reader = Csv.CreateAsyncReader(stream2, options);
            var records4 = await reader.ReadAllRecordsAsync();
            Assert.Equal(2, records4.Count);
            Assert.Equal("John", records4[0][0]);
            Assert.Equal("Jane", records4[1][0]);

            // Test async enumerable reader
            await using var stream3 = File.OpenRead(tempFile);
            using var reader2 = Csv.CreateAsyncReader(stream3, options);
            var records5 = new List<string[]>();
            await foreach (var record in reader2.ReadRecordsAsync())
            {
                records5.Add(record);
            }
            Assert.Equal(2, records5.Count);
            Assert.Equal("John", records5[0][0]);
            Assert.Equal("Jane", records5[1][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncMethods_WithoutHeader_IncludeAllRows()
    {
        // Arrange
        var csvWithoutHeader = "John,30,NYC\nJane,25,LA";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvWithoutHeader);
        var options = new CsvOptions(hasHeader: false);

        try
        {
            // Test ReadFileAsync
            var records1 = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);
            Assert.Equal(2, records1.Count);
            Assert.Equal("John", records1[0][0]);
            Assert.Equal("Jane", records1[1][0]);

            // Test ReadFileAsyncEnumerable
            var records2 = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, options, null, CancellationToken.None))
            {
                records2.Add(record);
            }
            Assert.Equal(2, records2.Count);
            Assert.Equal("John", records2[0][0]);
            Assert.Equal("Jane", records2[1][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncMethods_HeaderOnlyFile_ReturnEmpty()
    {
        // Arrange
        var headerOnly = "Name,Age,City";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, headerOnly);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Test ReadFileAsync
            var records1 = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);
            Assert.Empty(records1);

            // Test ReadFileAsyncEnumerable
            var records2 = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, options, null, CancellationToken.None))
            {
                records2.Add(record);
            }
            Assert.Empty(records2);

            // Test ReadStreamAsync
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
    public async Task AsyncVsSyncHeaderHandling_ProduceIdenticalResults()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,SF";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvContent);

        try
        {
            // Test with header
            var optionsWithHeader = new CsvOptions(hasHeader: true);
            var syncWithHeader = Csv.ReadAllRecords(csvContent, optionsWithHeader);
            var asyncWithHeader = await Csv.ReadFileAsync(tempFile, optionsWithHeader, null, CancellationToken.None);

            Assert.Equal(syncWithHeader.Count, asyncWithHeader.Count);
            Assert.Equal(3, syncWithHeader.Count); // 3 data rows
            
            for (int i = 0; i < syncWithHeader.Count; i++)
            {
                Assert.Equal(syncWithHeader[i][0], asyncWithHeader[i][0]);
                Assert.Equal(syncWithHeader[i][1], asyncWithHeader[i][1]);
                Assert.Equal(syncWithHeader[i][2], asyncWithHeader[i][2]);
            }

            // Test without header
            var optionsWithoutHeader = new CsvOptions(hasHeader: false);
            var syncWithoutHeader = Csv.ReadAllRecords(csvContent, optionsWithoutHeader);
            var asyncWithoutHeader = await Csv.ReadFileAsync(tempFile, optionsWithoutHeader, null, CancellationToken.None);

            Assert.Equal(syncWithoutHeader.Count, asyncWithoutHeader.Count);
            Assert.Equal(4, syncWithoutHeader.Count); // 4 rows including header

            for (int i = 0; i < syncWithoutHeader.Count; i++)
            {
                Assert.Equal(syncWithoutHeader[i][0], asyncWithoutHeader[i][0]);
                Assert.Equal(syncWithoutHeader[i][1], asyncWithoutHeader[i][1]);
                Assert.Equal(syncWithoutHeader[i][2], asyncWithoutHeader[i][2]);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AsyncMethods_WithEmptyLinesAndHeader_HandleCorrectly()
    {
        // Arrange
        var csvWithEmptyLines = "Name,Age,City\n\nJohn,30,NYC\n\nJane,25,LA\n\n";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvWithEmptyLines);
        var options = new CsvOptions(hasHeader: true);

        try
        {
            // Act
            var records = await Csv.ReadFileAsync(tempFile, options, null, CancellationToken.None);

            // Assert
            Assert.Equal(2, records.Count); // Should skip header and empty lines
            Assert.Equal("John", records[0][0]);
            Assert.Equal("Jane", records[1][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
#endif

    [Fact]
    public void SyncMethods_HeaderOnlyContent_ReturnEmpty()
    {
        // Arrange
        var headerOnly = "Name,Age,City";
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(headerOnly, options);

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void SyncMethods_WithEmptyLinesAndHeader_HandleCorrectly()
    {
        // Arrange
        var csvWithEmptyLines = "Name,Age,City\n\nJohn,30,NYC\n\nJane,25,LA\n\n";
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(csvWithEmptyLines, options);

        // Assert
        Assert.Equal(2, records.Count); // Should skip header and empty lines
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }
}