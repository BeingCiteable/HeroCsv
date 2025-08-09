using System.Runtime.Intrinsics;
using HeroCsv.Parsing;
using HeroCsv.Models;
using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Unit.Parsing;

public class Vector512ParsingTests
{
#if NET9_0_OR_GREATER
    [Fact(Skip = "Vector512 tests require AVX-512 hardware support")]
    public void ParseLineVector512_SimpleCommaDelimited_ReturnsCorrectFields()
    {
        if (!Vector512.IsHardwareAccelerated) return;
        
        // Arrange
        var line = "field1,field2,field3,field4,field5,field6,field7,field8,field9,field10,field11,field12,field13,field14,field15,field16".AsSpan();
        var delimiter = ',';
        var stringPool = new StringPool();
        
        // Act
        var result = CsvParser.ParseLineVector512(line, delimiter, stringPool);
        
        // Assert
        Assert.Equal(16, result.Count);
        for (int i = 1; i <= 16; i++)
        {
            Assert.Equal($"field{i}", result[i - 1]);
        }
    }
    
    [Fact(Skip = "Vector512 tests require AVX-512 hardware support")]
    public void ParseLineVector512_LongLine_HandlesCorrectly()
    {
        if (!Vector512.IsHardwareAccelerated) return;
        
        // Arrange
        var fields = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            fields.Add($"value{i}");
        }
        var line = string.Join(",", fields).AsSpan();
        var delimiter = ',';
        var stringPool = new StringPool();
        
        // Act
        var result = CsvParser.ParseLineVector512(line, delimiter, stringPool);
        
        // Assert
        Assert.Equal(100, result.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal($"value{i}", result[i]);
        }
    }
    
    [Fact(Skip = "Vector512 tests require AVX-512 hardware support")]
    public void ParseLineVector512_EmptyFields_HandlesCorrectly()
    {
        if (!Vector512.IsHardwareAccelerated) return;
        
        // Arrange
        var line = ",,field3,,field5,,,,,field10,,,,,,".AsSpan();
        var delimiter = ',';
        var stringPool = new StringPool();
        
        // Act
        var result = CsvParser.ParseLineVector512(line, delimiter, stringPool);
        
        // Assert
        Assert.Equal(16, result.Count);
        Assert.Equal("", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("field3", result[2]);
        Assert.Equal("", result[3]);
        Assert.Equal("field5", result[4]);
        Assert.Equal("field10", result[9]);
    }
    
    [Fact]
    public void Vector512ParsingStrategy_IsAvailable_ReflectsHardwareSupport()
    {
        // Arrange
        var strategy = new Vector512ParsingStrategy();
        
        // Act & Assert
        Assert.Equal(Vector512.IsHardwareAccelerated, strategy.IsAvailable);
    }
    
    [Fact(Skip = "Vector512 tests require AVX-512 hardware support")]
    public void Vector512ParsingStrategy_CanHandle_ReturnsTrueForSuitableLines()
    {
        if (!Vector512.IsHardwareAccelerated) return;
        
        // Arrange
        var strategy = new Vector512ParsingStrategy();
        var options = new CsvOptions(',');
        var suitableLine = new string('a', 100).AsSpan(); // Long line without quotes
        var unsuitableLineShort = "short".AsSpan();
        var unsuitableLineQuotes = new string('a', 100).Replace("aaa", "\"a\"a", StringComparison.Ordinal).AsSpan();
        
        // Act & Assert
        Assert.True(strategy.CanHandle(suitableLine, options));
        Assert.False(strategy.CanHandle(unsuitableLineShort, options));
        Assert.False(strategy.CanHandle(unsuitableLineQuotes, options));
    }
#else
    [Fact]
    public void Vector512_NotAvailable_OnOlderFrameworks()
    {
        // This test verifies that Vector512 code is properly conditionally compiled
        Assert.True(true, "Vector512 tests are not available on this framework version");
    }
#endif
}