using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Models;
using HeroCsv.Parsing;
using Xunit;

namespace HeroCsv.Tests;

public class ReaderStreamTests
{
    #region HeroCsvReader Additional Coverage

    [Fact]
    public void HeroCsvReader_CountRecords_WithValidation()
    {
        var csv = "Name,Age\nJohn,25\nJane,30";
        using var reader = new HeroCsvReader(
            csv,
            CsvOptions.Default,
            validateData: true,
            trackErrors: true);

        var count = reader.CountRecords();
        Assert.Equal(2, count);
    }

    [Fact]
    public void HeroCsvReader_CountRecords_StreamSource()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        using var reader = new HeroCsvReader(stream, CsvOptions.Default, validateData: false, trackErrors: false);

        var count = reader.CountRecords();
        Assert.Equal(2, count);
    }

    [Fact]
    public void HeroCsvReader_CountRecords_NoNewlineAtEnd()
    {
        var csv = "A,B\n1,2\n3,4"; // No trailing newline
        using var reader = new HeroCsvReader(csv, CsvOptions.Default, validateData: false, trackErrors: false);

        var count = reader.CountRecords();
        Assert.Equal(2, count);
    }

    [Fact]
    public void HeroCsvReader_CountRecords_WithNewlineAtEnd()
    {
        var csv = "A,B\n1,2\n3,4\n"; // With trailing newline
        using var reader = new HeroCsvReader(csv, CsvOptions.Default, validateData: false, trackErrors: false);

        var count = reader.CountRecords();
        Assert.Equal(2, count);
    }

    [Fact]
    public void HeroCsvReader_CountRecords_NoHeader()
    {
        var csv = "1,2\n3,4";
        var options = new CsvOptions(hasHeader: false);
        using var reader = new HeroCsvReader(csv, options, validateData: false, trackErrors: false);

        var count = reader.CountRecords();
        Assert.Equal(2, count);
    }

    [Fact]
    public void HeroCsvReader_GetRecords_Enumerable()
    {
        var csv = "A,B\n1,2\n3,4";
        using var reader = new HeroCsvReader(csv, CsvOptions.Default, validateData: false, trackErrors: false);

        var records = reader.GetRecords().ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal(new[] { "1", "2" }, records[0]);
        Assert.Equal(new[] { "3", "4" }, records[1]);
    }

    [Fact]
    public void HeroCsvReader_Dispose_MultipleTimes()
    {
        var reader = new HeroCsvReader("test", CsvOptions.Default, validateData: false, trackErrors: false);
        reader.Dispose();
        reader.Dispose(); // Should not throw
    }

    [Fact]
    public void HeroCsvReader_Properties()
    {
        var options = new CsvOptions(delimiter: '|', hasHeader: false);
        using var reader = new HeroCsvReader("A|B\n1|2", options, validateData: false, trackErrors: false);

        Assert.Equal(options, reader.Options);
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(0, reader.RecordCount);

        reader.TryReadRecord(out _);
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(1, reader.RecordCount);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task HeroCsvReader_ReadAllRecordsAsync()
    {
        var csv = "A,B\n1,2\n3,4";
        using var reader = new HeroCsvReader(csv, CsvOptions.Default, validateData: false, trackErrors: false);

        var records = await reader.ReadAllRecordsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task HeroCsvReader_ReadAllRecordsAsync_WithCancellation()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        using var reader = new HeroCsvReader(stream, CsvOptions.Default, validateData: false, trackErrors: false);
        using var cts = new CancellationTokenSource();

        cts.Cancel();
        var records = await reader.ReadAllRecordsAsync(cts.Token);
        // Should return empty or partial results when cancelled
        Assert.NotNull(records);
    }
#endif

    #endregion

    #region CsvRecord Additional Coverage

    [Fact]
    public void CsvRecord_Properties()
    {
        var line = "field1,field2,field3";
        using var reader = new HeroCsvReader(line, new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);

        Assert.NotNull(record);
        Assert.Equal(3, record.FieldCount);
    }

    [Fact]
    public void CsvRecord_TryGetFieldSpan()
    {
        var line = "A,B,C";
        using var reader = new HeroCsvReader(line, new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);

        Assert.True(record.TryGetField(0, out var field));
        Assert.Equal("A", field);

        Assert.True(record.TryGetField(1, out field));
        Assert.Equal("B", field);

        Assert.False(record.TryGetField(5, out _));
    }

    [Fact]
    public void CsvRecord_ToArray_Various()
    {
        // Test with quotes
        var line = "\"A\",B,\"C\"";
        using var reader1 = new HeroCsvReader(line, new CsvOptions(hasHeader: false));
        reader1.TryReadRecord(out var record1);
        var array = record1.ToArray();
        Assert.Equal(new[] { "A", "B", "C" }, array);

        // Test with empty fields
        line = "A,,C";
        using var reader2 = new HeroCsvReader(line, new CsvOptions(hasHeader: false));
        reader2.TryReadRecord(out var record2);
        array = record2.ToArray();
        Assert.Equal(new[] { "A", "", "C" }, array);
    }

    #endregion

    #region Data Source Additional Coverage

    [Fact]
    public void HeroCsvReader_DataSource_EdgeCases()
    {
        // Test with empty string
        using var reader1 = new HeroCsvReader("", CsvOptions.Default);
        Assert.False(reader1.HasMoreData);
        Assert.False(reader1.TryReadRecord(out _));

        // Test with single line without newline
        using var reader2 = new HeroCsvReader("single line", new CsvOptions(hasHeader: false));
        Assert.True(reader2.TryReadRecord(out var record));
        Assert.Single(record.ToArray());
        Assert.Equal("single line", record.ToArray()[0]);
        Assert.False(reader2.TryReadRecord(out _));
    }

    [Fact]
    public void HeroCsvReader_StreamDataSource_NonSeekable()
    {
        using var stream = new NonSeekableMemoryStream(Encoding.UTF8.GetBytes("A,B\n1,2"));
        using var reader = new HeroCsvReader(stream, new CsvOptions(hasHeader: false));

        // Cannot reset non-seekable stream
        Assert.Throws<NotSupportedException>(() => reader.Reset());

        Assert.True(reader.TryReadRecord(out var record));
        Assert.Equal(2, record.FieldCount);
        Assert.Equal("A", record.ToArray()[0]); // First record should be "A,B"
    }

    [Fact]
    public void HeroCsvReader_StreamDataSource_LeaveOpen()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        using (var reader = new HeroCsvReader(stream, new CsvOptions(hasHeader: false), leaveOpen: true))
        {
            reader.TryReadRecord(out _);
        }

        // Stream should still be open
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    #endregion

    #region Csv Class Additional Coverage

    [Fact]
    public void Csv_ReadStream_Basic()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var records = Csv.ReadStream(stream).ToList();
        Assert.Equal(2, records.Count); // Data records only, header excluded
    }

    [Fact]
    public void Csv_ReadStream_WithOptions()
    {
        var csv = "A|B\n1|2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new CsvOptions(delimiter: '|');

        var records = Csv.ReadStream(stream, options);
        Assert.Single(records);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task Csv_ReadStreamAsync_Basic()
    {
        var csv = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var records = await Csv.ReadStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, records.Count);
    }
