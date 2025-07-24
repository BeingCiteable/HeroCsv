using System.IO;
using System.Text;
using Xunit;

namespace FastCsv.Tests;

public class DataSourceTests
{
    [Fact]
    public void StringDataSource_SupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var options = new CsvOptions(hasHeader: false);
        
        // Act & Assert
        using var reader = Csv.CreateReader(csv, options);
        Assert.True(reader.TryReadRecord(out var record1));
        Assert.Equal("A", record1.GetField(0).ToString());
        
        reader.Reset();
        Assert.True(reader.TryReadRecord(out var record2));
        Assert.Equal("A", record2.GetField(0).ToString()); // Should be back at the beginning
    }
    
    [Fact]
    public void MemoryDataSource_SupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4".AsMemory();
        var options = new CsvOptions(hasHeader: false);
        
        // Act & Assert
        using var reader = Csv.CreateReader(csv, options);
        reader.TryReadRecord(out _); // Skip first
        reader.TryReadRecord(out var beforeReset);
        Assert.Equal("3", beforeReset.GetField(0).ToString());
        
        reader.Reset();
        reader.TryReadRecord(out var afterReset);
        Assert.Equal("A", afterReset.GetField(0).ToString());
    }
    
    [Fact]
    public void StreamDataSource_SeekableStreamSupportsReset()
    {
        // Arrange
        var csv = "A,B\n1,2\n3,4";
        var bytes = Encoding.UTF8.GetBytes(csv);
        using var stream = new MemoryStream(bytes);
        var options = new CsvOptions(hasHeader: false);
        
        // Act & Assert
        using var reader = Csv.CreateReader(stream, options, leaveOpen: true);
        var count1 = reader.CountRecords();
        
        reader.Reset();
        var count2 = reader.CountRecords();
        
        Assert.Equal(3, count1);
        Assert.Equal(3, count2);
    }
    
    [Fact]
    public void AllDataSources_HandleEmptyContent()
    {
        // String
        using var stringReader = Csv.CreateReader("");
        Assert.False(stringReader.TryReadRecord(out _));
        
        // Memory
        using var memoryReader = Csv.CreateReader(ReadOnlyMemory<char>.Empty);
        Assert.False(memoryReader.TryReadRecord(out _));
        
        // Stream
        using var stream = new MemoryStream();
        using var streamReader = Csv.CreateReader(stream);
        Assert.False(streamReader.TryReadRecord(out _));
    }
    
    [Fact]
    public void AllDataSources_ProduceIdenticalResults()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var options = new CsvOptions(hasHeader: false);
        
        // Act
        var stringRecords = Csv.ReadAllRecords(csvContent, options);
        var memoryRecords = Csv.ReadAllRecords(csvContent.AsMemory(), options);
        
        IReadOnlyList<string[]> streamRecords;
        using (var stream = new MemoryStream(bytes))
        {
            using var reader = Csv.CreateReader(stream, options);
            streamRecords = reader.ReadAllRecords();
        }
        
        // Assert
        Assert.Equal(3, stringRecords.Count);
        Assert.Equal(3, memoryRecords.Count);
        Assert.Equal(3, streamRecords.Count);
        
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(stringRecords[i][0], memoryRecords[i][0]);
            Assert.Equal(stringRecords[i][0], streamRecords[i][0]);
            Assert.Equal(stringRecords[i][1], memoryRecords[i][1]);
            Assert.Equal(stringRecords[i][1], streamRecords[i][1]);
            Assert.Equal(stringRecords[i][2], memoryRecords[i][2]);
            Assert.Equal(stringRecords[i][2], streamRecords[i][2]);
        }
    }
}