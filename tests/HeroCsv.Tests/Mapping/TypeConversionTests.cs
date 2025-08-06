using System;
using System.Collections.Generic;
using System.Linq;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Mapping;
using HeroCsv.Models;
using HeroCsv.Parsing;
using Xunit;

namespace HeroCsv.Tests.Mapping;

public class TypeConversionTests
{
    #region CsvFieldEnumerator Deep Coverage

    [Fact]
    public void CsvFieldEnumerator_QuotedFieldPath_StartOfLine()
    {
        // Test quoted field at position 0
        var line = "\"quoted\",normal".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("\"quoted\"", field.ToString());
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("normal", field.ToString());
    }

    [Fact]
    public void CsvFieldEnumerator_ComplexQuotedFields()
    {
        // Test the quoted field path with escaped quotes
        var line = "normal,\"has \"\"quotes\"\" inside\",end".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        // First field - normal
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("normal", field.ToString());
        
        // Second field - quoted with escaped quotes
        Assert.True(enumerator.TryGetNextField(out field));
        // The field should include the quotes
        Assert.Contains("\"", field.ToString());
        
        // Third field
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("end", field.ToString());
    }

    [Fact]
    public void CsvFieldEnumerator_QuotedFieldAtEnd()
    {
        var line = "start,\"quoted\"".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        Assert.True(enumerator.TryGetNextField(out var field));
        Assert.Equal("start", field.ToString());
        
        Assert.True(enumerator.TryGetNextField(out field));
        Assert.Equal("\"quoted\"", field.ToString());
        
        Assert.False(enumerator.TryGetNextField(out _));
    }

    [Fact]
    public void CsvFieldEnumerator_EmptyQuotedFields()
    {
        var line = "\"\",\"\",\"\"".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        for (int i = 0; i < 3; i++)
        {
            Assert.True(enumerator.TryGetNextField(out var field));
            Assert.Equal("\"\"", field.ToString());
        }
    }

    [Fact]
    public void CsvFieldEnumerator_GetFieldByIndex_BeyondRange()
    {
        var line = "A,B,C".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        // Beyond range
        var field = enumerator.GetFieldByIndex(10);
        Assert.True(field.IsEmpty);
        
        // Negative index
        field = enumerator.GetFieldByIndex(-1);
        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void CsvFieldEnumerator_CountFields_WithQuotes()
    {
        var line = "\"A,B\",\"C\",D,\"E,F,G\"".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        Assert.Equal(4, enumerator.CountTotalFields());
    }

    [Fact]
    public void CsvFieldEnumerator_SingleCharacterFields()
    {
        var line = "A,B,C,D,E,F,G".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(7, fields.Count);
        Assert.All(fields, f => Assert.Single(f));
    }

    #endregion

    #region CsvRow Deep Coverage

    [Fact]
    public void CsvRow_IndexerAccess_AllPaths()
    {
        var csv = "A,B,C,D,E,F,G,H,I,J";
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), new CsvOptions(hasHeader: false));
        
