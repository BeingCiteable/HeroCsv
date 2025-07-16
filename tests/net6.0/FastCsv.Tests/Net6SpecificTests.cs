using System.Numerics;
using Xunit;

namespace FastCsv.Tests;

public class Net6SpecificTests
{
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