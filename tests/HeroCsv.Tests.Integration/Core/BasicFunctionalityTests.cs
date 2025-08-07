using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroCsv;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Integration.Core;

/// <summary>
/// Integration tests for basic end-to-end CSV functionality
/// </summary>
public class BasicFunctionalityTests
{
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Fact]
    public void EndToEnd_BasicCsvOperations()
    {
        var content = "Name,Age\nJohn,25\nJane,30";

        // Test ReadContent
        var records = Csv.ReadContent(content).ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);

        // Test CountRecords
        var count = Csv.CountRecords(content);
        Assert.Equal(2, count);

        // Test CreateReader
        using var reader = Csv.CreateReader(content);
        Assert.NotNull(reader);
        Assert.True(reader.HasMoreData);
    }

    [Fact]
    public void EndToEnd_TypedCsvOperations()
    {
        var content = "Name,Age\nJohn,25\nJane,30";

        // Test typed reading
        var people = Csv.Read<Person>(content).ToList();
        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
    }

    [Fact]
    public void EndToEnd_CustomOptions()
    {
        var content = "Name;Age\nJohn;25";
        var options = new CsvOptions(delimiter: ';');

        var records = Csv.ReadContent(content, options).ToList();
        Assert.Single(records);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("25", records[0][1]);
    }

    [Fact]
    public void EndToEnd_NoHeadersOption()
    {
        var content = "John,25\nJane,30";
        var options = new CsvOptions(hasHeader: false);

        var records = Csv.ReadContent(content, options).ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task EndToEnd_AsyncFileOperations()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Name,Age\nJohn,25", TestContext.Current.CancellationToken);

            var records = await Csv.ReadFileAsync(tempFile, cancellationToken: TestContext.Current.CancellationToken);

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
