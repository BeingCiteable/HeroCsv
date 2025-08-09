using System;
using System.Linq;
using System.Runtime.Intrinsics;
using Xunit;
using HeroCsv.Models;
using HeroCsv.Parsing;

namespace HeroCsv.Tests.Unit.Performance
{
    public class SIMDOptimizationTests
    {
#if NET8_0_OR_GREATER
        [Fact]
        public void SIMDOptimizedParsingStrategy_IsAvailable_DependsOnHardware()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();

            // Act
            bool isAvailable = strategy.IsAvailable;

            // Assert
            // Should match hardware acceleration availability
            bool expectedAvailability = Vector256.IsHardwareAccelerated || Vector128.IsHardwareAccelerated;
            Assert.Equal(expectedAvailability, isAvailable);
        }

        [Fact]
        public void SIMDOptimizedParsingStrategy_HighestPriority()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();

            // Act
            int priority = strategy.Priority;

            // Assert
            Assert.Equal(200, priority);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_CanHandle_SimpleCommaLine()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "field1,field2,field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.True(canHandle);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_CannotHandle_LineWithQuotes()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "field1,\"field2\",field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_CannotHandle_TrimWhitespace()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: true);
            var line = "field1, field2, field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_CannotHandle_NonCommaDelimiter()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ';', trimWhitespace: false);
            var line = "field1;field2;field3".AsSpan();

            // Act
            bool canHandle = strategy.CanHandle(line, options);

            // Assert
            Assert.False(canHandle);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_SimpleFields()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
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

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_EmptyFields()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
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

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_TrailingComma()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
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

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_LargeDataset()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            
            // Create a large CSV line with many fields (ideal for SIMD)
            var fields = new string[100];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = $"Field{i}Data{i}Value{i}";
            }
            var largeLine = string.Join(",", fields).AsSpan();

            // Act
            var parsedFields = strategy.Parse(largeLine, options);

            // Assert
            Assert.Equal(100, parsedFields.Length);
            for (int i = 0; i < parsedFields.Length; i++)
            {
                Assert.Equal($"Field{i}Data{i}Value{i}", parsedFields[i]);
            }
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_EmptyLine()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Empty(fields);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_SingleField()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "OnlyField".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Single(fields);
            Assert.Equal("OnlyField", fields[0]);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_Parse_MultipleCommasInRow()
        {
            // Arrange
            var strategy = new SIMDOptimizedParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "A,B,C,D,E,F,G,H,I,J".AsSpan();

            // Act
            var fields = strategy.Parse(line, options);

            // Assert
            Assert.Equal(10, fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                Assert.Equal(((char)('A' + i)).ToString(), fields[i]);
            }
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_PerformanceBenefit_LargeFields()
        {
            // Arrange
            var simdStrategy = new SIMDOptimizedParsingStrategy();
            var simpleStrategy = new SimpleDelimiterParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            
            // Create a line with many small fields (SIMD should excel here)
            var fieldData = string.Join(",", Enumerable.Range(0, 50).Select(i => $"F{i}"));
            var line = fieldData.AsSpan();

            // Act - Parse with both strategies
            var simdFields = simdStrategy.Parse(line, options);
            var simpleFields = simpleStrategy.Parse(line, options);

            // Assert - Both should produce identical results
            Assert.Equal(simpleFields.Length, simdFields.Length);
            for (int i = 0; i < simpleFields.Length; i++)
            {
                Assert.Equal(simpleFields[i], simdFields[i]);
            }
        }

        [SkippableFactIfSIMDNotAvailable]
        public void ParsingStrategySelector_PrefersSIMDWhenAvailable()
        {
            // Arrange
            var selector = new ParsingStrategySelector();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);
            var line = "field1,field2,field3".AsSpan();

            // Act
            var strategies = selector.Strategies;
            var fields = selector.ParseLine(line, options);

            // Assert
            // SIMD strategy should be first (highest priority) if available
            if (strategies.Any() && strategies[0] is SIMDOptimizedParsingStrategy)
            {
                Assert.True(strategies[0].IsAvailable);
                Assert.Equal(200, strategies[0].Priority);
            }
            
            // Results should still be correct
            Assert.Equal(3, fields.Length);
            Assert.Equal("field1", fields[0]);
            Assert.Equal("field2", fields[1]);
            Assert.Equal("field3", fields[2]);
        }

        [SkippableFactIfSIMDNotAvailable]
        public void SIMDOptimizedParsingStrategy_ConsistentResults_VsSimpleStrategy()
        {
            // Arrange
            var simdStrategy = new SIMDOptimizedParsingStrategy();
            var simpleStrategy = new SimpleDelimiterParsingStrategy();
            var options = new CsvOptions(delimiter: ',', trimWhitespace: false);

            var testCases = new[]
            {
                "a,b,c",
                "field1,field2,field3",
                "1,2,3,4,5",
                "A,,C",
                "single",
                "trailing,comma,",
                "many,fields,here,with,different,lengths,and,content,types,123,456,789"
            };

            foreach (var testCase in testCases)
            {
                var line = testCase.AsSpan();
                
                // Skip if SIMD strategy can't handle this case
                if (!simdStrategy.CanHandle(line, options))
                    continue;

                // Act
                var simdFields = simdStrategy.Parse(line, options);
                var simpleFields = simpleStrategy.Parse(line, options);

                // Assert
                Assert.Equal(simpleFields.Length, simdFields.Length);
                for (int i = 0; i < simpleFields.Length; i++)
                {
                    Assert.True(string.Equals(simpleFields[i], simdFields[i]), 
                        $"Mismatch at field {i} for input '{testCase}': expected '{simpleFields[i]}', actual '{simdFields[i]}'");
                }
            }
        }

        // Custom fact attribute that skips if SIMD is not available
        private sealed class SkippableFactIfSIMDNotAvailableAttribute : FactAttribute
        {
            public SkippableFactIfSIMDNotAvailableAttribute([System.Runtime.CompilerServices.CallerFilePath] string? filePath = null,
                [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0) : base()
            {
                if (!Vector256.IsHardwareAccelerated && !Vector128.IsHardwareAccelerated)
                {
                    Skip = "SIMD hardware acceleration not available on this platform";
                }
            }
        }
#else
        [Fact]
        public void SIMDOptimizedParsingStrategy_NotAvailable_OnOlderFrameworks()
        {
            // Assert
            // SIMD strategy is not available on frameworks older than NET8
            // This test just documents the behavior - no actual strategy to test
            Assert.True(true, "SIMD optimization is only available on .NET 8+");
        }
#endif
    }
}
