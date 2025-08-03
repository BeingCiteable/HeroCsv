using System.Linq;
using HeroCsv.Mapping.Attributes;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests specifically for [CsvColumn(Ignore = true)] attribute functionality
/// </summary>
public class CsvIgnoreAttributeTests
{
    public class IgnorePropertyModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = "";
        
        [CsvColumn(Ignore = true)]
        public string Password { get; set; } = "DefaultPassword";
        
        [CsvColumn("Description", Ignore = true)]
        public string IgnoredWithName { get; set; } = "DefaultIgnored";
        
        public string Email { get; set; } = "";
    }

    public class PartialIgnoreModel
    {
        public string KeepThis { get; set; } = "";
        
        [CsvColumn(Ignore = true)]
        public string IgnoreThis { get; set; } = "ShouldNotChange";
        
        public int AlsoKeep { get; set; }
    }

    public class AllPropertiesIgnoredModel
    {
        [CsvColumn(Ignore = true)]
        public int Id { get; set; } = 999;
        
        [CsvColumn(Ignore = true)]
        public string Name { get; set; } = "DefaultName";
        
        [CsvColumn(Ignore = true)]
        public bool Active { get; set; } = true;
    }

    [Fact]
    public void CsvIgnore_PreventsMappingFromCsv()
    {
        var csv = "Id,Name,Password,Email,Description\n1,John,SecretPass,john@email.com,Some description";
        var results = Csv.Read<IgnorePropertyModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("John", results[0].Name);
        Assert.Equal("DefaultPassword", results[0].Password); // Should keep default value
        Assert.Equal("DefaultIgnored", results[0].IgnoredWithName); // Should keep default value
        Assert.Equal("john@email.com", results[0].Email);
    }

    [Fact]
    public void CsvIgnore_WorksWithPartialProperties()
    {
        var csv = "KeepThis,IgnoreThis,AlsoKeep\nValue1,ShouldNotMap,42";
        var results = Csv.Read<PartialIgnoreModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("Value1", results[0].KeepThis);
        Assert.Equal("ShouldNotChange", results[0].IgnoreThis); // Keeps default
        Assert.Equal(42, results[0].AlsoKeep);
    }

    [Fact]
    public void CsvIgnore_AllPropertiesIgnored_StillCreatesObjects()
    {
        var csv = "Id,Name,Active\n1,Test,false\n2,Another,true";
        var results = Csv.Read<AllPropertiesIgnoredModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // All properties should have their default values
        foreach (var result in results)
        {
            Assert.Equal(999, result.Id);
            Assert.Equal("DefaultName", result.Name);
            Assert.True(result.Active);
        }
    }

    [Fact]
    public void CsvIgnore_IgnoredColumnStillInCsv_DoesNotAffectOtherMappings()
    {
        var csv = "Id,Name,Password,Email\n1,John,pass123,john@test.com\n2,Jane,pass456,jane@test.com";
        var results = Csv.Read<IgnorePropertyModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // Password column exists in CSV but should be ignored
        Assert.Equal("DefaultPassword", results[0].Password);
        Assert.Equal("DefaultPassword", results[1].Password);
        
        // Other properties should map normally
        Assert.Equal("John", results[0].Name);
        Assert.Equal("Jane", results[1].Name);
    }

    [Fact]
    public void CsvIgnore_MissingIgnoredColumn_NoEffect()
    {
        var csv = "Id,Name,Email\n1,John,john@test.com"; // No Password column
        var results = Csv.Read<IgnorePropertyModel>(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("John", results[0].Name);
        Assert.Equal("DefaultPassword", results[0].Password);
        Assert.Equal("john@test.com", results[0].Email);
    }
}