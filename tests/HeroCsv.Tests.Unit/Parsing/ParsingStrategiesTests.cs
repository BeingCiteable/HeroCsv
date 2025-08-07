using System;
using System.Linq;
using Xunit;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Core;

namespace HeroCsv.Tests.Unit.Parsing
{
    public class ParsingStrategiesTests
    {
        [Fact]
        public void SimpleCommaParsingStrategy_CanHandle_SimpleCommaLine()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "field1,field2,field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_CannotHandle_LineWithQuotes()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "field1,\"field2\",field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_CannotHandle_TrimWhitespace()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: true);
            var line = "field1, field2, field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_CannotHandle_NonCommaDelimiter()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ';', trimWhitespace: false);
            var line = "field1;field2;field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Parse_SimpleFields()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "John,25,New York".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("25", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Parse_EmptyFields()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "John,,New York".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Parse_TrailingComma()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "John,25,".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("25", fields[1]);
            Assert.Equal("", fields[2]);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Parse_SingleField()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "OnlyField".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Single(fields);
            Assert.Equal("OnlyField", fields[0]);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Parse_EmptyLine()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Empty(fields);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_CanHandle_LineWithQuotes()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "field1,\"field2\",field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_CanHandle_TrimWhitespace()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: true);
            var line = "field1, field2, field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_QuotedFields()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "\"John\",\"25\",\"New York\"".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("25", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_EscapedQuotes()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "\"John \"\"JD\"\" Doe\",\"25\",\"New York\"".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John \"JD\" Doe", fields[0]);
            Assert.Equal("25", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_MixedQuotedAndUnquoted()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "John,\"25\",New York".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("25", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_QuotedFieldWithCommas()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "\"Smith, John\",\"Manager, Sales\",\"New York, NY\"".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("Smith, John", fields[0]);
            Assert.Equal("Manager, Sales", fields[1]);
            Assert.Equal("New York, NY", fields[2]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_QuotedFieldWithNewlines()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "\"Line 1\nLine 2\",\"Field 2\"".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(2, fields.Length);
            Assert.Equal("Line 1\nLine 2", fields[0]);
            Assert.Equal("Field 2", fields[1]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_WithTrimWhitespace()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: true);
            var line = " John , \" 25 \" , New York ".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal(" 25 ", fields[1]); // Quotes preserve internal whitespace
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Parse_EmptyQuotedField()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();
            var options = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);
            var line = "John,\"\",New York".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("John", fields[0]);
            Assert.Equal("", fields[1]);
            Assert.Equal("New York", fields[2]);
        }

        [Fact]
        public void ParsingStrategySelector_SelectsCorrectStrategy()
        {
            // Arrange
            var selector = new ParsingStrategySelector();
            var simpleOptions = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var quotedOptions = new CsvOptions(delimiter: ',', quote: '"', trimWhitespace: false);

            // Act & Assert - Simple line should use simple strategy
            var simpleFields = selector.ParseLine("John,25,NYC".AsSpan(), simpleOptions);
            Assert.Equal(3, simpleFields.Length);
            Assert.Equal("John", simpleFields[0]);

            // Act & Assert - Quoted line should use quoted strategy
            var quotedFields = selector.ParseLine("\"John, Jr.\",25,NYC".AsSpan(), quotedOptions);
            Assert.Equal(3, quotedFields.Length);
            Assert.Equal("John, Jr.", quotedFields[0]);
        }

        [Fact]
        public void ParsingStrategySelector_StrategiesOrderedByPriority()
        {
            // Arrange
            var selector = new ParsingStrategySelector();

            // Act
            var strategies = selector.Strategies;

            // Assert
            Assert.True(strategies.Count >= 2);
            // Verify strategies are ordered by priority (highest first)
            for (int i = 0; i < strategies.Count - 1; i++)
            {
                Assert.True(strategies[i].Priority >= strategies[i + 1].Priority,
                    $"Strategy {i} priority ({strategies[i].Priority}) should be >= strategy {i + 1} priority ({strategies[i + 1].Priority})");
            }
        }

        [Fact]
        public void ParsingStrategySelector_EmptyLine_ReturnsEmptyArray()
        {
            // Arrange
            var selector = new ParsingStrategySelector();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);

            // Act
            var fields = selector.ParseLine("".AsSpan(), options);

            // Assert
            Assert.Empty(fields);
        }

        [Fact]
        public void ParsingStrategySelector_FallbackToQuotedStrategy()
        {
            // Arrange
            var selector = new ParsingStrategySelector();
            var options = new CsvOptions(delimiter: ';', quote: '"', trimWhitespace: true);
            var line = " field1 ; \" field2 \" ; field3 ".AsSpan();

            // Act
            var fields = selector.ParseLine(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("field1", fields[0]); // Trimmed
            Assert.Equal(" field2 ", fields[1]); // Quoted, preserves spaces
            Assert.Equal("field3", fields[2]); // Trimmed
        }

        [Theory]
        [InlineData(',')]
        [InlineData(';')]
        [InlineData('|')]
        [InlineData('\t')]
        public void ParsingStrategies_SupportDifferentDelimiters(char delimiter)
        {
            // Arrange
            var selector = new ParsingStrategySelector();
            var options = new CsvOptions(delimiter: delimiter, trimWhitespace: false);
            var line = $"field1{delimiter}field2{delimiter}field3".AsSpan();

            // Act
            var fields = selector.ParseLine(line, options);

            // Assert
            Assert.Equal(3, fields.Length);
            Assert.Equal("field1", fields[0]);
            Assert.Equal("field2", fields[1]);
            Assert.Equal("field3", fields[2]);
        }

        [Fact]
        public void SimpleCommaParsingStrategy_Properties()
        {
            // Arrange
            var strategy = new SimpleCommaParsingStrategy();

            // Assert
            Assert.Equal(100, strategy.Priority);
            Assert.True(strategy.IsAvailable);
        }

        [Fact]
        public void QuotedFieldParsingStrategy_Properties()
        {
            // Arrange
            var strategy = new QuotedFieldParsingStrategy();

            // Assert
            Assert.Equal(50, strategy.Priority);
            Assert.True(strategy.IsAvailable);
        }
    }
}
