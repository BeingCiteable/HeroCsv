using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastCsv;
using FastCsv.Core;
using FastCsv.DataSources;
using FastCsv.Mapping;
using FastCsv.Models;
using FastCsv.Parsing;
using Xunit;

namespace FastCsv.Tests;

public class EdgeCaseTests
{
    #region CsvRecord Coverage Boost

    [Fact]
    public void CsvRecord_GetField_ThrowsOnInvalidIndex()
    {
        using var reader = new FastCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => record.GetField(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => record.GetField(3));
    }

    [Fact]
    public void CsvRecord_TryGetField_Success()
    {
        using var reader = new FastCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.True(record.TryGetField(0, out var field));
        Assert.Equal("A", field);
        
        Assert.True(record.TryGetField(1, out field));
        Assert.Equal("B", field);
        
        Assert.True(record.TryGetField(2, out field));
        Assert.Equal("C", field);
    }

    [Fact]
    public void CsvRecord_TryGetField_Failure()
    {
        using var reader = new FastCsvReader("A,B", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.False(record.TryGetField(-1, out var field));
        Assert.True(field.IsEmpty);
        
        Assert.False(record.TryGetField(2, out field));
        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void CsvRecord_IsValidIndex()
    {
        using var reader = new FastCsvReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        Assert.False(record.IsValidIndex(-1));
        Assert.True(record.IsValidIndex(0));
        Assert.True(record.IsValidIndex(1));
        Assert.True(record.IsValidIndex(2));
        Assert.False(record.IsValidIndex(3));
    }

    [Fact]
    public void CsvRecord_GetAllFields()
    {
        using var reader = new FastCsvReader("A,B,C,D,E", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        // Test with exact size
        var fields = new string[5];
        var count = record.GetAllFields(fields);
        Assert.Equal(5, count);
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, fields);
        
        // Test with smaller destination
        fields = new string[3];
        count = record.GetAllFields(fields);
        Assert.Equal(3, count);
        Assert.Equal(new[] { "A", "B", "C" }, fields);
        
        // Test with larger destination
        fields = new string[10];
        count = record.GetAllFields(fields);
        Assert.Equal(5, count);
        Assert.Equal("A", fields[0]);
        Assert.Equal("E", fields[4]);
        Assert.Null(fields[5]);
    }

    #endregion

    #region CsvFieldEnumerator Coverage Boost

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

    #endregion

    #region CsvMapper Coverage Boost

    [Fact]
    public void CsvMapper_ConvertValue_AllTypes()
    {
        var mapper = new CsvMapper<TestModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "ByteValue", "ShortValue", "UShortValue", "UIntValue", "ULongValue", "FloatValue", "CharValue", "TimeSpanValue" });
        
        var record = new[] { "255", "32767", "65535", "4294967295", "18446744073709551615", "3.14", "A", "1.02:03:04" };
        var model = mapper.MapRecord(record);
        
        Assert.Equal(255, model.ByteValue);
        Assert.Equal(32767, model.ShortValue);
        Assert.Equal(65535, model.UShortValue);
        Assert.Equal(4294967295U, model.UIntValue);
        Assert.Equal(18446744073709551615UL, model.ULongValue);
        Assert.Equal(3.14f, model.FloatValue);
        Assert.Equal('A', model.CharValue);
        Assert.Equal(new TimeSpan(1, 2, 3, 4), model.TimeSpanValue);
    }

