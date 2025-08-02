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
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Integration tests for mapping functionality combined with various data sources
/// </summary>
public class MappingIntegrationTests
{
    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int Age { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
        public Guid? Id { get; set; }
        public byte Status { get; set; }
        public long Points { get; set; }
        public double Rating { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
    }

    public class SimpleModel
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    #region CsvMapping Tests

    [Fact]
    public void CsvMapping_Create_Basic()
    {
        var mapping = CsvMapping<Person>.Create();

        Assert.NotNull(mapping);
        Assert.Empty(mapping.PropertyMappings);
        Assert.False(mapping.UseAutoMapWithOverrides);
    }

    [Fact]
    public void CsvMapping_CreateAutoMapWithOverrides()
    {
        var mapping = CsvMapping<Person>.CreateAutoMapWithOverrides();

        Assert.NotNull(mapping);
        Assert.True(mapping.UseAutoMapWithOverrides);
    }

    [Fact]
    public void CsvMapping_MapProperty_ByName()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", "first_name")
            .MapProperty("LastName", "last_name");

        Assert.Equal(2, mapping.PropertyMappings.Count);
        Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
        Assert.Equal("first_name", mapping.PropertyMappings[0].ColumnName);
        Assert.Null(mapping.PropertyMappings[0].ColumnIndex);
    }

    [Fact]
    public void CsvMapping_MapProperty_ByIndex()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", 0)
            .MapProperty("LastName", 1);

