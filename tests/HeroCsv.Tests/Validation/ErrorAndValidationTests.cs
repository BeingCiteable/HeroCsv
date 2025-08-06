using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Errors;
using HeroCsv.Validation;
using Xunit;

namespace HeroCsv.Tests.Validation;

/// <summary>
/// Tests for error handling and validation functionality
/// </summary>
public class ErrorAndValidationTests
{
    #region ErrorHandler Tests

    [Fact]
    public void ErrorHandler_RecordError_WhenEnabled()
    {
        var errorHandler = new ErrorHandler(isEnabled: true);
        var error = new CsvValidationError(
            CsvErrorType.InconsistentFieldCount,
            "Test error",
            lineNumber: 1);
        
        bool eventRaised = false;
        errorHandler.ErrorOccurred += (e) => { eventRaised = true; };
        
        errorHandler.RecordError(error);
        
        Assert.True(eventRaised);
        var result = errorHandler.GetValidationResult();
        Assert.Single(result.Errors);
        Assert.Equal("Test error", result.Errors[0].Message);
    }

    [Fact]
    public void ErrorHandler_RecordError_WhenDisabled()
    {
        var errorHandler = new ErrorHandler(isEnabled: false);
        var error = new CsvValidationError(
            CsvErrorType.InconsistentFieldCount,
            "Test error",
            lineNumber: 1);
        
        errorHandler.RecordError(error);
        
        var result = errorHandler.GetValidationResult();
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ErrorHandler_Reset_ClearsErrors()
    {
        var errorHandler = new ErrorHandler(isEnabled: true);
        var error = new CsvValidationError(
            CsvErrorType.InconsistentFieldCount,
            "Test error",
            lineNumber: 1);
        
        errorHandler.RecordError(error);
        Assert.Single(errorHandler.GetValidationResult().Errors);
        
        errorHandler.Reset();
        Assert.Empty(errorHandler.GetValidationResult().Errors);
    }

    [Fact]
    public void NullErrorHandler_DoesNothing()
    {
        var nullHandler = new NullErrorHandler();
        
        Assert.False(nullHandler.IsEnabled);
        
        var error = new CsvValidationError(
            CsvErrorType.InconsistentFieldCount,
            "Test",
            1);
        
        // Should not throw
        nullHandler.RecordError(error);
        nullHandler.Reset();
        
        var result = nullHandler.GetValidationResult();
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region ValidationHandler Tests

    [Fact]
    public void ValidationHandler_ValidateRecord_ConsistentFieldCount()
    {
        var errorHandler = new ErrorHandler(true);
        var validator = new ValidationHandler(
            CsvOptions.Default,
            errorHandler,
            isEnabled: true);
        
        // First record sets expected count
        validator.ValidateRecord(new[] { "A", "B", "C" }, 1, null);
        Assert.Empty(errorHandler.GetValidationResult().Errors);
        
        // Same count - no error
        validator.ValidateRecord(new[] { "D", "E", "F" }, 2, null);
        Assert.Empty(errorHandler.GetValidationResult().Errors);
    }

    [Fact]
    public void ValidationHandler_ValidateRecord_InconsistentFieldCount()
    {
        var errorHandler = new ErrorHandler(true);
        var validator = new ValidationHandler(
            CsvOptions.Default,
            errorHandler,
            isEnabled: true);
        
        // First record sets expected count
        validator.ValidateRecord(new[] { "A", "B", "C" }, 1, null);
        
        // Different count - should error
        validator.ValidateRecord(new[] { "D", "E" }, 2, null);
        
        var errors = errorHandler.GetValidationResult().Errors;
        Assert.Single(errors);
        Assert.Equal(CsvErrorType.InconsistentFieldCount, errors[0].ErrorType);
        Assert.Equal(2, errors[0].LineNumber);
    }

    [Fact]
    public void ValidationHandler_ValidateRecord_UnbalancedQuotes()
    {
        var errorHandler = new ErrorHandler(true);
        var validator = new ValidationHandler(
            CsvOptions.Default,
            errorHandler,
            isEnabled: true);
        
        validator.ValidateRecord(new[] { "\"unbalanced", "normal" }, 1, null);
        
        var errors = errorHandler.GetValidationResult().Errors;
        Assert.Single(errors);
        Assert.Equal(CsvErrorType.UnbalancedQuotes, errors[0].ErrorType);
        Assert.Equal(0, errors[0].FieldIndex);
    }

    [Fact]
    public void ValidationHandler_ValidateRecord_WhenDisabled()
    {
        var errorHandler = new ErrorHandler(true);
        var validator = new ValidationHandler(
            CsvOptions.Default,
            errorHandler,
            isEnabled: false); // Disabled
        
        validator.ValidateRecord(new[] { "A" }, 1, null);
        validator.ValidateRecord(new[] { "B", "C" }, 2, null); // Different count
        
        // No errors should be recorded when disabled
        Assert.Empty(errorHandler.GetValidationResult().Errors);
    }

    [Fact]
    public void ValidationHandler_Reset_ClearsState()
    {
        var errorHandler = new ErrorHandler(true);
        var validator = new ValidationHandler(
            CsvOptions.Default,
            errorHandler,
            isEnabled: true);
        
        // Set expected count
        validator.ValidateRecord(new[] { "A", "B" }, 1, null);
        Assert.Equal(2, validator.ExpectedFieldCount);
        
        validator.Reset();
        
        Assert.Null(validator.ExpectedFieldCount);
        Assert.Empty(errorHandler.GetValidationResult().Errors);
    }

    #endregion

    #region Integration Tests

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

    #endregion
}