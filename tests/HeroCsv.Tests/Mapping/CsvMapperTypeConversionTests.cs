using System;
using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Mapping;

/// <summary>
/// Tests for CsvMapper's type conversion capabilities with various data types
/// </summary>
public class CsvMapperTypeConversionTests
{
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
        var mapping = CsvMapping.CreateAutoMapWithOverrides<TestModel>();
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
}