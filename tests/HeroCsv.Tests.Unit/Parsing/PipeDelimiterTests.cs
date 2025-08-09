using System.Linq;
using Xunit;
using HeroCsv;
using HeroCsv.Models;

namespace HeroCsv.Tests.Unit.Parsing;

public class PipeDelimiterTests
{
    [Fact]
    public void ParseLine_WithPipeDelimiter_ParsesCorrectly()
    {
        // Arrange
        var csvContent = "name|age|city\nJohn|30|NYC\nJane|25|LA";
        var options = new CsvOptions('|', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("30", records[0][1]);
        Assert.Equal("NYC", records[0][2]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("25", records[1][1]);
        Assert.Equal("LA", records[1][2]);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void AutoDetect_WithPipeDelimiter_DetectsCorrectly()
    {
        // Arrange
        var csvContent = "id|product|price\n1|Widget|19.99\n2|Gadget|29.99\n3|Tool|39.99";
        
        // Act
        var records = Csv.ReadAutoDetect(csvContent).ToList();
        
        // Assert - AutoDetect treats first line as header
        Assert.Equal(3, records.Count);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("Widget", records[0][1]);
        Assert.Equal("19.99", records[0][2]);
        Assert.Equal("2", records[1][0]);
        Assert.Equal("Gadget", records[1][1]);
        Assert.Equal("29.99", records[1][2]);
    }

    [Fact]
    public void AutoDetect_MixedDelimiters_ChoosesMostFrequent()
    {
        // Arrange - More pipes than commas (6 pipes vs 2 commas total)
        var csvWithMorePipes = "a|b|c,d\n1|2|3,4\n5|6|7,8";
        
        // Act
        var records = Csv.ReadAutoDetect(csvWithMorePipes).ToList();
        
        // Assert - Should choose pipe as delimiter (6 pipes vs 2 commas in sample)
        Assert.Equal(2, records.Count); // Two data rows after header
        Assert.Equal(3, records[0].Length); // Split by pipe: "1", "2", "3,4"
        Assert.Equal("3,4", records[0][2]); // Comma is part of the field
    }
#endif

    [Fact]
    public void CsvParser_WithPipeDelimiter_HandlesQuotedFields()
    {
        // Arrange
        var csvContent = "\"name|with|pipes\"|age|\"city\"\n\"John|Doe\"|30|\"NYC\"";
        var options = new CsvOptions('|', '"', false);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("name|with|pipes", records[0][0]); // Pipes inside quotes preserved
        Assert.Equal("age", records[0][1]);
        Assert.Equal("city", records[0][2]);
        Assert.Equal("John|Doe", records[1][0]); // Pipe inside quotes preserved
        Assert.Equal("30", records[1][1]);
        Assert.Equal("NYC", records[1][2]);
    }

    [Fact]
    public void CsvParser_PipeDelimiter_WithEmptyFields()
    {
        // Arrange
        var csvContent = "a||c\n1||3\n||";
        var options = new CsvOptions('|', '"', false);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][1]); // Empty field
        Assert.Equal("", records[2][0]); // All empty fields
        Assert.Equal("", records[2][1]);
        Assert.Equal("", records[2][2]);
    }
}