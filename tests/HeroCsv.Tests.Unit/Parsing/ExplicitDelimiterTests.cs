using System.Linq;
using Xunit;
using HeroCsv;
using HeroCsv.Models;

namespace HeroCsv.Tests.Unit.Parsing;

public class ExplicitDelimiterTests
{
    [Theory]
    [InlineData(',', "name,age,city\nJohn,30,NYC\nJane,25,LA", "John", "30", "NYC")]
    [InlineData(';', "name;age;city\nJohn;30;NYC\nJane;25;LA", "John", "30", "NYC")]
    [InlineData('\t', "name\tage\tcity\nJohn\t30\tNYC\nJane\t25\tLA", "John", "30", "NYC")]
    [InlineData('|', "name|age|city\nJohn|30|NYC\nJane|25|LA", "John", "30", "NYC")]
    public void ParseWithExplicitDelimiter_StandardFormat_ParsesCorrectly(
        char delimiter, string csvContent, string expectedField0, string expectedField1, string expectedField2)
    {
        // Arrange
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal(expectedField0, records[0][0]);
        Assert.Equal(expectedField1, records[0][1]);
        Assert.Equal(expectedField2, records[0][2]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("25", records[1][1]);
        Assert.Equal("LA", records[1][2]);
    }

    [Theory]
    [InlineData(',', "\"field,with,commas\",normal,\"quoted\"\n\"data,here\",value,\"text\"", "data,here", "value", "text")]
    [InlineData(';', "\"field;with;semicolons\";normal;\"quoted\"\n\"data;here\";value;\"text\"", "data;here", "value", "text")]
    [InlineData('\t', "\"field\twith\ttabs\"\tnormal\t\"quoted\"\n\"data\there\"\tvalue\t\"text\"", "data\there", "value", "text")]
    [InlineData('|', "\"field|with|pipes\"|normal|\"quoted\"\n\"data|here\"|value|\"text\"", "data|here", "value", "text")]
    public void ParseWithQuotedFields_DelimiterInsideQuotes_PreservesContent(
        char delimiter, string csvContent, string expectedField0, string expectedField1, string expectedField2)
    {
        // Arrange
        var options = new CsvOptions(delimiter, '"', true); // hasHeader = true, so first line is header
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Single(records); // Only one data row (header is skipped)
        Assert.Equal(expectedField0, records[0][0]); // Delimiter inside quotes preserved
        Assert.Equal(expectedField1, records[0][1]);
        Assert.Equal(expectedField2, records[0][2]);
    }

    [Theory]
    [InlineData(',', "a,b,c\n1,,3\n,,,\n5,6,", 1)]
    [InlineData(';', "a;b;c\n1;;3\n;;;\n5;6;", 1)]
    [InlineData('\t', "a\tb\tc\n1\t\t3\n\t\t\n5\t6\t", 1)]
    [InlineData('|', "a|b|c\n1||3\n|||\n5|6|", 1)]
    public void ParseWithEmptyFields_HandlesCorrectly(char delimiter, string csvContent, int emptyFieldIndex)
    {
        // Arrange
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][emptyFieldIndex]); // Empty field at index
        Assert.All(records[1], field => Assert.Equal("", field)); // All empty row
        Assert.Equal("", records[2][2]); // Empty field at end
    }

    [Theory]
    [InlineData(',', "a,b,c\n1,2,3", 3)]
    [InlineData(';', "a;b;c\n1;2;3", 3)]
    [InlineData('\t', "a\tb\tc\n1\t2\t3", 3)]
    [InlineData('|', "a|b|c\n1|2|3", 3)]
    [InlineData('^', "a^b^c\n1^2^3", 3)] // Custom delimiter
    [InlineData(' ', "a b c\n1 2 3", 3)] // Space delimiter
    public void ParseWithVariousDelimiters_ParsesCorrectFieldCount(
        char delimiter, string csvContent, int expectedFieldCount)
    {
        // Arrange
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Single(records);
        Assert.Equal(expectedFieldCount, records[0].Length);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("2", records[0][1]);
        Assert.Equal("3", records[0][2]);
    }

    [Theory]
    [InlineData(',', false, 3)] // All lines are data
    [InlineData(',', true, 2)]  // First line is header
    [InlineData(';', false, 3)]
    [InlineData(';', true, 2)]
    [InlineData('\t', false, 3)]
    [InlineData('\t', true, 2)]
    [InlineData('|', false, 3)]
    [InlineData('|', true, 2)]
    public void ParseWithHeaderOption_HandlesCorrectly(char delimiter, bool hasHeader, int expectedRecordCount)
    {
        // Arrange
        var csvContent = delimiter switch
        {
            ',' => "a,b,c\n1,2,3\n4,5,6",
            ';' => "a;b;c\n1;2;3\n4;5;6",
            '\t' => "a\tb\tc\n1\t2\t3\n4\t5\t6",
            '|' => "a|b|c\n1|2|3\n4|5|6",
            _ => throw new System.ArgumentException("Unsupported delimiter")
        };
        var options = new CsvOptions(delimiter, '"', hasHeader);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(expectedRecordCount, records.Count);
        if (!hasHeader)
        {
            Assert.Equal("a", records[0][0]); // First line treated as data
        }
        else
        {
            Assert.Equal("1", records[0][0]); // First line skipped as header
        }
    }

    [Theory]
    [InlineData(',', 100)]
    [InlineData(',', 500)]
    [InlineData(',', 1000)]
    [InlineData(';', 100)]
    [InlineData(';', 500)]
    [InlineData(';', 1000)]
    [InlineData('\t', 100)]
    [InlineData('\t', 500)]
    [InlineData('\t', 1000)]
    [InlineData('|', 100)]
    [InlineData('|', 500)]
    [InlineData('|', 1000)]
    public void ParseLargeDataset_HandlesEfficiently(char delimiter, int rowCount)
    {
        // Arrange - Create dataset
        var rows = new System.Collections.Generic.List<string>();
        var delim = delimiter.ToString();
        rows.Add($"id{delim}name{delim}value");
        
        for (int i = 0; i < rowCount; i++)
        {
            rows.Add($"{i}{delim}Item{i}{delim}{i * 10}");
        }
        
        var csvContent = string.Join("\n", rows);
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(rowCount, records.Count);
        var lastIndex = rowCount - 1;
        Assert.Equal(lastIndex.ToString(), records[lastIndex][0]);
        Assert.Equal($"Item{lastIndex}", records[lastIndex][1]);
        Assert.Equal((lastIndex * 10).ToString(), records[lastIndex][2]);
    }

    [Fact]
    public void ParseWithCommaDelimiter_EscapedQuotes_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "title,quote,author\n\"The \"\"Great\"\" Book\",\"He said, \"\"Hello!\"\"\",\"John Doe\"";
        var options = new CsvOptions(',', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Single(records);
        Assert.Equal("The \"Great\" Book", records[0][0]); // Escaped quotes handled
        Assert.Equal("He said, \"Hello!\"", records[0][1]); // Escaped quotes with comma
        Assert.Equal("John Doe", records[0][2]);
    }

    [Fact]
    public void ParseWithSemicolonDelimiter_EuropeanFormat_PreservesDecimalComma()
    {
        // Arrange - Common in European locales with comma as decimal separator
        var csvContent = "product;price;quantity\nApple;1,50;100\nBanana;0,75;150";
        var options = new CsvOptions(';', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Apple", records[0][0]);
        Assert.Equal("1,50", records[0][1]); // European decimal notation preserved
        Assert.Equal("100", records[0][2]);
    }

    [Theory]
    [InlineData('|', "name|desc\n\"Product|1\"|\"A pipe | in text\"", "Product|1", "A pipe | in text")]
    [InlineData('|', "a|b\n\"|\"|\"||\"", "|", "||")]
    [InlineData('|', "field1|field2\n\"|start\"|\"end|\"", "|start", "end|")]
    [InlineData(',', "name,desc\n\"Product,1\",\"A comma , in text\"", "Product,1", "A comma , in text")]
    [InlineData(';', "name;desc\n\"Product;1\";\"A semicolon ; in text\"", "Product;1", "A semicolon ; in text")]
    public void Delimiter_EdgeCases_QuotedDelimitersHandledCorrectly(char delimiter, string csvContent, string expected0, string expected1)
    {
        // Arrange - Edge cases with delimiters inside quotes
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Single(records);
        Assert.Equal(expected0, records[0][0]);
        Assert.Equal(expected1, records[0][1]);
    }

    [Fact]
    public void DifferentDelimiters_WithSameContent_ParseDifferently()
    {
        // Arrange - Content with multiple potential delimiters
        var csvContent = "a,b;c|d\n1,2;3|4";
        
        // Act & Assert - Comma delimiter
        var commaOptions = new CsvOptions(',', '"', false);
        var commaRecords = Csv.ReadContent(csvContent, commaOptions).ToList();
        Assert.Equal(2, commaRecords[0].Length); // ["a", "b;c|d"]
        Assert.Equal("b;c|d", commaRecords[0][1]);
        
        // Act & Assert - Semicolon delimiter
        var semicolonOptions = new CsvOptions(';', '"', false);
        var semicolonRecords = Csv.ReadContent(csvContent, semicolonOptions).ToList();
        Assert.Equal(2, semicolonRecords[0].Length); // ["a,b", "c|d"]
        Assert.Equal("a,b", semicolonRecords[0][0]);
        
        // Act & Assert - Pipe delimiter
        var pipeOptions = new CsvOptions('|', '"', false);
        var pipeRecords = Csv.ReadContent(csvContent, pipeOptions).ToList();
        Assert.Equal(2, pipeRecords[0].Length); // ["a,b;c", "d"]
        Assert.Equal("a,b;c", pipeRecords[0][0]);
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData("a,b,c\n1,2,3", ',')]
    [InlineData("a;b;c\n1;2;3", ';')]
    [InlineData("a\tb\tc\n1\t2\t3", '\t')]
    [InlineData("a|b|c\n1|2|3", '|')]
    public void AutoDetect_VariousFormats_DetectsCorrectDelimiter(string csvContent, char expectedDelimiter)
    {
        // Act - ReadAutoDetect uses auto-detection internally
        var records = Csv.ReadAutoDetect(csvContent).ToList();
        
        // Assert - Should parse correctly with auto-detected delimiter
        Assert.Single(records); // One data row after header
        Assert.Equal(3, records[0].Length);
        Assert.Equal("1", records[0][0]);
        Assert.Equal("2", records[0][1]);
        Assert.Equal("3", records[0][2]);
    }

    [Fact]
    public void AutoDetect_MixedDelimiters_ChoosesMostFrequent()
    {
        // Arrange - Test cases with different delimiter counts
        var csvWithMorePipes = "id|name,age;city\ttab\n1|John,30;NYC\ttab\n2|Jane,25;LA\ttab";
        var csvWithMoreCommas = "id,name|age;city\ttab\n1,John|30;NYC\ttab\n2,Jane|25;LA\ttab";
        var csvWithMoreSemicolons = "id;name|age,city\ttab\n1;John|30,NYC\ttab\n2;Jane|25,LA\ttab";
        var csvWithMoreTabs = "id\tname|age,city;tab\n1\tJohn|30,NYC;tab\n2\tJane|25,LA;tab";
        
        // Act & Assert - Should choose delimiter with highest count
        var pipeRecords = Csv.ReadAutoDetect(csvWithMorePipes).ToList();
        Assert.Equal(2, pipeRecords.Count);
        Assert.Equal(2, pipeRecords[0].Length); // Split by pipe (most frequent)
        
        var commaRecords = Csv.ReadAutoDetect(csvWithMoreCommas).ToList();
        Assert.Equal(2, commaRecords.Count);
        Assert.Equal(2, commaRecords[0].Length); // Split by comma (most frequent)
        
        var semicolonRecords = Csv.ReadAutoDetect(csvWithMoreSemicolons).ToList();
        Assert.Equal(2, semicolonRecords.Count);
        Assert.Equal(2, semicolonRecords[0].Length); // Split by semicolon (most frequent)
        
        var tabRecords = Csv.ReadAutoDetect(csvWithMoreTabs).ToList();
        Assert.Equal(2, tabRecords.Count);
        Assert.Equal(2, tabRecords[0].Length); // Split by tab (most frequent)
    }
#endif
}