#endif

    #endregion

    #region CsvValidationResult Additional Coverage

    [Fact]
    public void CsvValidationResult_Properties()
    {
        var result = new CsvValidationResult();

        // Initially valid
        Assert.True(result.IsValid);
        Assert.False(result.Errors.Count > 0);
        Assert.Empty(result.Errors);

        // Add an error
        var error = new CsvValidationError(CsvErrorType.InconsistentFieldCount, "Test", 1);
        result.AddError(error);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
        Assert.Single(result.Errors);

        // Clear errors
        result.Clear();
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CsvValidationResult_GetErrorsByType()
    {
        var result = new CsvValidationResult();

        result.AddError(new CsvValidationError(CsvErrorType.InconsistentFieldCount, "Count error", 1));
        result.AddError(new CsvValidationError(CsvErrorType.UnbalancedQuotes, "Quote error", 2));
        result.AddError(new CsvValidationError(CsvErrorType.InconsistentFieldCount, "Another count error", 3));

        // Test filtering errors by type
        var countErrors = result.Errors.Where(e => e.ErrorType == CsvErrorType.InconsistentFieldCount).ToList();
        Assert.Equal(2, countErrors.Count);

        var quoteErrors = result.Errors.Where(e => e.ErrorType == CsvErrorType.UnbalancedQuotes).ToList();
        Assert.Single(quoteErrors);
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public void CsvParser_ParseLine_EdgeCases()
    {
        var options = CsvOptions.Default;

        // Empty line
        var fields = CsvParser.ParseLine("".AsSpan(), options);
        Assert.Single(fields);
        Assert.Equal("", fields[0]);

        // Line with only delimiter
        fields = CsvParser.ParseLine(",".AsSpan(), options);
        Assert.Equal(2, fields.Length);
        Assert.Equal("", fields[0]);
        Assert.Equal("", fields[1]);

        // Multiple delimiters
        fields = CsvParser.ParseLine(",,,".AsSpan(), options);
        Assert.Equal(4, fields.Length);
        Assert.All(fields, f => Assert.Equal("", f));
    }

    [Fact]
    public void CsvFieldIterator_EdgeCases()
    {
        // Empty CSV
        var fieldCount = 0;
        foreach (var field in CsvFieldIterator.IterateFields("", new CsvOptions(hasHeader: false)))
        {
            fieldCount++;
        }
        Assert.Equal(0, fieldCount);

        // Single field
        fieldCount = 0;
        string? firstFieldValue = null;
        foreach (var field in CsvFieldIterator.IterateFields("single", new CsvOptions(hasHeader: false)))
        {
            if (fieldCount == 0)
                firstFieldValue = field.Value.ToString();
            fieldCount++;
        }
        Assert.Equal(1, fieldCount);
        Assert.Equal("single", firstFieldValue);

        // Multiple empty lines
        fieldCount = 0;
        foreach (var field in CsvFieldIterator.IterateFields("\n\n\n", new CsvOptions(hasHeader: false)))
        {
            fieldCount++;
        }
        Assert.Equal(3, fieldCount); // 3 empty fields
    }

    [Fact]
    public void HeroCsvReader_EnumerateRows_Test()
    {
        // Test with HeroCsvReader directly
        var reader = new HeroCsvReader("A,B\n1,2", new CsvOptions(hasHeader: false));

        // EnumerateRows is a method on HeroCsvReader, not ICsvReader
        if (reader is HeroCsvReader fastReader)
        {
            var rows = fastReader.EnumerateRows();
            var rowCount = 0;
            foreach (var row in rows)
            {
                rowCount++;
                if (rowCount == 1)
                {
                    Assert.Equal("A", row[0].ToString());
                    Assert.Equal("B", row[1].ToString());
                }
                else if (rowCount == 2)
                {
                    Assert.Equal("1", row[0].ToString());
                    Assert.Equal("2", row[1].ToString());
                }
            }
            Assert.Equal(2, rowCount);
        }

        reader.Dispose();
    }

    #endregion

    // Helper class
    private class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] buffer) : base(buffer) { }
        public override bool CanSeek => false;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    }
}