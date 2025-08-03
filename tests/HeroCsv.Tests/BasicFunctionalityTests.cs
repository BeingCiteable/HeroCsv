using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroCsv;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests to verify basic CSV reading and writing functionality
/// </summary>
public class BasicFunctionalityTests
{
    [Fact]
    public void Csv_ReadContent_Basic()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var records = Csv.ReadContent(content).ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void Csv_ReadContent_NoHeaders()
    {
        var content = "John,25\nJane,30";
        var options = new CsvOptions(hasHeader: false);
        var records = Csv.ReadContent(content, options).ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
    }

    [Fact]
    public void Csv_ReadContent_CustomDelimiter()
    {
        var content = "Name;Age\nJohn;25";
        var records = Csv.ReadContent(content, ';').ToList();

        Assert.Single(records);
        Assert.Equal("John", records[0][0]);
    }

    [Fact]
    public void Csv_CountRecords_Basic()
    {
        var content = "Header\nRow1\nRow2\nRow3";
        var count = Csv.CountRecords(content);
        Assert.Equal(3, count);
    }

    [Fact]
    public void Csv_ReadAllRecords_Basic()
    {
        var content = "A,B\n1,2\n3,4";
        var records = Csv.ReadAllRecords(content);

        // Now properly uses CsvOptions.Default with hasHeader:true
        Assert.Equal(2, records.Count);
        Assert.Equal(2, records[0].Length);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("2", records[0][1]);
    }

    [Fact]
    public void Csv_CreateReader_Basic()
    {
        using var reader = Csv.CreateReader("A,B\n1,2");

        Assert.NotNull(reader);
        Assert.True(reader.HasMoreData);
    }

    [Fact]
    public void CsvOptions_Default()
    {
        var options = CsvOptions.Default;

        Assert.Equal(',', options.Delimiter);
        Assert.Equal('"', options.Quote);
        Assert.True(options.HasHeader);
    }

    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Fact]
    public void Csv_Read_Typed()
    {
        var content = "Name,Age\nJohn,25\nJane,30";
        var people = Csv.Read<Person>(content).ToList();

        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
    }

    [Fact]
    public void Csv_Configure_Basic()
    {
        var builder = Csv.Configure();
        Assert.NotNull(builder);
    }

    [Fact]
    public void ICsvReader_BasicOperations()
    {
        using var reader = Csv.CreateReader("A,B\n1,2");

        // First record is the header when using default options (hasHeader:true)
        var header = reader.ReadRecord();
        Assert.NotNull(header);
        Assert.Equal("A", header.GetField(0).ToString());

        // Second record is the data
        var record = reader.ReadRecord();
        Assert.NotNull(record);
        Assert.Equal("1", record.GetField(0).ToString());

        // No more records
        Assert.False(reader.TryReadRecord(out var record2));
        Assert.Null(record2);
    }

    [Fact]
    public void ICsvReader_Reset()
    {
        using var reader = Csv.CreateReader("A,B\n1,2");

        reader.ReadRecord();
        reader.Reset();

        var record = reader.ReadRecord();
        Assert.NotNull(record);
    }

    [Fact]
    public void ICsvRecord_GetField()
    {
        using var reader = Csv.CreateReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);

        Assert.Equal(3, record.FieldCount);
        Assert.Equal("A", record.GetField(0).ToString());
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task Csv_ReadFileAsync_Basic()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Name,Age\nJohn,25", TestContext.Current.CancellationToken);
            var records = await Csv.ReadFileAsync(tempFile, CsvOptions.Default, null, TestContext.Current.CancellationToken);

            Assert.Single(records);
            Assert.Equal("John", records[0][0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
#endif
}