using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Mapping;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Parsing;

/// <summary>
/// Tests for low-level CSV parsing components including iterators, enumerators, data sources, and utilities
/// </summary>
public class LowLevelParsingTests
{
    #region CsvFieldIterator Advanced Tests

    [Fact]
    public void CsvFieldIterator_QuotedFields()
    {
        var csv = "\"A,B\",\"C\"\"D\"\"\",\"E\"\n\"1\",\"2\",\"3\"";
        var options = new CsvOptions(hasHeader: false);
        
        var fields = new List<string>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add(field.Value.ToString());
        }
        
        Assert.Equal(6, fields.Count);
        Assert.Equal("A,B", fields[0]);
        Assert.Equal("C\"\"D\"\"", fields[1]);
        Assert.Equal("E", fields[2]);
    }

    [Fact]
    public void CsvFieldIterator_EscapedQuotesInQuotedField()
    {
        var csv = "\"Field with \"\"escaped\"\" quotes\",Normal";
        var options = new CsvOptions(hasHeader: false);
        
        var fields = new List<string>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add(field.Value.ToString());
        }
        
        Assert.Equal(2, fields.Count);
        Assert.Contains("escaped", fields[0]);
        Assert.Equal("Normal", fields[1]);
    }

    #endregion

    #region StringPool Comprehensive Tests

    [Fact]
    public void StringPool_BasicPooling()
    {
        var pool = new StringPool();

        var str1 = pool.GetString("test");
        var str2 = pool.GetString("test");
        var str3 = pool.GetString("different");

        Assert.Same(str1, str2);
        Assert.NotSame(str1, str3);
    }

    [Fact]
    public void StringPool_MaxLength()
    {
        var pool = new StringPool(maxStringLength: 5);

        var short1 = pool.GetString("short");
        var short2 = pool.GetString("short");
        var longString = "toolongstring";
        var long1 = pool.GetString(longString);
        var long2 = pool.GetString(longString);

        Assert.Same(short1, short2);
        Assert.Same(long1, long2); // Too long, not pooled but returns same instance passed in
        Assert.Same(longString, long1); // Confirms it's the original string
    }

    [Fact]
    public void StringPool_FromSpan()
    {
        var pool = new StringPool();
        var span = "test".AsSpan();

        var str1 = pool.GetString(span);
        var str2 = pool.GetString("test");

        Assert.Same(str1, str2);
    }

    [Fact]
    public void StringPool_Clear()
    {
        var pool = new StringPool();

        var str1 = pool.GetString("test");
        Assert.Equal(1, pool.Count);

        pool.Clear();
        Assert.Equal(0, pool.Count);

        // Use new string to avoid string interning
        var str2 = pool.GetString(new string("test".ToCharArray()));
        Assert.NotSame(str1, str2); // New instance after clear
    }

    [Fact]
    public void StringPool_EmptyString()
    {
        var pool = new StringPool();

        var str1 = pool.GetString("");
        var str2 = pool.GetString(string.Empty);

        Assert.Same(string.Empty, str1);
        Assert.Same(string.Empty, str2);
    }

    [Fact]
    public void StringPool_NullString()
    {
        var pool = new StringPool();

        var str = pool.GetString(null!);

        Assert.Null(str);
    }

    [Fact]
    public void StringPool_EdgeCases()
    {
        var pool = new StringPool();
        
        // Test empty string
        var empty1 = pool.GetString(ReadOnlySpan<char>.Empty);
        var empty2 = pool.GetString("");
        Assert.Equal("", empty1);
        Assert.Equal("", empty2);
        
        // Test very long string
        var longString = new string('X', 1000);
        var pooled = pool.GetString(longString);
        Assert.Equal(longString, pooled);
        
        // Test special characters
        var special = "Test\n\r\t\"',";
        var pooledSpecial = pool.GetString(special);
        Assert.Equal(special, pooledSpecial);
        
        // Clear and reuse
        pool.Clear();
        var afterClear = pool.GetString("Test");
        Assert.Equal("Test", afterClear);
    }

    [Fact]
    public void StringPool_ManyUniqueStrings()
    {
        var pool = new StringPool();
        var strings = new List<string>();
        
        // Add many unique strings
        for (int i = 0; i < 100; i++)
        {
            var str = $"UniqueString{i}";
            strings.Add(pool.GetString(str));
        }
        
        // Verify all strings are correct
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal($"UniqueString{i}", strings[i]);
        }
    }

    [Fact]
    public void CsvRow_WithStringPool()
    {
        var buffer = "field1,field1,field1".AsSpan();
        var pool = new StringPool();
        var options = new CsvOptions(stringPool: pool);
        var row = new CsvRow(buffer, 0, buffer.Length, options);

        var str1 = row.GetString(0);
        var str2 = row.GetString(1);
        var str3 = row.GetString(2);

        // All should be the same reference due to pooling
        Assert.Same(str1, str2);
        Assert.Same(str2, str3);
    }

    #endregion

    #region MemoryDataSource Tests

    [Fact]
    public void MemoryDataSource_CompleteLineReading()
    {
        // Test with no trailing newline
        var content = "Line1\nLine2\nLine3".AsMemory();
        using var source = new MemoryDataSource(content);
        
        var lines = new List<string>();
        while (source.TryReadLine(out var line, out _))
        {
            lines.Add(line.ToString());
        }
        
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line1", lines[0]);
        Assert.Equal("Line2", lines[1]);
        Assert.Equal("Line3", lines[2]);
        
        // Test counting - should count actual lines, not newlines
        source.Reset();
        Assert.Equal(3, source.CountLines());
    }

    [Fact]
    public void MemoryDataSource_EmptyLines()
    {
        var content = "\n\n\n".AsMemory();
        using var source = new MemoryDataSource(content);
        
        var count = 0;
        while (source.TryReadLine(out var line, out _))
        {
            Assert.Equal("", line.ToString());
            count++;
        }
        
        Assert.Equal(3, count);
    }

    [Fact]
    public void MemoryDataSource_MixedLineEndings()
    {
        var content = "A\r\nB\nC\rD".AsMemory();
        using var source = new MemoryDataSource(content);
        
        var lines = new List<string>();
        while (source.TryReadLine(out var line, out _))
        {
            lines.Add(line.ToString());
        }
        
        Assert.Equal(4, lines.Count);
        Assert.Equal(new[] { "A", "B", "C", "D" }, lines);
    }

    #endregion

    #region Async Operations Coverage

