using System.IO;
using System.Linq;
using System.Text;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Core;

/// <summary>
/// Tests specifically for Csv.CreateReader() factory methods
/// </summary>
public class CsvCreateReaderTests
{
    [Fact]
    public void CreateReader_FromReadOnlyMemory_CreatesValidReader()
    {
        ReadOnlyMemory<char> content = "Name,Age\nJohn,25\nJane,30".AsMemory();
        using var reader = Csv.CreateReader(content);
        
        Assert.NotNull(reader);
        Assert.True(reader.HasMoreData);
        
        // First record is header with default options
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("Name", record.ToArray()[0]);
        Assert.Equal("Age", record.ToArray()[1]);
    }

    [Fact]
    public void CreateReader_FromString_CreatesValidReader()
    {
        var content = "Name,Age\nJohn,25";
        using var reader = Csv.CreateReader(content);
        
        Assert.NotNull(reader);
        Assert.True(reader.HasMoreData);
        
        // Skip header
        reader.TryReadRecord(out _);
        
        // Read data
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("John", record.ToArray()[0]);
        Assert.Equal("25", record.ToArray()[1]);
    }

    [Fact]
    public void CreateReader_FromStream_CreatesValidReader()
    {
        var content = "Name,Age\nJohn,25";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var reader = Csv.CreateReader(stream);
        
        Assert.NotNull(reader);
        
        // Skip header
        reader.TryReadRecord(out _);
        
        // Read data
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("John", record.ToArray()[0]);
    }

    [Fact]
    public void CreateReader_WithCustomEncoding_HandlesCorrectly()
    {
        var content = "Name,City\nJosé,São Paulo";
        using var stream = new MemoryStream(Encoding.UTF32.GetBytes(content));
        using var reader = Csv.CreateReader(stream, CsvOptions.Default, Encoding.UTF32, leaveOpen: false);
        
        Assert.NotNull(reader);
        
        // Skip header
        reader.TryReadRecord(out _);
        
        // Read data with special characters
        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal("José", record.ToArray()[0]);
        Assert.Equal("São Paulo", record.ToArray()[1]);
    }

    [Fact]
    public void CreateReader_StreamLeaveOpen_KeepsStreamOpen()
    {
        var content = "Name,Age\nJohn,25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        using (var reader = Csv.CreateReader(stream, CsvOptions.Default, Encoding.UTF8, leaveOpen: true))
        {
            reader.TryReadRecord(out _);
        }
        
        // Stream should still be open
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public void CreateReader_EmptyContent_CreatesReaderWithNoData()
    {
        using var reader = Csv.CreateReader("");
        
        Assert.NotNull(reader);
        Assert.False(reader.HasMoreData);
        Assert.False(reader.TryReadRecord(out _));
    }
}