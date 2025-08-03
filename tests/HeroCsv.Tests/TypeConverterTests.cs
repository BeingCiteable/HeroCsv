using System;
using System.Globalization;
using HeroCsv.Mapping.Attributes;
using HeroCsv.Mapping.Converters;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests for custom type converters (ICsvConverter implementations)
/// </summary>
public class TypeConverterTests
{
    #region DateTimeConverter Tests
    
    public class DateTimeModel
    {
        [CsvConverter(typeof(DateTimeConverter))]
        [CsvColumn("Date", Format = "dd/MM/yyyy")]
        public DateTime Date { get; set; }
        
        [CsvConverter(typeof(DateTimeConverter))]
        [CsvColumn("Time", Format = "HH:mm:ss")]
        public DateTime Time { get; set; }
        
        [CsvConverter(typeof(DateTimeConverter))]
        [CsvColumn("Timestamp", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime Timestamp { get; set; }
        
        [CsvConverter(typeof(DateTimeConverter))]
        public DateTime DefaultFormat { get; set; }
    }
    
    public class DateTimeOffsetModel
    {
        [CsvConverter(typeof(DateTimeConverter))]
        [CsvColumn("Offset", Format = "yyyy-MM-dd HH:mm:ss zzz")]
        public DateTimeOffset Offset { get; set; }
        
        [CsvConverter(typeof(DateTimeConverter))]
        public DateTimeOffset? NullableOffset { get; set; }
    }
    
    [Fact]
    public void DateTimeConverter_Should_ParseDates_UsingSpecifiedFormats()
    {
        var csv = @"Date,Time,Timestamp,DefaultFormat
15/03/2023,14:30:45,2023-03-15 14:30:45,2023-03-15 14:30:45
01/01/2024,08:00:00,2024-01-01 08:00:00,2024-01-01 08:00:00";
        
        var results = Csv.Read<DateTimeModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        Assert.Equal(new DateTime(2023, 3, 15), results[0].Date);
        Assert.Equal(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 14, 30, 45), results[0].Time);
        Assert.Equal(new DateTime(2023, 3, 15, 14, 30, 45), results[0].Timestamp);
        Assert.Equal(new DateTime(2023, 3, 15, 14, 30, 45), results[0].DefaultFormat);
    }
    
    [Fact]
    public void DateTimeConverter_Should_ReturnNull_ForEmptyValues()
    {
        var csv = @"Offset,NullableOffset
2023-03-15 14:30:45 +00:00,2023-03-15 14:30:45
,";
        
        var results = Csv.Read<DateTimeOffsetModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.NotNull(results[0].NullableOffset); // Falls back to standard parsing
        Assert.Null(results[1].NullableOffset); // Empty value should be null
    }
    
    [Fact]
    public void DateTimeConverter_Should_ConvertBetweenStringAndDateTime_Correctly()
    {
        var converter = new DateTimeConverter();
        
        // Test with format
        var result1 = converter.ConvertFromString("15/03/2023", typeof(DateTime), "dd/MM/yyyy");
        Assert.Equal(new DateTime(2023, 3, 15), result1);
        
        // Test without format (fallback)
        var result2 = converter.ConvertFromString("2023-03-15", typeof(DateTime), null);
        Assert.Equal(new DateTime(2023, 3, 15), result2);
        
        // Test null/empty
        var result3 = converter.ConvertFromString("", typeof(DateTime?), null);
        Assert.Null(result3);
        
        // Test ConvertToString
        var str1 = converter.ConvertToString(new DateTime(2023, 3, 15), "dd/MM/yyyy");
        Assert.Equal("15/03/2023", str1);
        
        var str2 = converter.ConvertToString(null, null);
        Assert.Equal("", str2);
    }
    
    #endregion
    
    #region BooleanConverter Tests
    
    public class BooleanModel
    {
        [CsvConverter(typeof(BooleanConverter))]
        public bool Standard { get; set; }
        
        [CsvConverter(typeof(BooleanConverter))]
        public bool YesNo { get; set; }
        
        [CsvConverter(typeof(BooleanConverter))]
        public bool OneZero { get; set; }
        
        [CsvConverter(typeof(BooleanConverter))]
        public bool OnOff { get; set; }
        
        [CsvConverter(typeof(BooleanConverter))]
        public bool? Nullable { get; set; }
    }
    
    [Fact]
    public void BooleanConverter_Should_ParseMultipleTextRepresentations_OfBooleanValues()
    {
        var csv = @"Standard,YesNo,OneZero,OnOff,Nullable
true,yes,1,on,true
false,no,0,off,
FALSE,NO,0,OFF,false
True,Yes,1,On,";
        
        var results = Csv.Read<BooleanModel>(csv).ToList();
        
        Assert.Equal(4, results.Count);
        
        // First row - all true
        Assert.True(results[0].Standard);
        Assert.True(results[0].YesNo);
        Assert.True(results[0].OneZero);
        Assert.True(results[0].OnOff);
        Assert.True(results[0].Nullable);
        
        // Second row - all false except nullable (empty)
        Assert.False(results[1].Standard);
        Assert.False(results[1].YesNo);
        Assert.False(results[1].OneZero);
        Assert.False(results[1].OnOff);
        Assert.Null(results[1].Nullable);
        
        // Case insensitive
        Assert.False(results[2].Standard);
        Assert.True(results[3].Standard);
    }
    
