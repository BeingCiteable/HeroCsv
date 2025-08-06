using System.Linq;
using HeroCsv.Mapping.Attributes;
using Xunit;

namespace HeroCsv.Tests.Mapping;

/// <summary>
/// Tests specifically for [CsvColumn("ColumnName")] attribute functionality
/// </summary>
public class CsvColumnNameAttributeTests
{
    public class NameMappingModel
    {
        [CsvColumn("Product ID")]
        public int Id { get; set; }
        
        [CsvColumn("Product Name")]
        public string Name { get; set; } = "";
        
        [CsvColumn("Unit Price")]
        public decimal Price { get; set; }
        
        // No attribute - should use property name
        public string Category { get; set; } = "";
    }

    public class SpecialCharacterModel
    {
        [CsvColumn("Column With Spaces")]
        public string SpacedColumn { get; set; } = "";
        
        [CsvColumn("Column-With-Dashes")]
        public string DashedColumn { get; set; } = "";
        
        [CsvColumn("Column.With.Dots")]
        public string DottedColumn { get; set; } = "";
    }

    [Fact]
    public void CsvColumnName_MapsToSpecifiedColumnName()
    {
        var csv = "Product ID,Product Name,Unit Price,Category\n1,Widget,19.99,Electronics";
        var results = Csv.Read<NameMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Widget", results[0].Name);
        Assert.Equal(19.99m, results[0].Price);
        Assert.Equal("Electronics", results[0].Category);
    }

    [Fact]
    public void CsvColumnName_OverridesPropertyName()
    {
        // CSV has "Name" column but model property is mapped to "Product Name"
        var csv = "Product ID,Name,Product Name,Unit Price\n1,Wrong,Correct,19.99";
        var results = Csv.Read<NameMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("Correct", results[0].Name); // Should use "Product Name" not "Name"
    }

    [Fact]
    public void CsvColumnName_HandlesSpecialCharacters()
    {
        var csv = "Column With Spaces,Column-With-Dashes,Column.With.Dots\nSpace Value,Dash Value,Dot Value";
        var results = Csv.Read<SpecialCharacterModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("Space Value", results[0].SpacedColumn);
        Assert.Equal("Dash Value", results[0].DashedColumn);
        Assert.Equal("Dot Value", results[0].DottedColumn);
    }

    [Fact]
    public void CsvColumnName_CaseInsensitiveMatching()
    {
        var csv = "product id,PRODUCT NAME,unit price,CATEGORY\n1,Widget,19.99,Electronics";
        var results = Csv.Read<NameMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Widget", results[0].Name);
    }

    [Fact]
    public void CsvColumnName_MissingMappedColumn_LeavesPropertyDefault()
    {
        var csv = "Product ID,Category\n1,Electronics"; // Missing "Product Name" and "Unit Price"
        var results = Csv.Read<NameMappingModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("", results[0].Name); // Default value
        Assert.Equal(0m, results[0].Price); // Default value
        Assert.Equal("Electronics", results[0].Category);
    }
}