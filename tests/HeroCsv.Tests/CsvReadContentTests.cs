using System.Linq;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests specifically for Csv.ReadContent() which returns string arrays
/// </summary>
public class CsvReadContentTests
{
    [Fact]
    public void ReadContent_SimpleData_ReturnsStringArrays()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var records = Csv.ReadContent(content).ToList();
        
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadContent_WithDelimiterChar_ParsesCorrectly()
    {
        var content = "Name;Age\nJohn;25";
        var records = Csv.ReadContent(content, ';').ToList();
        
        Assert.Single(records);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadContent_WithOptions_RespectsConfiguration()
    {
        var content = "John|25|NYC";
        var options = new CsvOptions(delimiter: '|', hasHeader: false);
        var records = Csv.ReadContent(content, options).ToList();
        
        Assert.Single(records);
        Assert.Equal(3, records[0].Length);
        Assert.Equal("NYC", records[0][2]);
    }

    [Fact]
    public void ReadContent_QuotedFields_HandlesEscaping()
    {
        var content = "Name,Address\n\"John Doe\",\"123 Main St, Apt 4\"";
        var records = Csv.ReadContent(content).ToList();
        
        Assert.Single(records);
        Assert.Equal("John Doe", records[0][0]);
        Assert.Equal("123 Main St, Apt 4", records[0][1]);
    }

    [Fact]
    public void ReadContent_EmptyFields_PreservesEmptyStrings()
    {
        var content = "A,B,C\n1,,3\n,2,";
        var records = Csv.ReadContent(content).ToList();
        
        Assert.Equal(2, records.Count);
        Assert.Equal("", records[0][1]);
        Assert.Equal("", records[1][0]);
        Assert.Equal("", records[1][2]);
    }

    [Fact]
    public void ReadContent_MixedLineEndings_HandlesAllTypes()
    {
        var content = "A,B\r\n1,2\n3,4\r5,6";
        var records = Csv.ReadContent(content).ToList();
        
        Assert.Equal(3, records.Count);
        Assert.Equal("5", records[2][0]);
        Assert.Equal("6", records[2][1]);
    }

    [Fact]
    public void ReadContent_NoHeader_ReturnsAllRows()
    {
        var content = "1,2,3\n4,5,6";
        var options = new CsvOptions(hasHeader: false);
        var records = Csv.ReadContent(content, options).ToList();
        
        Assert.Equal(2, records.Count);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("4", records[1][0]);
    }
}