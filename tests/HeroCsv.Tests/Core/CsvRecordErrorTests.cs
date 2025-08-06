using System;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Core;

/// <summary>
/// Tests error handling and invalid operations on CsvRecord
/// </summary>
public class CsvRecordErrorTests
{
    [Fact]
    public void CsvRecord_GetField_ThrowsOnInvalidIndex()
    {
        using var reader = new HeroCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => record.GetField(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => record.GetField(3));
    }

    [Fact]
    public void CsvRecord_TryGetField_Success()
    {
        using var reader = new HeroCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.True(record.TryGetField(0, out var field));
        Assert.Equal("A", field);
        
        Assert.True(record.TryGetField(1, out field));
        Assert.Equal("B", field);
        
        Assert.True(record.TryGetField(2, out field));
        Assert.Equal("C", field);
    }

    [Fact]
    public void CsvRecord_TryGetField_Failure()
    {
        using var reader = new HeroCsvReader("A,B", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.False(record.TryGetField(-1, out var field));
        Assert.True(field.IsEmpty);
        
        Assert.False(record.TryGetField(2, out field));
        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void CsvRecord_IsValidIndex()
    {
        using var reader = new HeroCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.False(record.IsValidIndex(-1));
        Assert.True(record.IsValidIndex(0));
        Assert.True(record.IsValidIndex(1));
        Assert.True(record.IsValidIndex(2));
        Assert.False(record.IsValidIndex(3));
    }

    [Fact]
    public void CsvRecord_GetAllFields()
    {
        using var reader = new HeroCsvReader("A,B,C,D,E", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        // Test with exact size
        var fields = new string[5];
        var count = record.GetAllFields(fields);
        Assert.Equal(5, count);
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, fields);
        
        // Test with smaller destination
        fields = new string[3];
        count = record.GetAllFields(fields);
        Assert.Equal(3, count);
        Assert.Equal(new[] { "A", "B", "C" }, fields);
        
        // Test with larger destination
        fields = new string[10];
        count = record.GetAllFields(fields);
        Assert.Equal(5, count);
        Assert.Equal("A", fields[0]);
        Assert.Equal("E", fields[4]);
        Assert.Null(fields[5]);
    }
}