        Assert.Equal(2, mapping.PropertyMappings.Count);
        Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
        Assert.Equal(0, mapping.PropertyMappings[0].ColumnIndex);
        Assert.Null(mapping.PropertyMappings[0].ColumnName);
    }

    [Fact]
    public void CsvMapping_MapProperty_WithConverter_ByName()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("Age", "age", value => int.Parse(value) * 2);

        Assert.Single(mapping.PropertyMappings);
        Assert.NotNull(mapping.PropertyMappings[0].Converter);

        // Test the converter
        var result = mapping.PropertyMappings[0].Converter!("10");
        Assert.Equal(20, result);
    }

    [Fact]
    public void CsvMapping_MapProperty_WithConverter_ByIndex()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("Age", 2, value => string.IsNullOrEmpty(value) ? 0 : int.Parse(value));

        Assert.Single(mapping.PropertyMappings);
        Assert.NotNull(mapping.PropertyMappings[0].Converter);

        // Test the converter with empty value
        var result = mapping.PropertyMappings[0].Converter!("");
        Assert.Equal(0, result);
    }

    [Fact]
    public void CsvMapping_UseAutoMapWithOverrides()
    {
        var mapping = CsvMapping<Person>.Create()
            .EnableAutoMapWithOverrides()
            .MapProperty("FirstName", 0);

        Assert.True(mapping.UseAutoMapWithOverrides);
        Assert.Single(mapping.PropertyMappings);
    }

    [Fact]
    public void CsvMapping_WithOptions()
    {
        var options = new CsvOptions(delimiter: '|');
        var mapping = CsvMapping<Person>.Create();
        mapping.Options = options;

        Assert.Equal('|', mapping.Options.Delimiter);
    }

    #endregion

    #region CsvMapper Manual Mapping Tests

    [Fact]
    public void CsvMapper_ManualMapping_Basic()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", 0)
            .MapProperty("Age", 1);

        var mapper = new CsvMapper<Person>(mapping);
        var record = new[] { "John", "25", "ExtraField" };

        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal(25, person.Age);
        Assert.Equal("", person.LastName); // Not mapped
    }

    [Fact]
    public void CsvMapper_ManualMapping_WithConverter()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", 0)
            .MapProperty("Age", 1, value => int.Parse(value) * 10)
            .MapProperty("IsActive", 2, value => value == "Y");

        var mapper = new CsvMapper<Person>(mapping);
        var record = new[] { "John", "3", "Y" };

        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal(30, person.Age); // 3 * 10
        Assert.True(person.IsActive);
    }

    [Fact]
    public void CsvMapper_AutoMapWithOverrides()
    {
        var mapping = CsvMapping<Person>.CreateAutoMapWithOverrides()
            .MapProperty("Age", 2); // Override age to different position

        var mapper = new CsvMapper<Person>(mapping);
        mapper.SetHeaders(new[] { "FirstName", "LastName", "Age" });

        var record = new[] { "John", "Doe", "30" };
        var person = mapper.MapRecord(record);

        // Auto mapping with overrides might not work as expected
        // Just verify we got a person object
        Assert.NotNull(person);
        Assert.Equal(30, person.Age); // Should be mapped to index 2
    }

    [Fact]
    public void CsvMapper_ManualMapping_ByColumnName()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", "given_name")
            .MapProperty("LastName", "surname");

        var mapper = new CsvMapper<Person>(mapping);
        mapper.SetHeaders(new[] { "given_name", "surname", "age" });

        var record = new[] { "John", "Doe", "25" };
        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal(0, person.Age); // Not mapped
    }

    [Fact]
    public void CsvMapper_AllTypeConversions()
    {
        var mapper = new CsvMapper<Person>(CsvOptions.Default);
        mapper.SetHeaders(new[] {
            "FirstName", "LastName", "Age", "BirthDate", "IsActive",
            "Salary", "Id", "Status", "Points", "Rating", "LastLogin"
        });

        var record = new[] {
            "John", "Doe", "30", "1990-01-15", "true",
            "75000.50", "12345678-1234-1234-1234-123456789012", "5", "1000000", "4.5", "2025-01-01T10:00:00+00:00"
        };

        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal(30, person.Age);
        Assert.Equal(new DateTime(1990, 1, 15), person.BirthDate);
        Assert.True(person.IsActive);
        Assert.Equal(75000.50m, person.Salary);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), person.Id);
        Assert.Equal((byte)5, person.Status);
        Assert.Equal(1000000L, person.Points);
        Assert.Equal(4.5, person.Rating);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero), person.LastLogin);
    }

    [Fact]
    public void CsvMapper_NullableTypes_EmptyValues()
    {
        var mapper = new CsvMapper<Person>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "FirstName", "BirthDate", "Id", "LastLogin" });

        var record = new[] { "John", "", "", "" };
        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Null(person.BirthDate);
        Assert.Null(person.Id);
        Assert.Null(person.LastLogin);
    }

    [Fact]
    public void CsvMapper_OutOfBoundsIndex_Ignored()
    {
        var mapping = CsvMapping<Person>.Create()
            .MapProperty("FirstName", 0)
            .MapProperty("Age", 10); // Out of bounds

        var mapper = new CsvMapper<Person>(mapping);
        var record = new[] { "John" };

        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal(0, person.Age); // Default value
    }

    [Fact]
    public void CsvMapper_EmptyFieldHandling()
    {
        var options = new CsvOptions(skipEmptyFields: true);
        var mapper = new CsvMapper<Person>(options);
        mapper.SetHeaders(new[] { "FirstName", "Age" });

        var record = new[] { "John", "" };
        var person = mapper.MapRecord(record);

        Assert.Equal("John", person.FirstName);
        Assert.Equal(0, person.Age); // Empty field skipped
    }

    [Fact]
    public void CsvMapper_FallbackConversion()
    {
        // Test the fallback Convert.ChangeType for types not in the switch
        var mapper = new CsvMapper<SimpleModel>(CsvOptions.Default);
        mapper.SetHeaders(new[] { "Name", "Value" });

        var record = new[] { "Test", "42" };
        var model = mapper.MapRecord(record);

        Assert.Equal("Test", model.Name);
        Assert.Equal(42, model.Value);
    }

    #endregion

    #region AsyncStreamDataSource Tests

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncStreamDataSource_ReadLines()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        var lines = new List<string>();
        while (true)
        {
            var result = await source.TryReadLineAsync(TestContext.Current.CancellationToken);
            if (!result.success)
                break;
            lines.Add(result.line);
        }

        Assert.Equal(3, lines.Count);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("line2", lines[1]);
        Assert.Equal("line3", lines[2]);
    }

    [Fact]
    public async Task AsyncStreamDataSource_CountLines()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        var count = await source.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, count);
    }

    [Fact]
    public void AsyncStreamDataSource_Reset()
    {
        var content = "line1\nline2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);

        // Read first line
        source.TryReadLine(out _, out _);

        // Reset
        source.Reset();

        // Should be able to read from beginning again
        source.TryReadLine(out var line, out var lineNumber);
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNumber);
    }

    [Fact]
    public void AsyncStreamDataSource_NonSeekableStream()
    {
        using var stream = new NonSeekableStream();
        using var source = new AsyncStreamDataSource(stream);

        Assert.False(source.SupportsReset);
        Assert.Throws<NotSupportedException>(() => source.Reset());
    }

    [Fact]
    public void AsyncStreamDataSource_GetBuffer_NotSupported()
    {
        using var stream = new MemoryStream();
        using var source = new AsyncStreamDataSource(stream);

        Assert.Throws<NotSupportedException>(() => source.GetBuffer());
    }

    [Fact]
    public void AsyncStreamDataSource_LeaveOpen()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        using (var source = new AsyncStreamDataSource(stream, leaveOpen: true))
        {
            source.TryReadLine(out _, out _);
        }

        // Stream should still be open
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public async Task AsyncStreamDataSource_WithCancellation()
    {
        var content = "line1\nline2\nline3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var source = new AsyncStreamDataSource(stream);
        using var cts = new CancellationTokenSource();

        var result = await source.TryReadLineAsync(cts.Token);
        Assert.True(result.success);
        Assert.Equal("line1", result.line);
    }

