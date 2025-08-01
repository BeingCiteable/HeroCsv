using System;
using System.Collections.Generic;
using System.Linq;
using FastCsv.Mapping;
using FastCsv.Mapping.Attributes;
using FastCsv.Mapping.Converters;
using FastCsv.Models;
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Tests for edge cases and error handling in CSV mapping (nullables, unsupported types, access modifiers, etc.)
/// </summary>
public class MappingEdgeCasesTests
{
    #region Test Models
    
    public class EdgeCaseModel
    {
        public int Id { get; set; }
        public string? NullableString { get; set; }
        public int? NullableInt { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public bool? NullableBool { get; set; }
    }
    
    public class InvalidTypeModel
    {
        public int Id { get; set; }
        public List<string> StringList { get; set; } = new(); // Not supported
        public Dictionary<string, int> Dictionary { get; set; } = new(); // Not supported
        public int[] IntArray { get; set; } = Array.Empty<int>(); // Not supported
    }
    
    public class PropertyTypeMismatchModel
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
    }
    
    public class ReadOnlyPropertyModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ComputedValue => $"{Id}-{Name}"; // Read-only
        public string ReadOnlyProp { get; } = "ReadOnly";
    }
    
    public class PrivateSetterModel
    {
        public int Id { get; set; }
        public string Name { get; private set; } = "";
        public string ProtectedProp { get; protected set; } = "";
        public string InternalProp { get; internal set; } = "";
    }
    
    public class EmptyModel
    {
    }
    
    public class AllIgnoredModel
    {
        [CsvColumn(Ignore = true)]
        public int Id { get; set; }
        
        [CsvColumn(Ignore = true)]
        public string Name { get; set; } = "";
    }
    
    public class ConflictingMappingModel
    {
        [CsvColumn("Name")]
        public string FirstName { get; set; } = "";
        
        [CsvColumn("Name")] // Same column name
        public string LastName { get; set; } = "";
    }
    
    #endregion
    
    [Fact]
    public void ParseNullableTypes_EmptyAndNullStrings_HandlesCorrectly()
    {
        var csv = @"Id,NullableString,NullableInt,NullableDateTime,NullableBool
1,Value,100,2023-01-15,true
2,,,,,
3,null,null,null,null";
        
        var results = Csv.Read<EdgeCaseModel>(csv).ToList();
        
        Assert.Equal(3, results.Count);
        
        // First row - all values present
        Assert.Equal("Value", results[0].NullableString);
        Assert.Equal(100, results[0].NullableInt);
        Assert.Equal(new DateTime(2023, 1, 15), results[0].NullableDateTime);
        Assert.True(results[0].NullableBool);
        
        // Second row - all empty
        Assert.Null(results[1].NullableString);
        Assert.Null(results[1].NullableInt);
        Assert.Null(results[1].NullableDateTime);
        Assert.Null(results[1].NullableBool);
        
        // Third row - "null" strings
        Assert.Equal("null", results[2].NullableString);
        Assert.Null(results[2].NullableInt); // "null" is not a valid int
        Assert.Null(results[2].NullableDateTime); // "null" is not a valid DateTime
        Assert.Null(results[2].NullableBool); // "null" is not a valid bool
    }
    
    [Fact]
    public void MapUnsupportedTypes_ReturnsDefaultValues()
    {
        var csv = @"Id,StringList,Dictionary,IntArray
1,""a,b,c"",""key:value"",""1,2,3""
2,,,";
        
        var results = Csv.Read<InvalidTypeModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // Unsupported types should remain as default values
        Assert.Equal(1, results[0].Id);
        Assert.Empty(results[0].StringList);
        Assert.Empty(results[0].Dictionary);
        Assert.Null(results[0].IntArray); // Arrays return null when not supported
    }
    
    [Fact]
    public void ParseInvalidFormat_ThrowsFormatException()
    {
        var csv = @"Id,Age,Date,Active
1,25,2023-01-15,true
2,invalid,invalid-date,invalid-bool
3,30.5,2023/01/15,1";
        
        // This will throw exceptions for invalid conversions
        Assert.Throws<FormatException>(() => 
        {
            var results = Csv.Read<PropertyTypeMismatchModel>(csv).ToList();
        });
    }
    
    [Fact]
    public void MapReadOnlyProperties_SkipsDuringMapping()
    {
        var csv = @"Id,Name,ComputedValue,ReadOnlyProp
1,Test,ShouldBeIgnored,AlsoIgnored
2,Another,Ignored,Ignored";
        
        var results = Csv.Read<ReadOnlyPropertyModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // Read-only properties should not be mapped
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
        Assert.Equal("1-Test", results[0].ComputedValue); // Computed from other properties
        Assert.Equal("ReadOnly", results[0].ReadOnlyProp); // Default value
    }
    
    [Fact]
    public void MapWithNonPublicSetters_OnlyMapsPublicProperties()
    {
        var csv = @"Id,Name,ProtectedProp,InternalProp
1,Test,Protected,Internal
2,Another,Prop2,Prop3";
        
        var results = Csv.Read<PrivateSetterModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // Properties with non-public setters are not mapped by reflection
        Assert.Equal(1, results[0].Id);
        Assert.Equal("", results[0].Name); // Private setter, not set
        Assert.Equal("", results[0].ProtectedProp); // Protected setter, not set
        Assert.Equal("", results[0].InternalProp); // Internal setter, not set (mapper is in different assembly)
    }
    
    [Fact]
    public void MapEmptyModel_CreatesInstanceSuccessfully()
    {
        var csv = @"Column1,Column2
Value1,Value2
Value3,Value4";
        
        var results = Csv.Read<EmptyModel>(csv).ToList();
        
        // Should create empty instances
        Assert.Equal(2, results.Count);
        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
    }
    
    [Fact]
    public void MapAllIgnoredModel_CreatesInstanceWithDefaults()
    {
        var csv = @"Id,Name
1,Test
2,Another";
        
        var results = Csv.Read<AllIgnoredModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        // All properties are ignored, so they should have default values
        Assert.Equal(0, results[0].Id);
        Assert.Equal("", results[0].Name);
    }
    
    [Fact]
    public void ParseManyColumns_HandlesHundredsSuccessfully()
    {
        // Create CSV with 100 columns
        var headers = string.Join(",", Enumerable.Range(1, 100).Select(i => $"Col{i}"));
        var values = string.Join(",", Enumerable.Range(1, 100).Select(i => i.ToString()));
        var csv = $"{headers}\n{values}";
        
        // Use dynamic mapping
        var results = Csv.ReadContent(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal(100, results[0].Length);
        Assert.Equal("1", results[0][0]);
        Assert.Equal("100", results[0][99]);
    }
    
    [Fact]
    public void ParseLongFields_Handles10000CharactersCorrectly()
    {
        var longValue = new string('A', 10000);
        var csv = $"Id,LongField\n1,{longValue}";
        
        var results = Csv.ReadContent(csv).ToList();
        
        Assert.Single(results);
        Assert.Equal("1", results[0][0]);
        Assert.Equal(longValue, results[0][1]);
    }
    
    [Fact]
    public void ParseMixedLineEndings_HandlesAllFormatsCorrectly()
    {
        var csv = "Id,Name\r\n1,Windows\n2,Unix\r3,OldMac";
        
        var results = Csv.ReadContent(csv).ToList();
        
        Assert.Equal(3, results.Count);
        Assert.Equal("Windows", results[0][1]);
        Assert.Equal("Unix", results[1][1]);
        Assert.Equal("OldMac", results[2][1]);
    }
    
    [Fact]
    public void ParseEmptyString_ReturnsEmptyCollection()
    {
        var csv = "";
        var results = Csv.Read<EdgeCaseModel>(csv).ToList();
        Assert.Empty(results);
    }
    
    [Fact]
    public void ParseHeadersOnly_ReturnsEmptyCollection()
    {
        var csv = "Id,Name,Value";
        var results = Csv.Read<EdgeCaseModel>(csv).ToList();
        Assert.Empty(results);
    }
    
    [Fact]
    public void MapSingleColumn_ParsesCorrectly()
    {
        var csv = @"Id
1
2
3";
        
        var results = Csv.Read<EdgeCaseModel>(csv).ToList();
        
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
    }
    
    [Fact]
    public void MatchPropertyNames_CaseInsensitive_MapsCorrectly()
    {
        var csv = @"id,NULLABLESTRING,nullableint
1,Test,100
2,Another,200";
        
        var results = Csv.Read<EdgeCaseModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].NullableString);
        Assert.Equal(100, results[0].NullableInt);
    }
    
    [Fact]
    public void ParseDuplicateHeaders_HandlesAppropriately()
    {
        var csv = @"Id,Name,Name,Id
1,First,Second,2
3,Third,Fourth,4";
        
        var results = Csv.ReadContent(csv).ToList();
        
        Assert.Equal(2, results.Count);
        // Behavior depends on implementation - typically last column wins
        // or all columns are preserved in string array
        Assert.Equal(4, results[0].Length);
    }
    
    [Fact]
    public void MapMultiplePropertiesToSameColumn_AllReceiveSameValue()
    {
        var csv = @"Id,Name
1,TestName
2,AnotherName";
        
        var results = Csv.Read<ConflictingMappingModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        // Both properties map to same column - last one wins or both get same value
        // Implementation dependent behavior
        Assert.Equal("TestName", results[0].FirstName);
        Assert.Equal("TestName", results[0].LastName);
    }
    
    [Fact]
    public void MapWithNullConverter_UsesDefaultConversion()
    {
        var mapping = new CsvMapping<EdgeCaseModel>();
        mapping.MapProperty("Id", 0, null!); // Null converter
        
        var csv = "1,2,3";
        var options = new CsvOptions(hasHeader: false);
        
        mapping.Options = options;
        var results = Csv.Read<EdgeCaseModel>(csv, mapping).ToList();
        
        Assert.Single(results);
        Assert.Equal(1, results[0].Id); // Should still parse without custom converter
    }
}