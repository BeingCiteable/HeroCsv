using System.Linq;
using FastCsv.Mapping.Attributes;
using FastCsv.Models;
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Tests specifically for [CsvColumn(index)] attribute functionality
/// </summary>
public class CsvColumnIndexAttributeTests
{
    public class IndexMappingModel
    {
        [CsvColumn(0)]
        public string FirstColumn { get; set; } = "";
        
        [CsvColumn(2)]
        public string ThirdColumn { get; set; } = "";
        
        [CsvColumn(1)]
        public string SecondColumn { get; set; } = "";
        
        [CsvColumn(4)]
        public int FifthColumn { get; set; }
    }

    public class MixedIndexNameModel
    {
        [CsvColumn("Name", Index = 0)]
        public string Name { get; set; } = "";
        
        [CsvColumn(Index = 1)]
        public int Age { get; set; }
        
        [CsvColumn("City", Index = 2)]
        public string City { get; set; } = "";
    }

    public class DuplicateIndexModel
    {
        [CsvColumn(0)]
        public string First { get; set; } = "";
        
        [CsvColumn(0)]
        public string AlsoFirst { get; set; } = "";
        
        [CsvColumn(1)]
        public string Second { get; set; } = "";
    }

    [Fact]
    public void CsvColumnIndex_MapsToCorrectPosition()
    {
        var csv = "A,B,C,D,E\n1,2,3,4,5";
        var results = Csv.Read<IndexMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("1", results[0].FirstColumn);
        Assert.Equal("2", results[0].SecondColumn);
        Assert.Equal("3", results[0].ThirdColumn);
        Assert.Equal(5, results[0].FifthColumn);
    }

    [Fact]
    public void CsvColumnIndex_WorksWithoutHeaders()
    {
        var csv = "1,2,3,4,5\n6,7,8,9,10";
        var options = new CsvOptions(hasHeader: false);
        var results = Csv.Read<IndexMappingModel>(csv, options).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.Equal("1", results[0].FirstColumn);
        Assert.Equal("3", results[0].ThirdColumn);
        Assert.Equal("6", results[1].FirstColumn);
        Assert.Equal("8", results[1].ThirdColumn);
    }

    [Fact]
    public void CsvColumnIndex_OutOfRange_LeavesPropertyDefault()
    {
        var csv = "A,B,C\n1,2,3"; // Only 3 columns, but model expects index 4
        var results = Csv.Read<IndexMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("1", results[0].FirstColumn);
        Assert.Equal("3", results[0].ThirdColumn);
        Assert.Equal(0, results[0].FifthColumn); // Default value, index 4 doesn't exist
    }

    [Fact]
    public void CsvColumnIndex_PreferredOverColumnName()
    {
        // When both name and index are specified, index takes precedence
        var csv = "Name,Age,City\nWrongName,25,NYC";
        var results = Csv.Read<MixedIndexNameModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("WrongName", results[0].Name); // Index 0 of data row
        Assert.Equal(25, results[0].Age); // Index 1 of data row
        Assert.Equal("NYC", results[0].City); // Index 2 of data row
    }

    [Fact]
    public void CsvColumnIndex_MultipleMappingsToSameIndex()
    {
        var csv = "A,B,C\n1,2,3";
        var options = new CsvOptions(hasHeader: false);
        var results = Csv.Read<DuplicateIndexModel>(csv, options).ToList();
        
        Assert.Equal(2, results.Count);
        // Both properties map to index 0
        Assert.Equal("A", results[0].First);
        Assert.Equal("A", results[0].AlsoFirst);
        Assert.Equal("1", results[1].First);
        Assert.Equal("1", results[1].AlsoFirst);
    }

    [Fact]
    public void CsvColumnIndex_NonSequentialIndexes()
    {
        // Model uses indexes 0, 1, 2, 4 (skips 3)
        var csv = "A,B,C,D,E,F\nfirst,second,third,skip,5,extra";
        var results = Csv.Read<IndexMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("first", results[0].FirstColumn);
        Assert.Equal("second", results[0].SecondColumn);
        Assert.Equal("third", results[0].ThirdColumn);
        Assert.Equal(5, results[0].FifthColumn); // Column at index 4
    }
}