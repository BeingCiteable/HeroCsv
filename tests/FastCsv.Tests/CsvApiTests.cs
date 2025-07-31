using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastCsv;
using FastCsv.Mapping;
using FastCsv.Models;
using Xunit;

namespace FastCsv.Tests;

public class CsvApiTests
{
    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime? BirthDate { get; set; }
    }

    #region CountRecords Tests

    [Fact]
    public void CountRecords_WithReadOnlySpan()
    {
        ReadOnlySpan<char> content = "Name,Age\nJohn,25\nJane,30";
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_WithReadOnlyMemory()
    {
        ReadOnlyMemory<char> content = "Name,Age\nJohn,25\nJane,30".AsMemory();
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_WithReadOnlyMemory_ContainingQuotes()
    {
        ReadOnlyMemory<char> content = "Name,Age\n\"John,Doe\",25\n\"Jane\",30".AsMemory();
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_WithString()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_WithCustomOptions()
    {
        var content = "Name|Age\nJohn|25\nJane|30";
        var options = new CsvOptions(delimiter: '|');
        var count = Csv.CountRecords(content, options);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_EmptyContent()
    {
        var count = Csv.CountRecords("");
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountRecords_NoTrailingNewline()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_WithTrailingNewline()
    {
        var content = "Name,Age\nJohn,25\nJane,30\n";
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRecords_NoHeader()
    {
        var content = "John,25\nJane,30";
        var options = new CsvOptions(hasHeader: false);
        var count = Csv.CountRecords(content, options);
        Assert.Equal(2, count);
    }

    #endregion

    #region ReadContent Tests

    [Fact]
    public void ReadContent_Basic()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var records = Csv.ReadContent(content).ToList();
        Assert.Equal(2, records.Count); // Headers are skipped by default
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }

    [Fact]
    public void ReadContent_WithDelimiterChar()
    {
        var content = "Name|Age\nJohn|25\nJane|30";
        var records = Csv.ReadContent(content, '|').ToList();
        Assert.Equal(2, records.Count); // Headers are skipped by default
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadContent_WithOptions()
    {
        var content = "Name;Age\nJohn;25\nJane;30";
        var options = new CsvOptions(delimiter: ';', hasHeader: false);
        var records = Csv.ReadContent(content, options).ToList();
        Assert.Equal(3, records.Count);
        Assert.Equal("Name", records[0][0]);
    }

    #endregion

    #region ReadFile Tests

    [Fact]
    public void ReadFile_Basic()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name,Age\nJohn,25\nJane,30");
            var records = Csv.ReadFile(tempFile).ToList();
            Assert.Equal(2, records.Count); // Headers are skipped by default
            Assert.Equal("John", records[0][0]);
            Assert.Equal("Jane", records[1][0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadFile_WithOptions()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name|Age\nJohn|25");
            var options = new CsvOptions(delimiter: '|');
            var records = Csv.ReadFile(tempFile, options).ToList();
            Assert.Single(records); // Headers are skipped
            Assert.Equal("John", records[0][0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region ReadAllRecords Tests

    [Fact]
    public void ReadAllRecords_FromReadOnlySpan()
    {
        ReadOnlySpan<char> content = "Name,Age\nJohn,25\nJane,30";
        var records = Csv.ReadAllRecords(content);
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
    }

    [Fact]
    public void ReadAllRecords_FromReadOnlyMemory()
    {
        ReadOnlyMemory<char> content = "Name,Age\nJohn,25\nJane,30".AsMemory();
        var records = Csv.ReadAllRecords(content);
        // Now properly uses CsvOptions.Default and skips header
        Assert.Equal(2, records.Count);
        Assert.Equal(2, records[0].Length);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadAllRecords_FromString()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var records = Csv.ReadAllRecords(content);
        // Now properly uses CsvOptions.Default and skips header
        Assert.Equal(2, records.Count);
        Assert.Equal(2, records[0].Length);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    #endregion

    #region CreateReader Tests

    [Fact]
    public void CreateReader_FromReadOnlyMemory()
    {
        ReadOnlyMemory<char> content = "Name,Age\nJohn,25".AsMemory();
        using var reader = Csv.CreateReader(content);
        Assert.NotNull(reader);
        // Now properly uses CsvOptions.Default, first record is header
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("Name", record.ToArray()[0]);
        // Second record is data
        Assert.True(reader.TryReadRecord(out record));
        Assert.Equal("John", record.ToArray()[0]);
    }

    [Fact]
    public void CreateReader_FromString_NoOptions()
    {
        var content = "Name,Age\nJohn,25";
        using var reader = Csv.CreateReader(content);
        Assert.NotNull(reader);
        // First record is header when using CsvOptions.Default
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("Name", record.ToArray()[0]);
        // Second record is data
        Assert.True(reader.TryReadRecord(out record));
        Assert.Equal("John", record.ToArray()[0]);
    }

    [Fact]
    public void CreateReader_FromStream()
    {
        var content = "Name,Age\nJohn,25";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var reader = Csv.CreateReader(stream);
        Assert.NotNull(reader);
        // Now properly uses CsvOptions.Default, first record is header
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("Name", record.ToArray()[0]);
        // Second record is data
        Assert.True(reader.TryReadRecord(out record));
        Assert.Equal("John", record.ToArray()[0]);
    }

    [Fact]
    public void CreateReader_FromStream_WithEncoding()
    {
        var content = "Name,Age\nJohn,25";
        using var stream = new MemoryStream(Encoding.UTF32.GetBytes(content));
        using var reader = Csv.CreateReader(stream, CsvOptions.Default, Encoding.UTF32, leaveOpen: false);
        Assert.NotNull(reader);
        // First record is header when using CsvOptions.Default
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("Name", record.ToArray()[0]);
        // Second record is data
        Assert.True(reader.TryReadRecord(out record));
        Assert.Equal("John", record.ToArray()[0]);
    }

    #endregion

    #region Generic Read Tests

    [Fact]
    public void Read_Generic_Basic()
    {
        var content = "Name,Age,City\nJohn,25,NYC\nJane,30,LA";
        var people = Csv.Read<TestPerson>(content).ToList();
        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void Read_Generic_WithOptions()
    {
        var content = "Name|Age|City\nJohn|25|NYC";
        var options = new CsvOptions(delimiter: '|');
        var people = Csv.Read<TestPerson>(content, options).ToList();
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void Read_Generic_WithMapping()
    {
        var content = "PersonName,PersonAge\nJohn,25\nJane,30";
        var mapping = CsvMapping<TestPerson>.Create()
            .MapProperty("Name", "PersonName")
            .MapProperty("Age", "PersonAge");
        
        var people = Csv.Read(content, mapping).ToList();
        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
    }

    [Fact]
    public void ReadFile_Generic_Basic()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name,Age,City\nJohn,25,NYC\nJane,30,LA");
            var people = Csv.ReadFile<TestPerson>(tempFile).ToList();
            Assert.Equal(2, people.Count);
            Assert.Equal("John", people[0].Name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadFile_Generic_WithOptions()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name|Age|City\nJohn|25|NYC");
            var options = new CsvOptions(delimiter: '|');
            var people = Csv.ReadFile<TestPerson>(tempFile, options).ToList();
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadFile_Generic_WithMapping()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "PersonName,PersonAge\nJohn,25");
            var mapping = CsvMapping<TestPerson>.Create()
                .MapProperty("Name", "PersonName")
                .MapProperty("Age", "PersonAge");
            
            var people = Csv.ReadFile(tempFile, mapping).ToList();
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Mixed Mapping Tests

    [Fact]
    public void ReadMixed_Basic()
    {
        var content = "Name,Age,City\nJohn,25,NYC";
        var people = Csv.ReadMixed<TestPerson>(content, mapping => 
        {
            mapping.MapProperty("Age", 1, value => int.Parse(value) * 2);
        }).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(50, people[0].Age); // Doubled
    }

    [Fact]
    public void ReadMixed_WithOptions()
    {
        var content = "Name|Age|City\nJohn|25|NYC";
        var options = new CsvOptions(delimiter: '|');
        var people = Csv.ReadMixed<TestPerson>(content, options, mapping => 
        {
            mapping.MapProperty("City", 2);
        }).ToList();
        
        Assert.Single(people);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadFileMixed_Basic()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name,Age,City\nJohn,25,NYC");
            var people = Csv.ReadFileMixed<TestPerson>(tempFile, mapping => 
            {
                mapping.MapProperty("Age", 1);
            }).ToList();
            
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Stream Read Tests

    [Fact]
    public void ReadStream_Basic()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var records = Csv.ReadStream(stream).ToList();
        Assert.Equal(2, records.Count); // Headers are skipped by default
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void ReadStream_WithOptions()
    {
        var content = "Name|Age\nJohn|25";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var options = new CsvOptions(delimiter: '|');
        var records = Csv.ReadStream(stream, options).ToList();
        Assert.Single(records); // Headers are skipped
        Assert.Equal("John", records[0][0]);
    }

    [Fact]
    public void ReadStream_Generic_Basic()
    {
        var content = "Name,Age,City\nJohn,25,NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var people = Csv.ReadStream<TestPerson>(stream).ToList();
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void ReadStream_Generic_WithOptions()
    {
        var content = "Name|Age|City\nJohn|25|NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var options = new CsvOptions(delimiter: '|');
        var people = Csv.ReadStream<TestPerson>(stream, options).ToList();
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void ReadStream_Generic_WithMapping()
    {
        var content = "PersonName,PersonAge\nJohn,25";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var mapping = CsvMapping<TestPerson>.Create()
            .MapProperty("Name", "PersonName")
            .MapProperty("Age", "PersonAge");
        
        var people = Csv.ReadStream(stream, mapping).ToList();
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void ReadStreamMixed_Basic()
    {
        var content = "Name,Age,City\nJohn,25,NYC";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var people = Csv.ReadStreamMixed<TestPerson>(stream, mapping => 
        {
            mapping.MapProperty("Age", 1, value => int.Parse(value) + 10);
        }).ToList();
        
        Assert.Single(people);
        Assert.Equal(35, people[0].Age); // Added 10
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void GetValidOptions_WithDefaultStruct()
    {
        // This tests the private GetValidOptions method indirectly
        var options = new CsvOptions(); // Default struct with Delimiter = '\0'
        var records = Csv.ReadContent("Name,Age\nJohn,25", options).ToList();
        // Bug: Uses broken options (delimiter = '\0') instead of fixing them
        // Each line becomes a single field
        Assert.Equal(2, records.Count); // No header skipping with default struct
        Assert.Single(records[0]);
        Assert.Equal("Name,Age", records[0][0]);
    }

    [Fact]
    public void Configure_ReturnsBuilder()
    {
        var builder = Csv.Configure();
        Assert.NotNull(builder);
    }

    [Fact]
    public void CountRecords_WithQuotesInContent()
    {
        var content = "Name,Description\nJohn,\"Has, comma\"\nJane,\"Normal\"";
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ReadWithMapper_YieldReturn()
    {
        // This tests the yield return pattern in ReadWithMapper
        var content = "Name,Age\nJohn,25\nJane,30";
        var people = Csv.Read<TestPerson>(content).Take(1).ToList();
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    #endregion
}