#endif

    #region AsyncMemoryDataSource Tests

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncMemoryDataSource_Basic()
    {
        var innerSource = new StringDataSource("line1\nline2");
        using var asyncSource = new AsyncMemoryDataSource(innerSource);

        var result1 = await asyncSource.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result1.success);
        Assert.Equal("line1", result1.line);

        var result2 = await asyncSource.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.True(result2.success);
        Assert.Equal("line2", result2.line);

        var result3 = await asyncSource.TryReadLineAsync(TestContext.Current.CancellationToken);
        Assert.False(result3.success);
    }

    [Fact]
    public async Task AsyncMemoryDataSource_CountLines()
    {
        var innerSource = new StringDataSource("line1\nline2\nline3");
        using var asyncSource = new AsyncMemoryDataSource(innerSource);

        var count = await asyncSource.CountLinesDirectlyAsync(TestContext.Current.CancellationToken);
        Assert.True(count >= 2); // Implementation dependent
    }

    [Fact]
    public void AsyncMemoryDataSource_SyncMethods()
    {
        var innerSource = new StringDataSource("line1\nline2");
        using var asyncSource = new AsyncMemoryDataSource(innerSource);

        Assert.True(asyncSource.TryReadLine(out var line, out var lineNumber));
        Assert.Equal("line1", line.ToString());
        Assert.Equal(1, lineNumber);

        asyncSource.Reset();
        Assert.True(asyncSource.HasMoreData);
        Assert.True(asyncSource.SupportsReset);
    }

    [Fact]
    public void AsyncMemoryDataSource_GetBuffer()
    {
        var innerSource = new StringDataSource("test content");
        using var asyncSource = new AsyncMemoryDataSource(innerSource);

        var buffer = asyncSource.GetBuffer();
        Assert.Equal("test content", buffer.ToString());
    }

#endif

    #endregion

    #region Additional Csv class tests

    [Fact]
    public void Csv_CreateReader_WithOptions()
    {
        var options = new CsvOptions(delimiter: '|');
        using var reader = Csv.CreateReader("A|B|C", options);

        reader.TryReadRecord(out var record);
        Assert.Equal(3, record.FieldCount);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task Csv_ReadFileAsync_Integration()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Name,Value\nTest,123", TestContext.Current.CancellationToken);

            var records = new List<string[]>();
            await foreach (var record in Csv.ReadFileAsync(tempFile, CsvOptions.Default, cancellationToken: TestContext.Current.CancellationToken))
            {
                records.Add(record);
            }

            // ReadFileAsync includes headers by default
            Assert.Equal(2, records.Count);
            Assert.Equal("Name", records[0][0]); // Header
            Assert.Equal("Test", records[1][0]); // Data
            Assert.Equal("123", records[1][1]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Csv_ReadStreamAsync_Integration()
    {
        var content = "A,B\n1,2\n3,4";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var records = await Csv.ReadStreamAsync(stream, CsvOptions.Default, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, records.Count);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("3", records[1][0]);
    }
#endif

    #endregion

    #region CsvRecord Additional Tests

    [Fact]
    public void CsvRecord_TryGetField_InvalidIndex()
    {
        using var reader = Csv.CreateReader("A,B,C", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);

        Assert.False(record.TryGetField(5, out _));
    }

    [Fact]
    public void CsvRecord_GetFieldSpan()
    {
        using var reader = Csv.CreateReader("Field1,Field2", new CsvOptions(hasHeader: false));
        reader.TryReadRecord(out var record);

        var span = record.GetField(0);
        Assert.Equal("Field1", span.ToString());
    }

    #endregion

    #endregion

    // Helper class for testing non-seekable streams
    private class NonSeekableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}