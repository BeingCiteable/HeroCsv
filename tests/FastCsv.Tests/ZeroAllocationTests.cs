using System;
using System.Text;
using FastCsv.Core;
using FastCsv.Models;
using Xunit;

namespace FastCsv.Tests;

public class ZeroAllocationTests
{
    // Helper to create options with header = false
    private static CsvOptions NoHeaderOptions => new(',', '"', false);

    [Fact]
    public void ReadFromMemory_ReturnsCorrectRecords()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,Chicago".AsMemory();
        var options = NoHeaderOptions;

        // Act
        var records = Csv.ReadAllRecords(csvContent, options);

        // Assert
        Assert.Equal(4, records.Count); // Including header row
        Assert.Equal("Name", records[0][0]); // Header treated as data when hasHeader=false
        Assert.Equal("Age", records[0][1]);
        Assert.Equal("City", records[0][2]);
        Assert.Equal("John", records[1][0]);
        Assert.Equal("30", records[1][1]);
        Assert.Equal("NYC", records[1][2]);
    }

    [Fact]
    public void CreateReaderFromMemory_SupportsReset()
    {
        // Arrange
        var csvContent = "A,B,C\n1,2,3\n4,5,6".AsMemory();
        var options = NoHeaderOptions;

        // Act
        using var reader = Csv.CreateReader(csvContent, options);

        // First pass
        var count1 = reader.CountRecords();

        // Reset and count again
        reader.Reset();
        var count2 = reader.CountRecords();

        // Assert
        Assert.Equal(3, count1); // All 3 lines counted when hasHeader=false
        Assert.Equal(3, count2);
    }

    [Fact]
    public void CountRecordsFromMemory_ReturnsCorrectCount()
    {
        // Arrange
        var csvContent = "Header1,Header2\nValue1,Value2\nValue3,Value4".AsMemory();
        var options = NoHeaderOptions;

        // Act
        var count = Csv.CountRecords(csvContent, options);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void ParseLargeMemorySpan_HandlesCorrectly()
    {
        // Arrange
        var rows = 1000;
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("ID,Name,Value");
        for (int i = 0; i < rows; i++)
        {
            csvBuilder.AppendLine($"{i},Name{i},{i * 100}");
        }
        var csvContent = csvBuilder.ToString().AsMemory();
        var options = NoHeaderOptions;

        // Act
        var records = Csv.ReadAllRecords(csvContent, options);

        // Assert
        Assert.Equal(rows + 1, records.Count); // +1 for header
        Assert.Equal("999", records[rows][0]); // Last data row is at index 1000 (header at 0)
        Assert.Equal("Name999", records[rows][1]);
        Assert.Equal("99900", records[rows][2]);
    }

    [Fact]
    public void ReadOnlySpan_ConvertsToString()
    {
        // Arrange
        ReadOnlySpan<char> csvSpan = "A,B\n1,2\n3,4".AsSpan();
        var options = NoHeaderOptions;

        // Act
        var records = Csv.ReadAllRecords(csvSpan, options);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("A", records[0][0]); // Header row when hasHeader=false
        Assert.Equal("2", records[1][1]); // Second row, second column
    }

    [Fact]
    public void MemoryReader_HandlesEmptyLines()
    {
        // Arrange
        var csvContent = "A,B\n\n1,2\n\n3,4".AsMemory();
        var options = NoHeaderOptions;

        // Act
        using var reader = Csv.CreateReader(csvContent, options);
        var records = reader.ReadAllRecords();

        // Assert
        Assert.Equal(3, records.Count); // Empty lines are skipped
        Assert.Equal("A", records[0][0]); // Header row
        Assert.Equal("1", records[1][0]); // Second record (empty lines are skipped)
    }

    [Fact]
    public void MemoryReader_HandlesQuotedFields()
    {
        // Arrange
        var csvContent = "Name,Description\n\"John\",\"Says \"\"Hello\"\"\"\n\"Jane\",\"Normal text\"".AsMemory();
        var options = NoHeaderOptions;

        // Act
        var records = Csv.ReadAllRecords(csvContent, options);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("Name", records[0][0]); // Header row
        Assert.Equal("Description", records[0][1]);
        Assert.Equal("John", records[1][0]);
        Assert.Equal("Says \"Hello\"", records[1][1]);
        Assert.Equal("Jane", records[2][0]);
    }

    [Theory]
    [InlineData("A,B,C", 1, 3)] // 1 record, 3 fields
    [InlineData("A,B\n1,2\n3,4", 3, 2)] // 3 records, 2 fields each
    [InlineData("Single", 1, 1)] // 1 record, 1 field
    public void CountRecords_VariousInputs(string csv, int expectedRecords, int expectedFields)
    {
        // Arrange
        var memory = csv.AsMemory();
        var options = NoHeaderOptions;

        // Act
        var count = Csv.CountRecords(memory, options);
        var records = Csv.ReadAllRecords(memory, options);

        // Assert
        Assert.Equal(expectedRecords, count);
        Assert.Equal(expectedRecords, records.Count);
        if (records.Count > 0)
        {
            Assert.Equal(expectedFields, records[0].Length);
        }
    }
}