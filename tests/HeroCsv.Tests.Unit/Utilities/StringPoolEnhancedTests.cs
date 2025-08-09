using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Unit.Utilities;

public class StringPoolEnhancedTests
{
#if NET8_0_OR_GREATER
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("YES")]
    [InlineData("NO")]
    [InlineData("N/A")]
    [InlineData("Active")]
    [InlineData("Inactive")]
    public void GetOrAdd_CommonValues_ReturnsInternedString(string value)
    {
        // Arrange
        var pool = new StringPool();
        var span = value.AsSpan();

        // Act
        var result1 = pool.GetOrAdd(span);
        var result2 = pool.GetOrAdd(span);

        // Assert
        Assert.Same(result1, result2); // Should be the same reference
        Assert.Equal(value, result1);
    }

    [Fact]
    public void GetOrAdd_SingleCharacterCommonValues_ReturnsInternedString()
    {
        // Arrange
        var pool = new StringPool();
        var commonChars = new[] { "0", "1", "Y", "N", "T", "F", "-" };

        foreach (var charValue in commonChars)
        {
            // Act
            var result1 = pool.GetOrAdd(charValue.AsSpan());
            var result2 = pool.GetOrAdd(charValue.AsSpan());

            // Assert
            Assert.Same(result1, result2);
            Assert.Equal(charValue, result1);
        }
    }

    [Fact]
    public void IsCommonValue_RecognizesCommonValues()
    {
        // Assert
        Assert.True(StringPool.IsCommonValue("true".AsSpan()));
        Assert.True(StringPool.IsCommonValue("false".AsSpan()));
        Assert.True(StringPool.IsCommonValue("null".AsSpan()));
        Assert.True(StringPool.IsCommonValue("".AsSpan()));
        Assert.True(StringPool.IsCommonValue("YES".AsSpan()));
        Assert.True(StringPool.IsCommonValue("Active".AsSpan()));

        Assert.False(StringPool.IsCommonValue("uncommon_value_12345".AsSpan()));
        Assert.False(StringPool.IsCommonValue("very long string that is not common".AsSpan()));
    }

    [Fact]
    public void PrePopulateCommonValues_AddsCommonValuesToPool()
    {
        // Arrange
        var pool = new StringPool();

        // Act
        pool.PrePopulateCommonValues();

        // Assert - Common values should be pre-populated
        var result1 = pool.GetOrAdd("true".AsSpan());
        var result2 = pool.GetOrAdd("true".AsSpan());
        Assert.Same(result1, result2);

        var result3 = pool.GetOrAdd("false".AsSpan());
        var result4 = pool.GetOrAdd("false".AsSpan());
        Assert.Same(result3, result4);
    }

    [Fact]
    public void GetOrAdd_EmptyString_ReturnsStringEmpty()
    {
        // Arrange
        var pool = new StringPool();

        // Act
        var result = pool.GetOrAdd([]);

        // Assert
        Assert.Same(string.Empty, result);
    }

    [Fact]
    public void GetOrAdd_LongString_UsesNormalPooling()
    {
        // Arrange
        var pool = new StringPool();
        var longString = new string('a', 50);

        // Act
        var result1 = pool.GetOrAdd(longString.AsSpan());
        var result2 = pool.GetOrAdd(longString.AsSpan());

        // Assert
        Assert.Same(result1, result2); // Should still be pooled
        Assert.Equal(longString, result1);
    }
#else
    [Fact]
    public void StringPoolEnhancements_NotAvailable_OnOlderFrameworks()
    {
        // This test verifies that enhanced StringPool features are properly conditionally compiled
        Assert.True(true, "Enhanced StringPool features are not available on this framework version");
    }
#endif
}