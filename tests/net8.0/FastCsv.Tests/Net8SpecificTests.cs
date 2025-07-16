using System.Collections.Frozen;
using System.Numerics;
using Xunit;

namespace FastCsv.Tests;

public class Net8SpecificTests
{
    [Fact]
    public void SearchValuesOptimizationWorks()
    {
        // Arrange
        var csvData = "Name,Age\r\nJohn,25\r\nJane,30";
        var reader = new CsvReader(csvData.AsSpan(), CsvOptions.Default);

        // Act & Assert
        Assert.True(reader.HasMoreData);
        reader.ReadRecord();
        Assert.True(reader.HasMoreData);
        reader.ReadRecord();
        Assert.True(reader.HasMoreData);
        reader.ReadRecord();
        Assert.False(reader.HasMoreData);
    }

    [Fact]
    public void FrozenCollectionsAvailable()
    {
        // Test that FrozenSet/FrozenDictionary are available in NET8+
        var dict = new Dictionary<string, int> { ["test"] = 1 };
        var frozenDict = dict.ToFrozenDictionary();

        Assert.Equal(1, frozenDict["test"]);
    }

    [Fact]
    public void VectorizedFieldCountingWorks()
    {
        // Arrange
        var csvData = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
        var reader = new CsvReader(csvData.AsSpan(), CsvOptions.Default);

        // Act
        var record = reader.ReadRecord();

        // Assert
        Assert.True(reader.HasMoreData || record.LineNumber > 0);
        var fieldCount = 0;
        foreach (var field in record)
        {
            fieldCount++;
        }
        Assert.Equal(26, fieldCount);
    }

    [Fact]
    public void HardwareAccelerationAvailable()
    {
        // Assert
        Assert.True(Vector.IsHardwareAccelerated);
    }
}