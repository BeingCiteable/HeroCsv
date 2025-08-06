using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Parsing;

/// <summary>
/// Tests for core CSV parsing functionality
/// </summary>
public class CoreParsingTests
{
    #region Basic CsvRow Parsing

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
    public void CsvRow_PartialBuffer()
    {
        var buffer = "header1,header2\nfield1,field2,field3\nfooter".AsSpan();
        var row = new CsvRow(buffer, 16, 20, CsvOptions.Default); // Just the middle line

        // Test field access instead of internal Line property
        Assert.Equal(3, row.FieldCount);
        Assert.Equal("field1", row[0].ToString());
        Assert.Equal("field2", row[1].ToString());
        Assert.Equal("field3", row[2].ToString());
    }

    #endregion

    #region Basic CsvFieldEnumerator Tests

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
        // CsvFieldEnumerator returns fields with quotes included
        Assert.Equal("\"field1\"", fields[0]);
        Assert.Equal("\"field,2\"", fields[1]); // Contains delimiter
        Assert.Equal("\"field3\"", fields[2]);
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
        // CsvFieldEnumerator returns fields with quotes included
        Assert.Equal("\"field1\"", fields[0]);
        Assert.Equal("\"field\"\"2\"\"\"", fields[1]); // Contains escaped quotes (not resolved)
        Assert.Equal("\"field3\"", fields[2]);
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

    #endregion

    #region Basic CsvFieldIterator Tests

    [Fact]
    public void CsvFieldIterator_BasicIteration()
    {
        var csv = "A,B,C\n1,2,3\n4,5,6";
        var options = new CsvOptions(hasHeader: false);
        
        var fields = new List<(string value, int row, int field)>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add((field.Value.ToString(), field.RowIndex, field.FieldIndex));
        }
        
