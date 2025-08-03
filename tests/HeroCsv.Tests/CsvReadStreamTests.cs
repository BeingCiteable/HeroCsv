using System.IO;
using System.Linq;
using System.Text;
using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests specifically for Csv.ReadStream() methods
/// </summary>
public class CsvReadStreamTests
{
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
    }

    [Fact]
    public void ReadStream_StringArrays_ReadsCorrectly()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var records = Csv.ReadStream(stream).ToList();
        
        Assert.Equal(2, records.Count); // Headers are skipped
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadStream_WithCustomDelimiter_ParsesCorrectly()
    {
        var content = "Name|Age|City\nJohn|25|NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var options = new CsvOptions(delimiter: '|');
        
        var records = Csv.ReadStream(stream, options).ToList();
        
        Assert.Single(records);
        Assert.Equal("NYC", records[0][2]);
    }

    [Fact]
    public void ReadStream_Generic_MapsToObjects()
    {
        var content = "Name,Age,City\nJohn,25,NYC\nJane,30,LA";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var people = Csv.ReadStream<Person>(stream).ToList();
        
        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadStream_GenericWithOptions_RespectsConfiguration()
    {
        var content = "Name;Age;City\nJohn;25;NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var options = new CsvOptions(delimiter: ';');
        
        var people = Csv.ReadStream<Person>(stream, options).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void ReadStream_WithManualMapping_AppliesMapping()
    {
        var content = "PersonName,PersonAge,Location\nJohn,25,NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("Name", "PersonName")
            .MapProperty("Age", "PersonAge")
            .MapProperty("City", "Location");
        
        var people = Csv.ReadStream(stream, mapping).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadStreamAutoMapWithOverrides_AppliesCustomLogic()
    {
        var content = "Name,Age,City\nJohn,25,NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var people = Csv.ReadStreamAutoMapWithOverrides<Person>(stream, mapping => 
        {
            mapping.MapProperty("Age", 1, value => int.Parse(value) * 2);
        }).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(50, people[0].Age); // Doubled
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadStream_EmptyStream_ReturnsNoRecords()
    {
        using var stream = new MemoryStream();
        
        var records = Csv.ReadStream(stream).ToList();
        
        Assert.Empty(records);
    }

    [Fact]
    public void ReadStream_LargeData_HandlesEfficiently()
    {
        var sb = new StringBuilder("Id,Name,Value\n");
        for (int i = 1; i <= 1000; i++)
        {
            sb.AppendLine($"{i},Name{i},{i * 10}");
        }
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        
        var records = Csv.ReadStream(stream).ToList();
        
        Assert.Equal(1000, records.Count);
        Assert.Equal("500", records[499][0]);
    }

    [Fact]
    public void ReadStream_StreamPosition_ResetsToBeginning()
    {
        var content = "Name,Age\nJohn,25";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        // Move stream position
        stream.Position = 5;
        
        var records = Csv.ReadStream(stream).ToList();
        
        // Should still read from beginning
        Assert.Single(records);
        Assert.Equal("John", records[0][0]);
    }
}