using System.Buffers.Text;
using System.Numerics;
using System.Text;
using Xunit;

namespace FastCsv.Tests;

public class Net7SpecificTests
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
    public void BuffersTextAvailable()
    {
        // Test that System.Buffers.Text is available in NET7+
        var bytes = Encoding.UTF8.GetBytes("123");
        var success = Utf8Parser.TryParse(bytes, out int result, out _);

        Assert.True(success);
        Assert.Equal(123, result);
    }

    [Fact]
    public void HardwareAccelerationAvailable()
    {
        // Assert
        Assert.True(Vector.IsHardwareAccelerated);
    }
}