        Assert.Equal(9, fields.Count);
        Assert.Equal(("A", 0, 0), fields[0]);
        Assert.Equal(("B", 0, 1), fields[1]);
        Assert.Equal(("C", 0, 2), fields[2]);
        Assert.Equal(("1", 1, 0), fields[3]);
        Assert.Equal(("6", 2, 2), fields[8]);
    }

    [Fact]
    public void CsvFieldIterator_WithHeader()
    {
        var csv = "Header1,Header2\nValue1,Value2";
        var options = new CsvOptions(hasHeader: true);
        
        var fields = new List<(string value, int row, int field)>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add((field.Value.ToString(), field.RowIndex, field.FieldIndex));
        }
        
        // Should skip header
        Assert.Equal(2, fields.Count);
        Assert.Equal(("Value1", 0, 0), fields[0]);
        Assert.Equal(("Value2", 0, 1), fields[1]);
    }

    [Fact]
    public void CsvFieldIterator_IsFirstFieldInRow()
    {
        var csv = "A,B,C\n1,2,3";
        var options = new CsvOptions(hasHeader: false);
        
        var firstFields = new List<bool>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            firstFields.Add(field.IsFirstFieldInRow);
        }
        
        Assert.Equal(new[] { true, false, false, true, false, false }, firstFields);
    }

    #endregion

    #region Options and Configuration Tests

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
    public void CsvFieldIterator_TrimWhitespace()
    {
        var csv = "  A  ,  B  \n  1  ,  2  ";
        var options = new CsvOptions(hasHeader: false, trimWhitespace: true);
        
        var fields = new List<string>();
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add(field.Value.ToString());
        }
        
        Assert.Equal(4, fields.Count);
        Assert.Equal("A", fields[0]);
        Assert.Equal("B", fields[1]);
        Assert.Equal("1", fields[2]);
        Assert.Equal("2", fields[3]);
    }

    [Fact]
    public void CsvRow_WithSkipEmptyFields()
    {
        var csv = "A,,C,,E";
        var options = new CsvOptions(hasHeader: false, skipEmptyFields: true);
        
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), options);
        foreach (var row in rows)
        {
            // Even with skipEmptyFields, the fields are still there
            Assert.Equal(5, row.FieldCount);
            Assert.Equal("", row[1].ToString());
            Assert.Equal("", row[3].ToString());
        }
    }

    #endregion

    #region HeroCsvReader Basic Tests

    [Fact]
    public void HeroCsvReader_EnumerateRecords()
    {
        var csv = "A,B\n1,2\n3,4";
        using var reader = new HeroCsvReader(csv, CsvOptions.Default);
        
        var records = reader.ReadAllRecords();
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void HeroCsvReader_CurrentFieldCount()
    {
        var csv = "A,B,C\n1,2\n3,4,5,6";
        using var reader = new HeroCsvReader(csv, CsvOptions.Default);
        
        // TryReadRecord reads header first when hasHeader: true
        reader.TryReadRecord(out var header);
        Assert.Equal(3, header.FieldCount); // Header has 3 fields
        
        reader.TryReadRecord(out var record1);
        Assert.Equal(2, record1.FieldCount); // First data record has 2 fields
        
        reader.TryReadRecord(out var record2);
        Assert.Equal(4, record2.FieldCount); // Second data record has 4 fields
    }

    #endregion

    #region Csv Static API Tests

    [Fact]
    public void Csv_CreateReader_FromMemory()
    {
        var memory = "A,B,C".AsMemory();
        using var reader = Csv.CreateReader(memory);

        Assert.NotNull(reader);
        // Now properly uses CsvOptions.Default
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
        Assert.Equal(3, record.FieldCount); // Now properly uses CsvOptions.Default
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

    #region CsvRecord Tests

    [Fact]
    public void CsvRecord_GetField_Span()
    {
        using var reader = new HeroCsvReader("Test,Data", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        var field0 = record.GetField(0);
        Assert.Equal("Test", field0.ToString());
        
        var field1 = record.GetField(1);
        Assert.Equal("Data", field1.ToString());
    }

    #endregion

    #region Performance and Large Data Tests

    [Fact]
    public void HeroCsvReader_LargeFile_Performance()
    {
        // Generate a large CSV to test performance paths
        var sb = new StringBuilder();
        sb.AppendLine("Col1,Col2,Col3,Col4,Col5,Col6,Col7,Col8,Col9,Col10");
        
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i}");
        }
        
        var csv = sb.ToString();
        using var reader = new HeroCsvReader(csv, CsvOptions.Default);
        
        var count = reader.CountRecords();
        Assert.Equal(1000, count);
        
        // Test ReadAllRecords with pre-allocation
        reader.Reset();
        var records = reader.ReadAllRecords();
        Assert.Equal(1000, records.Count);
    }

    [Fact]
    public void CsvRow_FieldAccess_Performance()
    {
        // Test the field access patterns
        var csv = string.Join(",", Enumerable.Range(1, 100).Select(i => $"Field{i}"));
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), new CsvOptions(hasHeader: false));
        
        foreach (var row in rows)
        {
            // Access fields in different ways
            Assert.Equal(100, row.FieldCount);
            
            // Direct indexer access
            Assert.Equal("Field1", row[0].ToString());
            Assert.Equal("Field50", row[49].ToString());
            Assert.Equal("Field100", row[99].ToString());
            
            // Enumerator access
            var count = 0;
            var enumerator = row.GetFieldEnumerator();
            while (enumerator.TryGetNextField(out _))
            {
                count++;
            }
            Assert.Equal(100, count);
        }
    }

    [Fact]
    public void CsvRow_ComplexScenarios()
    {
        // Test with maximum fields
        var fieldList = Enumerable.Range(1, 50).Select(i => $"Field{i}");
        var csv = string.Join(",", fieldList);
        
        var rows = CsvParser.ParseWholeBuffer(csv.AsSpan(), new CsvOptions(hasHeader: false));
        foreach (var row in rows)
        {
            Assert.Equal(50, row.FieldCount);
            
            // Test various access patterns
            for (int i = 0; i < row.FieldCount; i++)
            {
                Assert.Equal($"Field{i + 1}", row[i].ToString());
            }
        }
    }

    #endregion
}