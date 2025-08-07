using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Integration.IO;

/// <summary>
/// Tests specifically for Csv.ReadFile() and Csv.ReadFileAsync() methods
/// </summary>
public class CsvReadFileTests : IDisposable
{
    private readonly string _tempFile;

    public CsvReadFileTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void ReadFile_SimpleData_ReadsCorrectly()
    {
        File.WriteAllText(_tempFile, "Name,Age\nJohn,25\nJane,30");

        var records = Csv.ReadFile(_tempFile).ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadFile_WithOptions_RespectsConfiguration()
    {
        File.WriteAllText(_tempFile, "Name;Age;City\nJohn;25;NYC");
        var options = new CsvOptions(delimiter: ';');

        var records = Csv.ReadFile(_tempFile, options).ToList();

        Assert.Single(records);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("NYC", records[0][2]);
    }

    [Fact]
    public void ReadFile_EmptyFile_ReturnsNoRecords()
    {
        File.WriteAllText(_tempFile, "");

        var records = Csv.ReadFile(_tempFile).ToList();

        Assert.Empty(records);
    }

    [Fact]
    public void ReadFile_HeaderOnly_ReturnsNoRecords()
    {
        File.WriteAllText(_tempFile, "Name,Age,City");

        var records = Csv.ReadFile(_tempFile).ToList();

        Assert.Empty(records);
    }

    [Fact]
    public void ReadFile_LargeFile_HandlesEfficiently()
    {
        // Create a file with 1000 rows
        var sb = new StringBuilder("Id,Name,Value\n");
        for (int i = 1; i <= 1000; i++)
        {
            sb.AppendLine($"{i},Name{i},{i * 100}");
        }
        File.WriteAllText(_tempFile, sb.ToString());

        var records = Csv.ReadFile(_tempFile).ToList();

        Assert.Equal(1000, records.Count);
        Assert.Equal("500", records[499][0]);
        Assert.Equal("Name500", records[499][1]);
    }

    [Fact]
    public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");

        Assert.Throws<FileNotFoundException>(() =>
        {
            Csv.ReadFile(nonExistentFile).ToList();
        });
    }

    [Fact]
    public void ReadFile_WithEncoding_ReadsCorrectly()
    {
        var content = "Name,City\nJosé,São Paulo\nFrançois,Paris";
        File.WriteAllText(_tempFile, content, Encoding.UTF8);

        var records = Csv.ReadFile(_tempFile).ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("José", records[0][0]);
        Assert.Equal("São Paulo", records[0][1]);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task ReadFileAsync_SimpleData_ReadsCorrectly()
    {
        await File.WriteAllTextAsync(_tempFile, "Name,Age\nJohn,25\nJane,30", TestContext.Current.CancellationToken);

        var records = new List<string[]>();
        await foreach (var record in Csv.ReadFileAsyncEnumerable(_tempFile, CsvOptions.Default, cancellationToken: TestContext.Current.CancellationToken))
        {
            records.Add(record);
        }

        // ReadFileAsync should skip headers by default (hasHeader: true)
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("30", records[1][1]);
    }

    [Fact]
    public async Task ReadFileAsync_WithCancellation_CanBeCancelled()
    {
        // Create a moderately sized file
        var sb = new StringBuilder("Id,Name,Description\n");
        for (int i = 1; i <= 1000; i++)
        {
            sb.AppendLine($"{i},Name{i},This is a longer description to make processing take more time {i}");
        }
        await File.WriteAllTextAsync(_tempFile, sb.ToString(), TestContext.Current.CancellationToken);

        // Pre-cancelled token should throw immediately
        using var preCancelledCts = new CancellationTokenSource();
        preCancelledCts.Cancel();
        
        var recordCount = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var record in Csv.ReadFileAsyncEnumerable(_tempFile, CsvOptions.Default, cancellationToken: preCancelledCts.Token))
            {
                recordCount++;
            }
        });
        
        // Should not have read any records with pre-cancelled token
        Assert.Equal(0, recordCount);
        
        // Also test cancellation during enumeration
        using var cts = new CancellationTokenSource();
        recordCount = 0;
        
        try
        {
            await foreach (var record in Csv.ReadFileAsyncEnumerable(_tempFile, CsvOptions.Default, cancellationToken: cts.Token))
            {
                recordCount++;
                if (recordCount == 5)
                {
                    cts.Cancel(); // Cancel after reading a few records
                }
            }
            Assert.Fail("Should have thrown OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
            Assert.True(recordCount >= 5, $"Should have read at least 5 records before cancellation, but read {recordCount}");
            Assert.True(recordCount < 1000, $"Should not have read all records, but read {recordCount}");
        }
    }
#endif
}
