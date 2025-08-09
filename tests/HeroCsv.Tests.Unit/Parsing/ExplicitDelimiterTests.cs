using System.Linq;
using Xunit;
using HeroCsv;
using HeroCsv.Models;

namespace HeroCsv.Tests.Unit.Parsing;

public class ExplicitDelimiterTests
{
    [Fact]
    public void ParseWithCommaDelimiter_StandardCsv_ParsesCorrectly()
    {
        // Arrange - Most common case: comma delimiter
        var csvContent = "name,age,city\nJohn,30,NYC\nJane,25,LA";
        var options = new CsvOptions(',', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("30", records[0][1]);
        Assert.Equal("NYC", records[0][2]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("25", records[1][1]);
        Assert.Equal("LA", records[1][2]);
    }

    [Fact]
    public void ParseWithSemicolonDelimiter_EuropeanFormat_ParsesCorrectly()
    {
        // Arrange - Common in European locales
        var csvContent = "product;price;quantity\nApple;1,50;100\nBanana;0,75;150";
        var options = new CsvOptions(';', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Apple", records[0][0]);
        Assert.Equal("1,50", records[0][1]); // European decimal notation preserved
        Assert.Equal("100", records[0][2]);
        Assert.Equal("Banana", records[1][0]);
        Assert.Equal("0,75", records[1][1]);
        Assert.Equal("150", records[1][2]);
    }

    [Fact]
    public void ParseWithTabDelimiter_TsvFormat_ParsesCorrectly()
    {
        // Arrange - Tab-separated values
        var csvContent = "id\tname\temail\nU001\tAlice\talice@example.com\nU002\tBob\tbob@example.com";
        var options = new CsvOptions('\t', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("U001", records[0][0]);
        Assert.Equal("Alice", records[0][1]);
        Assert.Equal("alice@example.com", records[0][2]);
        Assert.Equal("U002", records[1][0]);
        Assert.Equal("Bob", records[1][1]);
        Assert.Equal("bob@example.com", records[1][2]);
    }

    [Fact]
    public void ParseWithPipeDelimiter_DatabaseExport_ParsesCorrectly()
    {
        // Arrange - Common in database exports
        var csvContent = "order_id|customer|total|status\n1001|ACME Corp|2500.00|Shipped\n1002|Globex Inc|1750.50|Processing";
        var options = new CsvOptions('|', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("1001", records[0][0]);
        Assert.Equal("ACME Corp", records[0][1]);
        Assert.Equal("2500.00", records[0][2]);
        Assert.Equal("Shipped", records[0][3]);
    }

    [Fact]
    public void CommaDelimiter_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "name,description,price\n\"Widget, Deluxe\",\"A premium widget with extra features\",99.99\n\"Gadget\",\"Simple, effective tool\",49.99";
        var options = new CsvOptions(',', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Widget, Deluxe", records[0][0]); // Comma inside quotes preserved
        Assert.Equal("A premium widget with extra features", records[0][1]);
        Assert.Equal("Simple, effective tool", records[1][1]); // Comma inside quotes preserved
    }

    [Fact]
    public void SemicolonDelimiter_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "\"name;with;semicolons\";value;status\n\"Test;Data\";100;\"Active;Running\"\n\"Plain\";200;\"Stopped\"";
        var options = new CsvOptions(';', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Test;Data", records[0][0]); // Semicolons inside quotes preserved
        Assert.Equal("100", records[0][1]);
        Assert.Equal("Active;Running", records[0][2]);
        Assert.Equal("Plain", records[1][0]);
        Assert.Equal("200", records[1][1]);
        Assert.Equal("Stopped", records[1][2]);
    }

    [Fact]
    public void TabDelimiter_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "\"field\twith\ttabs\"\tvalue\tstatus\n\"Data\tTabbed\"\t123\t\"Status\tInfo\"\nPlain\t456\tSimple";
        var options = new CsvOptions('\t', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("Data\tTabbed", records[0][0]); // Tabs inside quotes preserved
        Assert.Equal("123", records[0][1]);
        Assert.Equal("Status\tInfo", records[0][2]);
    }

    [Fact]
    public void CommaDelimiter_WithEmptyFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "a,b,c,d\n1,,3,\n,,,,\n5,6,,8";
        var options = new CsvOptions(',', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][1]); // Empty field
        Assert.Equal("", records[0][3]); // Empty field at end
        Assert.All(records[1], field => Assert.Equal("", field)); // All empty
        Assert.Equal("", records[2][2]); // Empty field in middle
    }

    [Fact]
    public void SemicolonDelimiter_WithEmptyFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "a;b;c\n1;;3\n;;;\n5;6;";
        var options = new CsvOptions(';', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][1]); // Empty field
        Assert.All(records[1], field => Assert.Equal("", field)); // All empty
        Assert.Equal("", records[2][2]); // Empty field at end
    }

    [Fact]
    public void TabDelimiter_WithEmptyFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "a\tb\tc\n1\t\t3\n\t\t\n5\t6\t";
        var options = new CsvOptions('\t', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][1]); // Empty field
        Assert.All(records[1], field => Assert.Equal("", field)); // All empty
        Assert.Equal("", records[2][2]); // Empty field at end
    }

    [Fact]
    public void PipeDelimiter_WithEmptyFields_HandlesCorrectly()
    {
        // Arrange
        var csvContent = "a|b|c\n1||3\n|||\n5|6|";
        var options = new CsvOptions('|', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("", records[0][1]); // Empty field
        Assert.All(records[1], field => Assert.Equal("", field)); // All empty
        Assert.Equal("", records[2][2]); // Empty field at end
    }

    [Fact]
    public void CommaDelimiter_WithEscapedQuotes_HandlesCorrectly()
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

    [Theory]
    [InlineData(',', "a,b,c\n1,2,3", 3)]
    [InlineData(';', "a;b;c\n1;2;3", 3)]
    [InlineData('\t', "a\tb\tc\n1\t2\t3", 3)]
    [InlineData('|', "a|b|c\n1|2|3", 3)]
    public void ExplicitDelimiter_ParsesCorrectFieldCount(char delimiter, string csvContent, int expectedFieldCount)
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

    [Fact]
    public void DifferentDelimiters_WithSameContent_ParseDifferently()
    {
        // Arrange - Content with multiple delimiters
        var csvContent = "a,b;c|d\n1,2;3|4";
        
        // Act & Assert - Comma delimiter
        var commaOptions = new CsvOptions(',', '"', false);
        var commaRecords = Csv.ReadContent(csvContent, commaOptions).ToList();
        Assert.Equal(2, commaRecords.Count); // Two rows
        Assert.Equal(2, commaRecords[0].Length); // Split only on comma: ["a", "b;c|d"]
        Assert.Equal("a", commaRecords[0][0]);
        Assert.Equal("b;c|d", commaRecords[0][1]);
        
        // Act & Assert - Semicolon delimiter
        var semicolonOptions = new CsvOptions(';', '"', false);
        var semicolonRecords = Csv.ReadContent(csvContent, semicolonOptions).ToList();
        Assert.Equal(2, semicolonRecords.Count); // Two rows
        Assert.Equal(2, semicolonRecords[0].Length); // Split only on semicolon: ["a,b", "c|d"]
        Assert.Equal("a,b", semicolonRecords[0][0]);
        Assert.Equal("c|d", semicolonRecords[0][1]);
        
        // Act & Assert - Pipe delimiter
        var pipeOptions = new CsvOptions('|', '"', false);
        var pipeRecords = Csv.ReadContent(csvContent, pipeOptions).ToList();
        Assert.Equal(2, pipeRecords.Count); // Two rows
        Assert.Equal(2, pipeRecords[0].Length); // Split only on pipe: ["a,b;c", "d"]
        Assert.Equal("a,b;c", pipeRecords[0][0]);
        Assert.Equal("d", pipeRecords[0][1]);
        
        // Act & Assert - Tab delimiter (no tabs in content, so no splitting)
        var tabOptions = new CsvOptions('\t', '"', false);
        var tabRecords = Csv.ReadContent(csvContent, tabOptions).ToList();
        Assert.Equal(2, tabRecords.Count); // Two rows
        Assert.Single(tabRecords[0]); // No tabs, so single field: ["a,b;c|d"]
        Assert.Equal("a,b;c|d", tabRecords[0][0]);
    }

    [Fact]
    public void CustomDelimiter_NotInStandardSet_WorksCorrectly()
    {
        // Arrange - Using caret as delimiter (not in standard set)
        var csvContent = "field1^field2^field3\nvalue1^value2^value3";
        var options = new CsvOptions('^', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Single(records);
        Assert.Equal(3, records[0].Length);
        Assert.Equal("value1", records[0][0]);
        Assert.Equal("value2", records[0][1]);
        Assert.Equal("value3", records[0][2]);
    }

    [Fact]
    public void SpaceDelimiter_HandlesCorrectly()
    {
        // Arrange - Space as delimiter (less common but valid)
        var csvContent = "first last age\nJohn Doe 30\nJane Smith 25";
        var options = new CsvOptions(' ', '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Doe", records[0][1]);
        Assert.Equal("30", records[0][2]);
    }

    [Fact]
    public void ExplicitDelimiter_NoHeaders_ParsesAllAsData()
    {
        // Arrange
        var csvContent = "a,b,c\n1,2,3\n4,5,6";
        var options = new CsvOptions(',', '"', false); // hasHeader = false
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(3, records.Count); // All lines are data
        Assert.Equal("a", records[0][0]); // First line treated as data
        Assert.Equal("b", records[0][1]);
        Assert.Equal("c", records[0][2]);
    }

    [Theory]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('\t')]
    [InlineData('|')]
    public void ExplicitDelimiter_LargeDataset_HandlesEfficiently(char delimiter)
    {
        // Arrange - Create larger dataset
        var rows = new System.Collections.Generic.List<string>();
        rows.Add($"id{delimiter}name{delimiter}value");
        
        for (int i = 0; i < 1000; i++)
        {
            rows.Add($"{i}{delimiter}Item{i}{delimiter}{i * 10}");
        }
        
        var csvContent = string.Join("\n", rows);
        var options = new CsvOptions(delimiter, '"', true);
        
        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();
        
        // Assert
        Assert.Equal(1000, records.Count);
        Assert.Equal("999", records[999][0]);
        Assert.Equal("Item999", records[999][1]);
        Assert.Equal("9990", records[999][2]);
    }
}