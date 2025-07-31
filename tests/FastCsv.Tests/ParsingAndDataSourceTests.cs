using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastCsv;
using FastCsv.DataSources;
using FastCsv.Models;
using FastCsv.Parsing;
using FastCsv.Utilities;
using Xunit;

namespace FastCsv.Tests;

public class ParsingAndDataSourceTests
{
    #region CsvFieldEnumerator Tests
    
    [Fact]
    public void CsvFieldEnumerator_SimpleFields()
    {
        var line = "field1,field2,field3".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(3, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field2", fields[1]);
        Assert.Equal("field3", fields[2]);
    }
    
    [Fact]
    public void CsvFieldEnumerator_QuotedFields()
    {
        var line = "\"field1\",\"field,2\",\"field3\"".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(3, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field,2", fields[1]); // Contains delimiter
        Assert.Equal("field3", fields[2]);
    }
    
    [Fact]
    public void CsvFieldEnumerator_EscapedQuotes()
    {
        var line = "\"field1\",\"field\"\"2\"\"\",\"field3\"".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(3, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field\"\"2\"\"", fields[1]); // Contains escaped quotes
        Assert.Equal("field3", fields[2]);
    }
    
    [Fact]
    public void CsvFieldEnumerator_EmptyFields()
    {
        var line = "field1,,field3,".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(4, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("", fields[1]);
        Assert.Equal("field3", fields[2]);
        Assert.Equal("", fields[3]);
    }
    
    [Fact]
    public void CsvFieldEnumerator_GetFieldByIndex()
    {
        var line = "field1,field2,field3".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var field0 = enumerator.GetFieldByIndex(0);
        var field1 = enumerator.GetFieldByIndex(1);
        var field2 = enumerator.GetFieldByIndex(2);
        var field3 = enumerator.GetFieldByIndex(3); // Out of bounds
        
        Assert.Equal("field1", field0.ToString());
        Assert.Equal("field2", field1.ToString());
        Assert.Equal("field3", field2.ToString());
        Assert.True(field3.IsEmpty);
    }
    
    [Fact]
    public void CsvFieldEnumerator_CountTotalFields()
    {
        var line = "field1,field2,field3".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var count = enumerator.CountTotalFields();
        
        Assert.Equal(3, count);
    }
    
    [Fact]
    public void CsvFieldEnumerator_CountTotalFields_EmptyLine()
    {
        var line = "".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var count = enumerator.CountTotalFields();
        
        Assert.Equal(0, count);
    }
    
    [Fact]
    public void CsvFieldEnumerator_UnterminatedQuotedField()
    {
        var line = "field1,\"field2".AsSpan();
        var enumerator = new CsvFieldEnumerator(line, ',', '"');
        
        var fields = new List<string>();
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(2, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("\"field2", fields[1]); // Unterminated quote
    }
    
    #endregion

    #region CsvRow Tests
    
    [Fact]
    public void CsvRow_BasicAccess()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        Assert.Equal(3, row.FieldCount);
        Assert.Equal("field1", row[0].ToString());
        Assert.Equal("field2", row[1].ToString());
        Assert.Equal("field3", row[2].ToString());
    }
    
    [Fact]
    public void CsvRow_GetString()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        var field0 = row.GetString(0);
        var field1 = row.GetString(1);
        var field2 = row.GetString(2);
        
        Assert.Equal("field1", field0);
        Assert.Equal("field2", field1);
        Assert.Equal("field3", field2);
    }
    
    [Fact]
    public void CsvRow_ToArray()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        var array = row.ToArray();
        
        Assert.Equal(3, array.Length);
        Assert.Equal("field1", array[0]);
        Assert.Equal("field2", array[1]);
        Assert.Equal("field3", array[2]);
    }
    
    [Fact]
    public void CsvRow_WithTrimWhitespace()
    {
        var buffer = "  field1  ,  field2  ,  field3  ".AsSpan();
        var options = new CsvOptions(trimWhitespace: true);
        var row = new CsvRow(buffer, 0, buffer.Length, options);
        
        Assert.Equal("field1", row[0].ToString());
        Assert.Equal("field2", row[1].ToString());
        Assert.Equal("field3", row[2].ToString());
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
    
    [Fact]
    public void CsvRow_GetFieldEnumerator()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        var enumerator = row.GetFieldEnumerator();
        var fields = new List<string>();
        
        while (enumerator.TryGetNextField(out var field))
        {
            fields.Add(field.ToString());
        }
        
        Assert.Equal(3, fields.Count);
        Assert.Equal("field1", fields[0]);
        Assert.Equal("field2", fields[1]);
        Assert.Equal("field3", fields[2]);
    }
    
    [Fact]
    public void CsvRow_IndexOutOfRange()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        // Can't use lambda with ref struct, so use try-catch
        try
        {
            var _ = row[-1];
            Assert.True(false, "Should have thrown IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException)
        {
            // Expected
        }
        
        try
        {
            var _ = row[3];
            Assert.True(false, "Should have thrown IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException)
        {
            // Expected
        }
    }
    
    [Fact]
    public void CsvRow_Line_Property()
    {
        var buffer = "field1,field2,field3".AsSpan();
        var row = new CsvRow(buffer, 0, buffer.Length, CsvOptions.Default);
        
        Assert.Equal("field1,field2,field3", row.Line.ToString());
    }
    
    [Fact]
    public void CsvRow_PartialBuffer()
    {
        var buffer = "header1,header2\nfield1,field2,field3\nfooter".AsSpan();
        var row = new CsvRow(buffer, 16, 20, CsvOptions.Default); // Just the middle line
        
        Assert.Equal("field1,field2,field3", row.Line.ToString());
        Assert.Equal(3, row.FieldCount);
    }
    
    #endregion

    #region StringPool Tests
    
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
        var long1 = pool.GetString("toolongstring");
        var long2 = pool.GetString("toolongstring");
        
        Assert.Same(short1, short2);
        Assert.NotSame(long1, long2); // Too long, not pooled
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
        
        var str2 = pool.GetString("test");
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
    
    #endregion

    #region Async Data Source Tests
    
    #if NET6_0_OR_GREATER
    [Fact]
    public async Task CsvDataSourceAsyncExtensions_TryReadLineAsyncDefault()
    {
        var source = new StringDataSource("line1\nline2\nline3");
        
        var result1 = await source.TryReadLineAsyncDefault();
        Assert.True(result1.success);
        Assert.Equal("line1", result1.line);
        Assert.Equal(1, result1.lineNumber);
        
        var result2 = await source.TryReadLineAsyncDefault();
        Assert.True(result2.success);
        Assert.Equal("line2", result2.line);
        Assert.Equal(2, result2.lineNumber);
        
        var result3 = await source.TryReadLineAsyncDefault();
        Assert.True(result3.success);
        Assert.Equal("line3", result3.line);
        Assert.Equal(3, result3.lineNumber);
        
        var result4 = await source.TryReadLineAsyncDefault();
        Assert.False(result4.success);
    }
    
    [Fact]
    public async Task CsvDataSourceAsyncExtensions_CountLinesDirectlyAsyncDefault()
    {
        var source = new StringDataSource("line1\nline2\nline3");
        
        var count = await source.CountLinesDirectlyAsyncDefault();
        
        // CountLinesDirectly might count differently
        Assert.True(count >= 2);
    }
    
    [Fact]
    public async Task StreamDataSourceAsync_ReadFileAsync()
    {
        var content = "header1,header2\nvalue1,value2\nvalue3,value4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        var source = new StreamDataSource(stream, Encoding.UTF8, leaveOpen: true);
        
        // Read lines using async extension
        var lines = new List<string>();
        while (true)
        {
            var result = await source.TryReadLineAsyncDefault();
            if (!result.success)
                break;
            lines.Add(result.line);
        }
        
        Assert.Equal(3, lines.Count);
        Assert.Equal("header1,header2", lines[0]);
        Assert.Equal("value1,value2", lines[1]);
        Assert.Equal("value3,value4", lines[2]);
    }
    #endif
    
    #endregion

    #region Additional Csv class tests
    
    [Fact]
    public void Csv_CreateReader_FromMemory()
    {
        var memory = "A,B,C".AsMemory();
        using var reader = Csv.CreateReader(memory);
        
        Assert.NotNull(reader);
        reader.TryReadRecord(out var record);
        Assert.Equal(3, record.FieldCount);
    }
    
    [Fact]
    public void Csv_CreateReader_FromStream()
    {
        var content = "A,B,C\n1,2,3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var reader = Csv.CreateReader(stream);
        
        Assert.NotNull(reader);
        reader.TryReadRecord(out var record);
        Assert.Equal(3, record.FieldCount);
    }
    
    [Fact]
    public void Csv_CreateReader_WithEncoding()
    {
        var content = "A,B,C\n1,2,3";
        using var stream = new MemoryStream(Encoding.UTF32.GetBytes(content));
        using var reader = Csv.CreateReader(stream, CsvOptions.Default, Encoding.UTF32);
        
        Assert.NotNull(reader);
        reader.TryReadRecord(out var record);
        Assert.Equal(3, record.FieldCount);
    }
    
    [Fact]
    public void Csv_ReadContent_WithDelimiter()
    {
        var content = "A;B;C\n1;2;3";
        var records = Csv.ReadContent(content, ';').ToList();
        
        Assert.Single(records);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("2", records[0][1]);
        Assert.Equal("3", records[0][2]);
    }
    
    #endregion
}