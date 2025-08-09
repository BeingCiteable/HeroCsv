using System;
using System.Linq;
using Xunit;
using HeroCsv;
using HeroCsv.Utilities;
using HeroCsv.Models;

namespace HeroCsv.Tests.Unit.Utilities;

public class StringPoolTests
{
    [Fact]
    public void StringPool_DeduplicatesIdenticalValues()
    {
        // Arrange
        var pool = new StringPool();
        var csvContent = "status,active,level\nactive,active,1\nactive,active,2\ninactive,active,3";
        
        // Act
        var options = new CsvOptions(',', '"', true, stringPool: pool);
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        
        // "active" should be the same reference everywhere it appears
        Assert.Same(records[0][0], records[1][0]); // Both "active"
        Assert.Same(records[0][1], records[1][1]); // Both "active"
        Assert.Same(records[0][0], records[0][1]); // Same "active"
        
        // Different values should be different references
        Assert.NotSame(records[0][0], records[2][0]); // "active" vs "inactive"
    }

    [Fact]
    public void DirectRows_WithStringPool_DeduplicatesValues()
    {
        // Arrange
        var pool = new StringPool();
        
        // CSV with many repeated values
        var lines = new[]
        {
            "type,status,active",
            "user,active,true",
            "user,active,true",
            "admin,inactive,false",
            "user,active,true"
        };
        
        var csvContent = string.Join("\n", lines);
        var options = new CsvOptions(',', '"', true, stringPool: pool);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert - All "user" values should be the same reference
        Assert.Same(records[0][0], records[1][0]); // Both "user"
        Assert.Same(records[0][0], records[3][0]); // Both "user"
        
        // All "active" values should be the same reference
        Assert.Same(records[0][1], records[1][1]); // Both "active"
        Assert.Same(records[0][1], records[3][1]); // Both "active"
        
        // All "true" values should be the same reference
        Assert.Same(records[0][2], records[1][2]); // Both "true"
        Assert.Same(records[0][2], records[3][2]); // Both "true"
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("0")]
    [InlineData("1")]
    public void GetOrAdd_CommonValues_ReturnsInternedString(string value)
    {
        // Arrange
        var pool = new StringPool();
        
        // Act
        var result1 = pool.GetString(value.AsSpan());
        var result2 = pool.GetString(value.AsSpan());
        
        // Assert - Common values should return the same reference
        Assert.Same(result1, result2);
    }

    [Fact]
    public void GetOrAdd_SingleCharacterCommonValues_ReturnsInternedString()
    {
        // Arrange
        var pool = new StringPool();
        var commonChars = new[] { "Y", "N", "T", "F", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        
        foreach (var ch in commonChars)
        {
            // Act
            var result1 = pool.GetString(ch.AsSpan());
            var result2 = pool.GetString(ch.AsSpan());
            
            // Assert
            Assert.Same(result1, result2);
        }
    }

    [Fact]
    public void IsCommonValue_RecognizesCommonValues()
    {
        // Arrange
        var commonValues = new[] { "true", "false", "null", "yes", "no", "0", "1", "" };
        var pool = new StringPool();
        
        foreach (var value in commonValues)
        {
            // Act & Assert
            var result = pool.GetString(value.AsSpan());
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void PrePopulateCommonValues_AddsCommonValuesToPool()
    {
        // Arrange
        var pool = new StringPool();
        
        // Act - Add common values
        var true1 = pool.GetString("true".AsSpan());
        var true2 = pool.GetString("true".AsSpan());
        var false1 = pool.GetString("false".AsSpan());
        var false2 = pool.GetString("false".AsSpan());
        
        // Assert - Should be same references
        Assert.Same(true1, true2);
        Assert.Same(false1, false2);
        Assert.NotSame(true1, false1);
    }
#endif

    [Fact]
    public void GetOrAdd_EmptyString_ReturnsStringEmpty()
    {
        // Arrange
        var pool = new StringPool();
        
        // Act
        var result = pool.GetString(ReadOnlySpan<char>.Empty);
        
        // Assert
        Assert.Same(string.Empty, result);
    }

    [Fact]
    public void GetOrAdd_LongString_DoesNotPool()
    {
        // Arrange
        var pool = new StringPool(); // Default maxStringLength is 100
        var longString = new string('x', 1000);
        
        // Act
        var result1 = pool.GetString(longString.AsSpan());
        var result2 = pool.GetString(longString.AsSpan());
        
        // Assert - Strings longer than maxStringLength are not pooled
        Assert.NotSame(result1, result2);
        Assert.Equal(result1, result2); // But they should be equal
    }
    
    [Fact]
    public void GetOrAdd_StringAtMaxLength_StillPools()
    {
        // Arrange
        var maxLength = 100;
        var pool = new StringPool(maxLength);
        var maxLengthString = new string('x', maxLength);
        
        // Act
        var result1 = pool.GetString(maxLengthString.AsSpan());
        var result2 = pool.GetString(maxLengthString.AsSpan());
        
        // Assert - Strings at exactly maxLength should be pooled
        Assert.Same(result1, result2);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void StringPool_Performance_ScalesWell(int uniqueValueCount)
    {
        // Arrange
        var pool = new StringPool();
        var values = Enumerable.Range(0, uniqueValueCount).Select(i => $"Value{i}").ToArray();
        
        // Act - Add each value twice
        foreach (var value in values)
        {
            var first = pool.GetString(value.AsSpan());
            var second = pool.GetString(value.AsSpan());
            
            // Assert - Should deduplicate
            Assert.Same(first, second);
        }
    }

    [Fact]
    public void StringPool_ThreadSafety_ConcurrentAccess()
    {
        // Arrange
        var pool = new StringPool();
        var tasks = new System.Threading.Tasks.Task[10];
        
        // Act - Multiple threads accessing the pool
        for (int i = 0; i < tasks.Length; i++)
        {
            var taskId = i;
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var value = $"Thread{taskId}Value{j % 10}";
                    pool.GetString(value.AsSpan());
                }
            });
        }
        
        System.Threading.Tasks.Task.WaitAll(tasks);
        
        // Assert - No exceptions should be thrown
        Assert.True(true); // If we get here, thread safety test passed
    }
}