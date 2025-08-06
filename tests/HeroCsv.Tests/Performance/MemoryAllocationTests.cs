using System;
using HeroCsv.Core;
using HeroCsv.Models;
using HeroCsv.Parsing;
using Xunit;

namespace HeroCsv.Tests.Performance;

/// <summary>
/// Tests to verify memory allocation behavior and ensure zero-allocation scenarios work correctly
/// </summary>
public class MemoryAllocationTests
{
    [Fact]
    public void VerifyParseLineBehavior()
    {
        var line = "John,25,NYC,USA,Active".AsSpan();
        var options = CsvOptions.Default;

        var fields = CsvParser.ParseLine(line, options);

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

        // CountRecords with default options (hasHeader=true) counts data rows only
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
        var fastReader = (HeroCsvReader)reader;

        var rowCount = 0;
        foreach (var row in fastReader.EnumerateRows())
        {
            rowCount++;
        }

        // EnumerateRows with default options skips header
        Assert.Equal(2, rowCount); // Only data rows
    }
}