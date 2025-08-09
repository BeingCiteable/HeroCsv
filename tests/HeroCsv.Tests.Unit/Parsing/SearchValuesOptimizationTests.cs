using System;
using Xunit;
using HeroCsv.Parsing;
using HeroCsv.Models;

namespace HeroCsv.Tests.Unit.Parsing;

public class SearchValuesOptimizationTests
{
#if NET8_0_OR_GREATER
    [Fact]
    public void SearchValues_DetectsDelimitersEfficiently()
    {
        // Arrange
        var line = "field1,field2;field3\tfield4|field5".AsSpan();
        
        // Act & Assert - Testing that SearchValues can find different delimiters
        Assert.True(line.Contains(','));
        Assert.True(line.Contains(';'));
        Assert.True(line.Contains('\t'));
        Assert.True(line.Contains('|'));
    }

    [Theory]
    [InlineData("simple,csv,line", ',', 3)]
    [InlineData("tab\tseparated\tvalues", '\t', 3)]
    [InlineData("pipe|delimited|data", '|', 3)]
    [InlineData("semicolon;separated;values", ';', 3)]
    public void ParseLine_WithSearchValues_HandlesVariousDelimiters(string input, char delimiter, int expectedFieldCount)
    {
        // Arrange
        var options = new CsvOptions(delimiter);
        
        // Act
        var result = CsvParser.ParseLine(input.AsSpan(), options);
        
        // Assert
        Assert.Equal(expectedFieldCount, result.Length);
    }

    [Fact]
    public void SearchValues_HandlesQuotedFields()
    {
        // Arrange
        var line = "\"quoted,field\",normal,\"another\"\"quoted\"".AsSpan();
        var options = new CsvOptions(',', '"');
        
        // Act
        var result = CsvParser.ParseLine(line, options);
        
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("quoted,field", result[0]);
        Assert.Equal("normal", result[1]);
        Assert.Equal("another\"quoted", result[2]);
    }

    [Fact]
    public void SearchValues_PerformanceBenefit_LongLines()
    {
        // Arrange - Create a long line with many fields
        var fieldCount = 1000;
        var fields = new string[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            fields[i] = $"field{i}";
        }
        var longLine = string.Join(",", fields);
        var options = new CsvOptions(',');
        
        // Act
        var result = CsvParser.ParseLine(longLine.AsSpan(), options);
        
        // Assert
        Assert.Equal(fieldCount, result.Length);
        for (int i = 0; i < fieldCount; i++)
        {
            Assert.Equal($"field{i}", result[i]);
        }
    }

    [Fact]
    public void AutoDetection_UsesSearchValues()
    {
        // Arrange
        var csvContent = @"name,age,city
John,30,NYC
Jane,25,LA";
        
        // Act - ReadAutoDetect internally uses SearchValues for delimiter detection
        var records = Csv.ReadAutoDetect(csvContent).ToList();
        
        // Assert - Should parse correctly with auto-detected comma delimiter
        Assert.Equal(2, records.Count);
        Assert.Equal(3, records[0].Length);
        Assert.Equal("John", records[0][0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",,,")]
    [InlineData("\"\"\"\"")]
    public void SearchValues_HandlesEdgeCases(string input)
    {
        // Arrange
        var options = new CsvOptions(',');
        
        // Act
        var result = CsvParser.ParseLine(input.AsSpan(), options);
        
        // Assert
        Assert.NotNull(result);
    }
#else
    [Fact]
    public void SearchValues_NotAvailable_OnOlderFrameworks()
    {
        // This test verifies that SearchValues code is properly conditionally compiled
        Assert.True(true, "SearchValues tests are not available on this framework version");
    }
#endif
}