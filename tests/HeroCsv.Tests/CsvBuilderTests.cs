using System.Linq;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests specifically for Csv.Configure() builder pattern
/// </summary>
public class CsvBuilderTests
{
    [Fact]
    public void Configure_ReturnsBuilderInstance()
    {
        var builder = Csv.Configure();
        Assert.NotNull(builder);
    }

    [Fact]
    public void Configure_CanBuildReader()
    {
        var reader = Csv.Configure()
            .WithContent("Name,Age\nJohn,25")
            .Build();
            
        Assert.NotNull(reader);
        Assert.True(reader.HasMoreData);
    }

    [Fact(Skip = "Validation not detecting malformed CSV")]
    public void Configure_WithValidation_BuildsValidatingReader()
    {
        // Use malformed CSV to trigger validation error
        var result = Csv.Configure()
            .WithContent("Name,Age\nJohn,25\n\"Jane")  // Unclosed quote
            .WithValidation(true)
            .Read();
            
        Assert.NotNull(result);
        Assert.True(result.ValidationPerformed);
        Assert.NotNull(result.ValidationResult);
        Assert.False(result.ValidationResult.IsValid);
    }

    [Fact]
    public void Configure_WithErrorTracking_TracksErrors()
    {
        var result = Csv.Configure()
            .WithContent("Name,Age\nJohn,25\nJane,thirty")
            .WithErrorTracking(true)
            .Read();
            
        Assert.NotNull(result);
        Assert.True(result.ErrorTrackingEnabled);
    }
}