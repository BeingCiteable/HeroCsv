using System;
using FastCsv.Parsing;
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Tests boundary conditions and special cases for CsvFieldEnumerator
/// </summary>
public class CsvFieldEnumeratorBoundaryTests
{
    [Fact]
    public void CsvFieldEnumerator_TryGetNextField_NoMoreFields()
    {
        var enumerator = new CsvFieldEnumerator("A,B".AsSpan(), ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out _));
        Assert.True(enumerator.TryGetNextField(out _));
        Assert.False(enumerator.TryGetNextField(out var field));
        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void CsvFieldEnumerator_QuotedFields_Basic()
    {
        var enumerator = new CsvFieldEnumerator("\"A\",\"B\",\"C\"".AsSpan(), ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("\"A\"", field.ToString());
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("\"B\"", field.ToString());
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("\"C\"", field.ToString());
    }

    [Fact]
    public void CsvFieldEnumerator_QuotedFields_WithEscapedQuotes()
    {
        // This tests the quoted field path with escaped quotes
        var line = "\"field with \"\"escaped\"\" quotes\",normal".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        // First field should be the quoted field
        Assert.True(enumerator.TryGetNextField(out var field));
        // The field includes the quotes
        Assert.Equal("\"field with \"\"escaped\"\" quotes\"", field.ToString());
        
        // Second field
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("normal", field.ToString());
    }

    [Fact]
    public void CsvFieldEnumerator_QuotedFields_Unterminated()
    {
        // Test unterminated quoted field
        var enumerator = new CsvFieldEnumerator("\"unterminated".AsSpan(), ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("\"unterminated", field.ToString());
        
        Assert.False(enumerator.TryGetNextField(out _));
    }

    [Fact]
    public void CsvFieldEnumerator_GetFieldByIndex()
    {
        var line = "A,B,C,D,E".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        Assert.Equal("A", enumerator.GetFieldByIndex(0).ToString());
        Assert.Equal("B", enumerator.GetFieldByIndex(1).ToString());
        Assert.Equal("C", enumerator.GetFieldByIndex(2).ToString());
        Assert.Equal("D", enumerator.GetFieldByIndex(3).ToString());
        Assert.Equal("E", enumerator.GetFieldByIndex(4).ToString());
        
        // Out of range
        Assert.True(enumerator.GetFieldByIndex(5).IsEmpty);
    }

    [Fact]
    public void CsvFieldEnumerator_CountTotalFields()
    {
        var enumerator = new CsvFieldEnumerator("A,B,C,D,E".AsSpan(), ',', '"');
        Assert.Equal(5, enumerator.CountTotalFields());
        
        enumerator = new CsvFieldEnumerator("".AsSpan(), ',', '"');
        Assert.Equal(0, enumerator.CountTotalFields());
        
        enumerator = new CsvFieldEnumerator("single".AsSpan(), ',', '"');
        Assert.Equal(1, enumerator.CountTotalFields());
        
        enumerator = new CsvFieldEnumerator("A,B,".AsSpan(), ',', '"');
        Assert.Equal(2, enumerator.CountTotalFields()); // CsvFieldEnumerator doesn't count trailing empty field
    }

    [Fact]
    public void CsvFieldEnumerator_MixedQuotedUnquoted()
    {
        var enumerator = new CsvFieldEnumerator("normal,\"quoted\",normal2".AsSpan(), ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("normal", field.ToString());
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("\"quoted\"", field.ToString()); // CsvFieldEnumerator returns fields with quotes
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("normal2", field.ToString());
    }
}