#if NET6_0_OR_GREATER
    [Fact]
    public async Task StreamDataSource_LargeBuffer()
    {
        // Create a large CSV to test buffer handling
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"Line{i},Data{i}");
        }
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var source = new StreamDataSource(stream);
        
        var lineCount = 0;
        while ((await source.TryReadLineAsync(CancellationToken.None)).success)
        {
            lineCount++;
        }
        
        Assert.Equal(1000, lineCount);
    }
#endif

    #endregion

    #region CsvMapper Edge Cases

    [Fact]
    public void CsvMapper_InvalidConversions_UsesDefaults()
    {
        var mapper = new CsvMapper<TestModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "IntValue", "DateValue" });
        
        // Test invalid int conversion - uses default value
        var record = new[] { "NotANumber", "2025-01-01" };
        var result = mapper.MapRecord(record);
        Assert.Equal(0, result.IntValue); // Default int
        Assert.Equal(new DateTime(2025, 1, 1), result.DateValue);
        
        // Test invalid date conversion - uses default value
        record = new[] { "42", "NotADate" };
        result = mapper.MapRecord(record);
        Assert.Equal(42, result.IntValue);
        Assert.Equal(default(DateTime), result.DateValue); // Default DateTime
    }

    [Fact]
    public void CsvMapper_EnumConversion()
    {
        var mapper = new CsvMapper<TestModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "EnumValue" });
        
        // Test valid enum
        var record = new[] { "Value2" };
        var model = mapper.MapRecord(record);
        Assert.Equal(TestEnum.Value2, model.EnumValue);
        
        // Test invalid enum
        record = new[] { "InvalidValue" };
        model = mapper.MapRecord(record);
        Assert.Equal(default(TestEnum), model.EnumValue);
        
        // Test numeric enum value
        record = new[] { "1" };
        model = mapper.MapRecord(record);
        Assert.Equal(TestEnum.Value2, model.EnumValue);
    }

    [Fact]
    public void CsvMapper_ArrayProperties()
    {
        var mapper = new CsvMapper<TestModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "ArrayValue" });
        
        // Arrays are not directly supported, should remain null
        var record = new[] { "1,2,3,4" };
        var model = mapper.MapRecord(record);
        Assert.Null(model.ArrayValue);
    }

    public class TestModel
    {
        public int IntValue { get; set; }
        public DateTime DateValue { get; set; }
        public TestEnum EnumValue { get; set; }
        public int[]? ArrayValue { get; set; }
    }

    public enum TestEnum
    {
        Value1 = 0,
        Value2 = 1,
        Value3 = 2
    }

    #endregion
}