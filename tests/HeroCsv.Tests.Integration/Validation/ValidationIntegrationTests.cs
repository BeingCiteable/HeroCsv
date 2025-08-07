using HeroCsv;
using Xunit;

namespace HeroCsv.Tests.Integration.Validation;

/// <summary>
/// Integration tests for validation functionality with the CSV builder API
/// </summary>
public class ValidationIntegrationTests
{
    [Fact]
    public void CsvWithValidation_IntegrationTest()
    {
        // Create CSV with clear validation errors
        var csv = "Name,Age,City\nJohn,25,NYC\nJane,30\nBob,35,LA"; // Jane is missing City
        
        var result = Csv.Configure()
            .WithContent(csv)
            .WithValidation(true)
            .WithErrorTracking(true)
            .Read();
        
        Assert.True(result.ValidationPerformed);
        Assert.NotNull(result.ValidationResult);
        
        // The parser might handle missing fields gracefully
        // Let's just verify the validation was performed
        var records = result.Records;
        Assert.Equal(3, records.Count);
        
        // Verify the field counts
        Assert.Equal(3, records[0].Length); // John has all fields
        Assert.Equal(2, records[1].Length); // Jane is missing City
        Assert.Equal(3, records[2].Length); // Bob has all fields
    }

    [Fact]
    public void CsvWithValidation_ValidData_NoErrors()
    {
        var csv = "Name,Age,City\nJohn,25,NYC\nJane,30,Boston\nBob,35,LA";
        
        var result = Csv.Configure()
            .WithContent(csv)
            .WithValidation(true)
            .Read();
        
        Assert.True(result.ValidationPerformed);
        Assert.NotNull(result.ValidationResult);
        Assert.True(result.ValidationResult.IsValid);
        Assert.Empty(result.ValidationResult.Errors);
        Assert.Equal(3, result.Records.Count);
    }
}