    [Fact]
    public void CsvMapper_ConvertValue_NullableTypes()
    {
        var mapper = new CsvMapper<TestModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "NullableInt", "NullableDateTime", "NullableGuid", "NullableBool" });
        
        // Test with values
        var record = new[] { "42", "2025-01-01", "12345678-1234-1234-1234-123456789012", "true" };
        var model = mapper.MapRecord(record);
        
        Assert.Equal(42, model.NullableInt);
        Assert.Equal(new DateTime(2025, 1, 1), model.NullableDateTime);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), model.NullableGuid);
        Assert.True(model.NullableBool);
        
        // Test with empty values
        record = new[] { "", "", "", "" };
        model = mapper.MapRecord(record);
        
        Assert.Null(model.NullableInt);
        Assert.Null(model.NullableDateTime);
        Assert.Null(model.NullableGuid);
        Assert.Null(model.NullableBool);
    }

    [Fact]
    public void CsvMapper_WithSkipEmptyFields()
    {
        var options = new CsvOptions(skipEmptyFields: true);
        var mapper = new CsvMapper<TestModel>(options);
        mapper.SetHeaders(new[] { "StringValue", "IntValue" });
        
        var record = new[] { "", "" };
        var model = mapper.MapRecord(record);
        
        // Empty fields should be skipped
        Assert.Null(model.StringValue);
        Assert.Equal(0, model.IntValue);
    }

    [Fact]
    public void CsvMapper_ManualMapping_MixedMode()
    {
        var mapping = CsvMapping<TestModel>.CreateAutoMapWithOverrides();
        mapping.MapProperty("IntValue", 1, v => int.Parse(v) * 100);
        
        var mapper = new CsvMapper<TestModel>(mapping);
        mapper.SetHeaders(new[] { "StringValue", "IntValue" });
        
        var record = new[] { "Test", "5" };
        var model = mapper.MapRecord(record);
        
        Assert.Equal("Test", model.StringValue); // Auto-mapped
        Assert.Equal(500, model.IntValue); // Manual with converter
    }

    public class TestModel
    {
        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public byte ByteValue { get; set; }
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public uint UIntValue { get; set; }
        public ulong ULongValue { get; set; }
        public float FloatValue { get; set; }
        public char CharValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public int? NullableInt { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public Guid? NullableGuid { get; set; }
        public bool? NullableBool { get; set; }
    }

    #endregion

    #region StringDataSource and MemoryDataSource Coverage Boost

    [Fact]
    public void StringDataSource_AllScenarios()
    {
        // Test counting lines with various endings
        using (var source = new StringDataSource("line1\r\nline2\nline3\rline4"))
        {
            var count = source.CountLinesDirectly();
            Assert.Equal(4, count); // 4 lines: line1, line2, line3, line4
        }
        
        // Test empty lines
        using (var source = new StringDataSource("\n\n\n"))
        {
            Assert.True(source.TryReadLine(out var line, out _));
            Assert.Equal("", line.ToString());
            
            Assert.True(source.TryReadLine(out line, out _));
            Assert.Equal("", line.ToString());
        }
        
        // Test line ending at buffer end
        using (var source = new StringDataSource("no newline at end"))
        {
            Assert.True(source.TryReadLine(out var line, out _));
            Assert.Equal("no newline at end", line.ToString());
            Assert.False(source.TryReadLine(out _, out _));
        }
    }

    [Fact]
    public void MemoryDataSource_AllScenarios()
    {
        // Test with different line endings
        var content = "line1\r\nline2\nline3".AsMemory();
        using (var source = new MemoryDataSource(content))
        {
            Assert.True(source.TryReadLine(out var line, out var lineNum));
            Assert.Equal("line1", line.ToString());
            Assert.Equal(1, lineNum);
            
            Assert.True(source.TryReadLine(out line, out lineNum));
            Assert.Equal("line2", line.ToString());
            Assert.Equal(2, lineNum);
        }
        
        // Test reset functionality
        using (var source = new MemoryDataSource("test".AsMemory()))
        {
            source.TryReadLine(out _, out _);
            Assert.False(source.HasMoreData);
            
            source.Reset();
            Assert.True(source.HasMoreData);
        }
    }

    #endregion

    #region CsvRow ParseWholeBuffer Coverage

    [Fact]
    public void CsvRow_ParseWholeBuffer_Comprehensive()
    {
        // Test with header - ParseWholeBuffer skips header when hasHeader: true
        var buffer = "Header1,Header2\nValue1,Value2\nValue3,Value4".AsSpan();
        var options = CsvOptions.Default; // hasHeader: true by default
        
        var rows = CsvParser.ParseWholeBuffer(buffer, options);
        var rowCount = 0;
        
        foreach (var row in rows)
        {
            rowCount++;
            if (rowCount == 1)
            {
                // First row should be Value1,Value2 (header is skipped)
                Assert.Equal("Value1", row[0].ToString());
                Assert.Equal("Value2", row[1].ToString());
            }
            else if (rowCount == 2)
            {
                Assert.Equal("Value3", row[0].ToString());
                Assert.Equal("Value4", row[1].ToString());
            }
            
            // Test field enumerator
            var enumerator = row.GetFieldEnumerator();
            var fieldCount = 0;
            while (enumerator.TryGetNextField(out _))
            {
                fieldCount++;
            }
            Assert.Equal(2, fieldCount);
        }
        
        Assert.Equal(2, rowCount); // 2 data rows (header is skipped)
    }

    [Fact]
    public void CsvRow_EmptyBuffer()
    {
        var buffer = "".AsSpan();
        var options = CsvOptions.Default;
        
        var rows = CsvParser.ParseWholeBuffer(buffer, options);
        var count = 0;
        
        foreach (var _ in rows)
        {
            count++;
        }
        
        Assert.Equal(0, count);
    }

    #endregion

    #region Additional FastCsvReader Coverage

    [Fact]
    public void FastCsvReader_ValidationResult()
    {
        var reader = new FastCsvReader("A,B\n1,2,3\n4,5", CsvOptions.Default, validateData: true, trackErrors: true);
        
        // Read records to trigger validation
        while (reader.TryReadRecord(out _)) { }
        
        var result = reader.ValidationResult;
        Assert.NotNull(result);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void FastCsvReader_SkipRecord()
    {
        using var reader = new FastCsvReader("Header\nRow1\nRow2\nRow3", CsvOptions.Default);
        
        reader.SkipRecord(); // Skip header
        Assert.Equal(2, reader.LineNumber);
        
        reader.TryReadRecord(out var record);
        Assert.Equal("Row1", record.ToArray()[0]);
    }

    [Fact]
    public void FastCsvReader_SkipRecords()
    {
        using var reader = new FastCsvReader("Header\nRow1\nRow2\nRow3\nRow4", CsvOptions.Default);
        
        reader.SkipRecords(3); // Skip header and 2 rows
        Assert.Equal(4, reader.LineNumber);
        
        reader.TryReadRecord(out var record);
        Assert.Equal("Row3", record.ToArray()[0]);
    }

    [Fact]
    public void FastCsvReader_ReadRecord_ThrowsWhenNoMoreData()
    {
        using var reader = new FastCsvReader("A,B", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out _);
        
        Assert.Throws<InvalidOperationException>(() => reader.ReadRecord());
    }

    #endregion

    #region AsyncDataSource Coverage

    #if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncStreamDataSource_AllPaths()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);
        
        // Test counting
        var count = await source.CountLinesDirectlyAsync();
        Assert.Equal(3, count);
        
        // Reset and read
        source.Reset();
        
        var result = await source.TryReadLineAsync(CancellationToken.None);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);
        
        // Test sync CountLinesDirectly
        source.Reset();
        count = source.CountLinesDirectly();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AsyncMemoryDataSource_EdgeCases()
    {
        // Test with MemoryDataSource
        var memSource = new MemoryDataSource("test\ndata".AsMemory());
        using var asyncSource = new AsyncMemoryDataSource(memSource);
        
        var count = await asyncSource.CountLinesDirectlyAsync();
        Assert.Equal(2, count);
    }
    #endif

    #endregion
}