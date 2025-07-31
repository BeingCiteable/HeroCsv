using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastCsv;
using FastCsv.Core;
using FastCsv.Models;
using FastCsv.Parsing;
using Xunit;

namespace FastCsv.Tests;

public class ParsingEdgeCasesTests
{
    [Fact]
    public void CsvParser_ParseLine_WithQuotesAndDelimiters()
    {
        // Test complex quoted fields
        var options = CsvOptions.Default;
        
        // Field with delimiter inside quotes
        var fields = CsvParser.ParseLine("\"Name,Last\",\"Age\"".AsSpan(), options);
        Assert.Equal(2, fields.Length);
        Assert.Equal("Name,Last", fields[0]);
        Assert.Equal("Age", fields[1]);
        
        // Field with escaped quotes
        fields = CsvParser.ParseLine("\"He said \"\"Hello\"\"\",World".AsSpan(), options);
        Assert.Equal(2, fields.Length);
        Assert.Equal("He said \"Hello\"", fields[0]);
        Assert.Equal("World", fields[1]);
        
        // Empty quoted field
        fields = CsvParser.ParseLine("\"\",\"\"".AsSpan(), options);
        Assert.Equal(2, fields.Length);
        Assert.Equal("", fields[0]);
        Assert.Equal("", fields[1]);
    }

    [Fact]
    public void CsvParser_CountLines_VariousEndings()
    {
        // Unix endings
        Assert.Equal(2, CsvParser.CountLines("line1\nline2\nline3".AsSpan()));
        
        // Windows endings
        Assert.Equal(2, CsvParser.CountLines("line1\r\nline2\r\nline3".AsSpan()));
        
        // Mac endings
        Assert.Equal(2, CsvParser.CountLines("line1\rline2\rline3".AsSpan()));
        
        // Mixed endings
        Assert.Equal(3, CsvParser.CountLines("line1\r\nline2\nline3\rline4".AsSpan()));
        
        // Empty
        Assert.Equal(0, CsvParser.CountLines("".AsSpan()));
        
        // Single line no ending
        Assert.Equal(0, CsvParser.CountLines("single line".AsSpan()));
    }

    [Fact]
    public void FastCsvReader_EmptyLines()
    {
        var csv = "Name,Age\n\nJohn,25\n\n\nJane,30";
        using var reader = new FastCsvReader(csv, CsvOptions.Default);
        
        // Skip header since TryReadRecord doesn't auto-skip when hasHeader=true
        reader.TryReadRecord(out _); // Skip "Name,Age"
        
        var records = new List<string[]>();
        while (reader.TryReadRecord(out var record))
        {
            records.Add(record.ToArray());
        }
        
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
    }

    [Fact]
    public void FastCsvReader_OnlyEmptyLines()
    {
        var csv = "\n\n\n";
        using var reader = new FastCsvReader(csv, CsvOptions.Default);
        
        var count = 0;
        while (reader.TryReadRecord(out _))
        {
            count++;
        }
        
        Assert.Equal(0, count);
    }

    [Fact]
    public void CsvFieldIterator_ComplexFields()
    {
        // Test with quoted fields containing delimiters
        var csv = "normal,\"quoted,with,commas\",\"also \"\"quoted\"\"\",end";
        var fields = new List<string>();
        
        var options = new CsvOptions(hasHeader: false);
        foreach (var field in CsvFieldIterator.IterateFields(csv, options))
        {
            fields.Add(field.Value.ToString());
        }
        
        Assert.Equal(4, fields.Count);
        Assert.Equal("normal", fields[0]);
        Assert.Equal("quoted,with,commas", fields[1]);
        Assert.Equal("also \"\"quoted\"\"", fields[2]); // Raw escaped quotes
        Assert.Equal("end", fields[3]);
    }

    [Fact]
    public void CsvParser_FieldsWithNewlines()
    {
        // Fields containing newlines (when properly quoted)
        var line = "\"Multi\nLine\nField\",\"Normal\"".AsSpan();
        var fields = CsvParser.ParseLine(line, CsvOptions.Default);
        
        Assert.Equal(2, fields.Length);
        Assert.Equal("Multi\nLine\nField", fields[0]);
        Assert.Equal("Normal", fields[1]);
    }

    [Fact]
    public void FastCsvReader_LargeFile_Performance()
    {
        // Generate a large CSV to test performance paths
        var sb = new StringBuilder();
        sb.AppendLine("Col1,Col2,Col3,Col4,Col5,Col6,Col7,Col8,Col9,Col10");
        
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i},Value{i}");
        }
        
        var csv = sb.ToString();
        using var reader = new FastCsvReader(csv, CsvOptions.Default);
        
        var count = reader.CountRecords();
        Assert.Equal(1000, count);
        
        // Test ReadAllRecords with pre-allocation
        reader.Reset();
        var records = reader.ReadAllRecords();
        Assert.Equal(1000, records.Count);
    }

    [Fact]
    public void CsvOptions_AllConfigurations()
    {
        // Test various option combinations
        var options = new CsvOptions(
            delimiter: '|',
            quote: '\'',
            hasHeader: false,
            skipEmptyFields: true
        );
        
        var csv = "'Field1'|''|'Field3'\n'A'|'B'|'C'";
        using var reader = new FastCsvReader(csv, options);
        
        var records = reader.ReadAllRecords();
        Assert.Equal(2, records.Count);
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
    public void FastCsvReader_ErrorCallback()
    {
        var errorCount = 0;
        Action<CsvValidationError> callback = error => errorCount++;
        
        var csv = "A,B,C\n1,2\n3,4,5,6\n7,8,9";
        using var reader = new FastCsvReader(csv, CsvOptions.Default, validateData: true, trackErrors: true, errorCallback: callback);
        
        while (reader.TryReadRecord(out _)) { }
        
        Assert.True(errorCount > 0);
    }

    [Fact]
    public void CsvRecord_GetField_Span()
    {
        using var reader = new FastCsvReader("Test,Data", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);
        
        var field0 = record.GetField(0);
        Assert.Equal("Test", field0.ToString());
        
        var field1 = record.GetField(1);
        Assert.Equal("Data", field1.ToString());
    }
}