        foreach (var row in rows)
        {
            // Test multiple index accesses
            for (int i = 0; i < row.FieldCount; i++)
            {
                var field = row[i];
                Assert.False(field.IsEmpty);
                Assert.Equal(1, field.Length);
            }
            
            // Test out of bounds
            try
            {
                var _ = row[-1];
                Assert.Fail("Should throw");
            }
            catch (ArgumentOutOfRangeException) { }
            
            try
            {
                var _ = row[row.FieldCount];
                Assert.Fail("Should throw");
            }
            catch (ArgumentOutOfRangeException) { }
        }
    }

    [Fact]
    public void CsvRow_GetFieldEnumerator_CompleteIteration()
    {
        var csv = string.Join(",", Enumerable.Range(1, 20).Select(i => $"Field{i}"));
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), new CsvOptions(hasHeader: false));
        
        foreach (var row in rows)
        {
            var enumerator = row.GetFieldEnumerator();
            var count = 0;
            var fields = new List<string>();
            
            while (enumerator.TryGetNextField(out var field))
            {
                fields.Add(field.ToString());
                count++;
            }
            
            Assert.Equal(20, count);
            Assert.Equal(20, fields.Count);
            
            // Verify field values
            for (int i = 0; i < fields.Count; i++)
            {
                Assert.Equal($"Field{i + 1}", fields[i]);
            }
        }
    }

    [Fact]
    public void CsvRow_EmptyRow()
    {
        // Test completely empty row
        var csv = "";
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), new CsvOptions(hasHeader: false));
        
        var rowCount = 0;
        foreach (var row in rows)
        {
            rowCount++;
        }
        
        Assert.Equal(0, rowCount);
    }

    #endregion

    #region CsvMapper Deep Coverage

    [Fact]
    public void CsvMapper_ComplexTypeConversions()
    {
        var mapper = new CsvMapper<ComplexModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "GuidValue", "DateTimeOffsetValue", "DecimalValue", "DoubleValue" });
        
        var guid = Guid.NewGuid();
        var dto = DateTimeOffset.Now;
        var record = new[] { guid.ToString(), dto.ToString("O"), "123.456", "789.012" };
        
        var model = mapper.MapRecord(record);
        
        Assert.Equal(guid, model.GuidValue);
        Assert.Equal(dto.ToString("O"), model.DateTimeOffsetValue.ToString("O"));
        Assert.Equal(123.456m, model.DecimalValue);
        Assert.Equal(789.012, model.DoubleValue);
    }

    [Fact]
    public void CsvMapper_NullableTypeConversions()
    {
        var mapper = new CsvMapper<ComplexModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { 
            "NullableInt", "NullableGuid", "NullableDateTime", 
            "NullableBool", "NullableDecimal", "NullableDouble" 
        });
        
        // Test with values
        var record = new[] { "42", Guid.NewGuid().ToString(), "2025-01-01", "true", "123.45", "678.90" };
        var model = mapper.MapRecord(record);
        
        Assert.NotNull(model.NullableInt);
        Assert.NotNull(model.NullableGuid);
        Assert.NotNull(model.NullableDateTime);
        Assert.NotNull(model.NullableBool);
        Assert.NotNull(model.NullableDecimal);
        Assert.NotNull(model.NullableDouble);
        
        // Test with empty values
        record = new[] { "", "", "", "", "", "" };
        model = mapper.MapRecord(record);
        
        Assert.Null(model.NullableInt);
        Assert.Null(model.NullableGuid);
        Assert.Null(model.NullableDateTime);
        Assert.Null(model.NullableBool);
        Assert.Null(model.NullableDecimal);
        Assert.Null(model.NullableDouble);
    }

    [Fact]
    public void CsvMapper_PropertyNotInHeaders()
    {
        var mapper = new CsvMapper<ComplexModel>(CsvOptions.Default);
        // Set headers that don't match all properties
        mapper.SetHeaders(new[] { "NonExistentProperty", "AnotherMissing" });
        
        var record = new[] { "Value1", "Value2" };
        var model = mapper.MapRecord(record);
        
        // All properties should have default values
        Assert.Equal(Guid.Empty, model.GuidValue);
        Assert.Equal(0, model.IntValue);
        Assert.Null(model.StringValue);
    }

    [Fact]
    public void CsvMapper_RecordShorterThanHeaders()
    {
        var mapper = new CsvMapper<ComplexModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "IntValue", "StringValue", "DecimalValue", "BoolValue" });
        
        // Record has fewer fields than headers
        var record = new[] { "42", "Test" };
        var model = mapper.MapRecord(record);
        
        Assert.Equal(42, model.IntValue);
        Assert.Equal("Test", model.StringValue);
        Assert.Equal(0m, model.DecimalValue); // Default
        Assert.False(model.BoolValue); // Default
    }

    [Fact]
    public void CsvMapper_CaseInsensitivePropertyMatching()
    {
        var mapper = new CsvMapper<ComplexModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "INTVALUE", "stringvalue", "BoolValue" });
        
        var record = new[] { "99", "CaseTest", "true" };
        var model = mapper.MapRecord(record);
        
        Assert.Equal(99, model.IntValue);
        Assert.Equal("CaseTest", model.StringValue);
        Assert.True(model.BoolValue);
    }

    public class ComplexModel
    {
        public Guid GuidValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public int IntValue { get; set; }
        public string? StringValue { get; set; }
        public bool BoolValue { get; set; }
        
        // Nullable types
        public int? NullableInt { get; set; }
        public Guid? NullableGuid { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public bool? NullableBool { get; set; }
        public decimal? NullableDecimal { get; set; }
        public double? NullableDouble { get; set; }
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public void CsvParser_ParseLine_OnlyDelimiters()
    {
        // Line with only delimiters
        var line = ",,,,,".AsSpan();
        var fields = CsvParser.ParseLine(line, CsvOptions.Default);
        
        Assert.Equal(6, fields.Length);
        Assert.All(fields, f => Assert.Equal("", f));
    }

    [Fact]
    public void CsvParser_ParseWholeBuffer_WithEmptyLines()
    {
        var buffer = "A,B\n\nC,D\n\n\nE,F".AsSpan();
        var rows = CsvParser.ParseWholeBuffer(buffer, new CsvOptions(hasHeader: false));
        
        var rowCount = 0;
        var nonEmptyRows = new List<string[]>();
        
        foreach (var row in rows)
        {
            rowCount++;
            if (row.FieldCount > 0)
            {
                var fields = new string[row.FieldCount];
                for (int i = 0; i < row.FieldCount; i++)
                {
                    fields[i] = row[i].ToString();
                }
                if (fields.Any(f => !string.IsNullOrEmpty(f)))
                {
                    nonEmptyRows.Add(fields);
                }
            }
        }
        
        Assert.Equal(3, nonEmptyRows.Count);
    }

    #endregion
}