    [Fact]
    public void BooleanConverter_Should_AllowCustomTrueFalseValues()
    {
        var converter = new BooleanConverter(new[] { "si", "oui" }, new[] { "no", "non" });
        
        Assert.True((bool)converter.ConvertFromString("si", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("oui", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("no", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("non", typeof(bool))!);
        
        Assert.Throws<FormatException>(() => converter.ConvertFromString("yes", typeof(bool)));
    }
    
    [Fact]
    public void BooleanConverter_Should_HandleAllStandardBooleanFormats()
    {
        var converter = new BooleanConverter();
        
        // Test various true values
        Assert.True((bool)converter.ConvertFromString("true", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("TRUE", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("yes", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("y", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("1", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("on", typeof(bool))!);
        Assert.True((bool)converter.ConvertFromString("enabled", typeof(bool))!);
        
        // Test various false values
        Assert.False((bool)converter.ConvertFromString("false", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("no", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("n", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("0", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("off", typeof(bool))!);
        Assert.False((bool)converter.ConvertFromString("disabled", typeof(bool))!);
        
        // Test ConvertToString
        Assert.Equal("true", converter.ConvertToString(true));
        Assert.Equal("false", converter.ConvertToString(false));
        Assert.Equal("", converter.ConvertToString(null));
    }
    
    #endregion
    
    #region EnumConverter Tests
    
    public enum Color
    {
        Red,
        Green,
        Blue
    }
    
    [Flags]
    public enum FileAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute
    }
    
    public class EnumModel
    {
        [CsvConverter(typeof(EnumConverter))]
        public Color Color { get; set; }
        
        [CsvConverter(typeof(EnumConverter))]
        public Color? NullableColor { get; set; }
        
        [CsvConverter(typeof(EnumConverter))]
        public FileAccess Access { get; set; }
        
        [CsvConverter(typeof(EnumConverter))]
        [CsvColumn("Flags", Format = "Flags")]
        public FileAccess Flags { get; set; }
    }
    
    [Fact]
    public void EnumConverter_Should_ParseEnumNames_AndNumericValues()
    {
        var csv = @"Color,NullableColor,Access,Flags
Red,Green,1,Read|Write
Blue,,4,All
2,Blue,Read,None";
        
        var results = Csv.Read<EnumModel>(csv).ToList();
        
        Assert.Equal(3, results.Count);
        
        // First row
        Assert.Equal(Color.Red, results[0].Color);
        Assert.Equal(Color.Green, results[0].NullableColor);
        Assert.Equal(FileAccess.Read, results[0].Access); // Numeric value
        Assert.Equal(FileAccess.Read | FileAccess.Write, results[0].Flags);
        
        // Second row
        Assert.Equal(Color.Blue, results[1].Color);
        Assert.Null(results[1].NullableColor);
        Assert.Equal(FileAccess.Execute, results[1].Access);
        Assert.Equal(FileAccess.All, results[1].Flags);
        
        // Third row
        Assert.Equal(Color.Blue, results[2].Color); // Numeric value 2
        Assert.Equal(FileAccess.Read, results[2].Access); // Name
        Assert.Equal(FileAccess.None, results[2].Flags);
    }
    
    [Fact]
    public void EnumConverter_Should_HandleFlagsEnums_WithPipeDelimitedValues()
    {
        var converter = new EnumConverter();
        
        // Test name parsing
        Assert.Equal(Color.Red, converter.ConvertFromString("Red", typeof(Color)));
        Assert.Equal(Color.Red, converter.ConvertFromString("red", typeof(Color))); // Case insensitive
        
        // Test numeric parsing
        Assert.Equal(Color.Green, converter.ConvertFromString("1", typeof(Color)));
        
        // Test flags
        var flags = converter.ConvertFromString("Read|Write|Execute", typeof(FileAccess), "Flags");
        Assert.Equal(FileAccess.All, flags);
        
        // Test invalid value
        Assert.Throws<FormatException>(() => converter.ConvertFromString("Yellow", typeof(Color)));
        
        // Test ConvertToString
        Assert.Equal("Red", converter.ConvertToString(Color.Red));
        Assert.Equal("7", converter.ConvertToString(FileAccess.All, "Numeric"));
        Assert.Equal("", converter.ConvertToString(null));
    }
    
    [Fact]
    public void EnumConverter_Should_AcceptUndefinedNumericValues_ForNonFlagsEnums()
    {
        var converter = new EnumConverter();
        
        // Undefined numeric values should work for non-flags enums
        var result = converter.ConvertFromString("99", typeof(Color));
        Assert.Equal((Color)99, result);
    }
    
    #endregion
    
    #region Custom Converter Tests
    
    public class CustomConverter : ICsvConverter
    {
        public object? ConvertFromString(string value, Type targetType, string? format = null)
        {
            if (string.IsNullOrEmpty(value))
                return null;
                
            // Custom logic: prefix with format if provided
            return format != null ? $"{format}:{value}" : $"Custom:{value}";
        }
        
        public string ConvertToString(object? value, string? format = null)
        {
            return value?.ToString() ?? "";
        }
    }
    
    public class CustomConverterModel
    {
        [CsvConverter(typeof(CustomConverter))]
        [CsvColumn("Value1")]
        public string Value1 { get; set; } = "";
        
        [CsvConverter(typeof(CustomConverter))]
        [CsvColumn("Value2", Format = "Special")]
        public string Value2 { get; set; } = "";
    }
    
    [Fact]
    public void CustomConverter_Should_ReceiveFormatParameter_FromAttribute()
    {
        var csv = @"Value1,Value2
Test,Data
,Empty";
        
        var results = Csv.Read<CustomConverterModel>(csv).ToList();
        
        Assert.Equal(2, results.Count);
        
        Assert.Equal("Custom:Test", results[0].Value1);
        Assert.Equal("Special:Data", results[0].Value2);
        
        Assert.Null(results[1].Value1);
        Assert.Equal("Special:Empty", results[1].Value2);
    }
    
    #endregion
}