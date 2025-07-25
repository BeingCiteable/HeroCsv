using System;
using Xunit;
using Xunit.Abstractions;

namespace FastCsv.Tests;

/// <summary>
/// Simplified allocation verification tests
/// </summary>
public class AllocationVerificationTests
{
    private readonly ITestOutputHelper _output;

    public AllocationVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void VerifyParseLineBehavior()
    {
        var line = "John,25,NYC,USA,Active".AsSpan();
        var options = new CsvOptions();
        
        var fields = CsvParser.ParseLine(line, options);
        _output.WriteLine($"Fields parsed: {fields.Length}");
        for (int i = 0; i < fields.Length; i++)
        {
            _output.WriteLine($"Field {i}: '{fields[i]}'");
        }
        
        Assert.Equal(5, fields.Length);
        Assert.Equal("John", fields[0]);
        Assert.Equal("25", fields[1]);
        Assert.Equal("NYC", fields[2]);
        Assert.Equal("USA", fields[3]);
        Assert.Equal("Active", fields[4]);
    }

    [Fact]
    public void VerifyCountRecordsBehavior()
    {
        var csvData = @"Name,Age,City
John,25,NYC
Jane,30,LA";
        
        var count = Csv.CountRecords(csvData);
        _output.WriteLine($"Count returned: {count}");
        
        // CountRecords should count data rows only (excluding header)
        Assert.Equal(2, count);
    }

    [Fact]
    public void VerifyReadAllRecordsBehavior()
    {
        var csvData = @"Name,Age,City
John,25,NYC
Jane,30,LA";
        
        using var reader = Csv.CreateReader(csvData);
        var records = reader.ReadAllRecords();
        
        _output.WriteLine($"Records returned: {records.Count}");
        
        // ReadAllRecords should return data rows only (header skipped by default)
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }

    [Fact]
    public void VerifyEnumerateRowsBehavior()
    {
        var csvData = @"Name,Age,City
John,25,NYC
Jane,30,LA";
        
        using var reader = Csv.CreateReader(csvData);
        var fastReader = (FastCsvReader)reader;
        
        var rowCount = 0;
        foreach (var row in fastReader.EnumerateRows())
        {
            rowCount++;
            _output.WriteLine($"Row {rowCount}: FieldCount={row.FieldCount}");
        }
        
        // EnumerateRows includes header
        Assert.Equal(3, rowCount);
    }
}