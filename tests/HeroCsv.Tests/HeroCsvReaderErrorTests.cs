using System;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Errors;
using HeroCsv.Models;
using HeroCsv.Validation;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests error handling and special operations for HeroCsvReader
/// </summary>
public class HeroCsvReaderErrorTests
{
    [Fact]
    public void HeroCsvReader_ValidationResult()
    {
        var reader = new HeroCsvReader("A,B\n1,2,3\n4,5", CsvOptions.Default, validateData: true, trackErrors: true);
        
        // Read records to trigger validation
        while (reader.TryReadRecord(out _)) { }
        
        var result = reader.ValidationResult;
        Assert.NotNull(result);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void HeroCsvReader_SkipRecord()
    {
        using var reader = new HeroCsvReader("Header\nRow1\nRow2\nRow3", CsvOptions.Default);
        
        reader.SkipRecord(); // Skip header
        Assert.Equal(2, reader.LineNumber);
        
        reader.TryReadRecord(out var record);
        Assert.Equal("Row1", record.ToArray()[0]);
    }

    [Fact]
    public void HeroCsvReader_SkipRecords()
    {
        using var reader = new HeroCsvReader("Header\nRow1\nRow2\nRow3\nRow4", CsvOptions.Default);
        
        reader.SkipRecords(3); // Skip header and 2 rows
        Assert.Equal(4, reader.LineNumber);
        
        reader.TryReadRecord(out var record);
        Assert.Equal("Row3", record.ToArray()[0]);
    }

    [Fact]
    public void HeroCsvReader_ReadRecord_ThrowsWhenNoMoreData()
    {
        using var reader = new HeroCsvReader("A,B", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out _);
        
        Assert.Throws<InvalidOperationException>(() => reader.ReadRecord());
    }
}