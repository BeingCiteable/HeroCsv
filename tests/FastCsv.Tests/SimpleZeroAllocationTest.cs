using System;
using System.Text;
using Xunit;

namespace FastCsv.Tests;

public class SimpleZeroAllocationTest
{
    [Fact]
    public void Memory_vs_String_ProduceSameResults()
    {
        // Arrange
        var csvContent = "Name,Age\nJohn,30\nJane,25";
        var csvMemory = csvContent.AsMemory();
        
        // Act - Both should produce identical results
        var stringRecords = Csv.ReadAllRecords(csvContent);
        var memoryRecords = Csv.ReadAllRecords(csvMemory);
        
        // Assert
        Assert.Equal(stringRecords.Count, memoryRecords.Count);
        
        for (int i = 0; i < stringRecords.Count; i++)
        {
            Assert.Equal(stringRecords[i].Length, memoryRecords[i].Length);
            for (int j = 0; j < stringRecords[i].Length; j++)
            {
                Assert.Equal(stringRecords[i][j], memoryRecords[i][j]);
            }
        }
    }
    
    [Fact]
    public void CreateReader_Memory_WorksCorrectly()
    {
        // Arrange
        var csvContent = "A,B\n1,2\n3,4".AsMemory();
        
        // Act
        using var reader = Csv.CreateReader(csvContent);
        var count = 0;
        while (reader.TryReadRecord(out _))
        {
            count++;
        }
        
        // Assert  
        Assert.True(count > 0); // Just verify it reads something
    }
    
    [Fact]
    public void Memory_Reset_Works()
    {
        // Arrange
        var csvContent = "X,Y\n10,20".AsMemory();
        
        // Act
        using var reader = Csv.CreateReader(csvContent);
        
        // Read once
        reader.TryReadRecord(out var first1);
        var firstValue1 = first1?.GetField(0).ToString();
        
        // Reset and read again
        reader.Reset();
        reader.TryReadRecord(out var first2);
        var firstValue2 = first2?.GetField(0).ToString();
        
        // Assert
        Assert.Equal(firstValue1, firstValue2);
    }
}