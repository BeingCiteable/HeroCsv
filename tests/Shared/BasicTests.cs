using FastCsv;
using System;
using System.Collections.Generic;
using Xunit;

namespace FastCsv.Tests
{
    public class BasicTests
    {
        [Fact]
        public void CsvOptionsDefaultWorks()
        {
            // Arrange & Act
            var options = CsvOptions.Default;

            // Assert
            Assert.Equal(',', options.Delimiter);
            Assert.Equal('"', options.Quote);
            Assert.True(options.HasHeader);
            Assert.False(options.TrimWhitespace);
        }

        [Fact]
        public void CanCreateCsvReader()
        {
            // Arrange
            var csvData = "Name,Age\r\nJohn,25";
            
            // Act
            var reader = new CsvReader(csvData.AsSpan(), CsvOptions.Default);

            // Assert
            Assert.True(reader.HasMoreData);
            Assert.Equal(1, reader.LineNumber);
        }

        [Fact]
        public void CanCreateCsvWriter()
        {
            // Arrange
            using (var pooledWriter = new PooledCsvWriter())
            {
                // Act
                var writer = new CsvWriter(pooledWriter);
                writer.WriteField("test");

                // Assert
                var result = pooledWriter.ToString();
                Assert.Equal("test", result);
            }
        }

        [Fact]
        public void CanReadMultipleRecords()
        {
            // Arrange
            var csvData = "Name,Age\r\nJohn,25\r\nJane,30";
            var reader = new CsvReader(csvData.AsSpan(), CsvOptions.Default);

            // Act & Assert
            Assert.True(reader.HasMoreData);
            var record1 = reader.ReadRecord();
            
            Assert.True(reader.HasMoreData);
            var record2 = reader.ReadRecord();
            
            Assert.True(reader.HasMoreData);
            var record3 = reader.ReadRecord();
            
            Assert.False(reader.HasMoreData);
        }

        [Fact]
        public void CanWriteMultipleFields()
        {
            // Arrange
            using (var pooledWriter = new PooledCsvWriter())
            {
                var writer = new CsvWriter(pooledWriter);
                
                // Act
                writer.WriteRecord("John", "25");
                writer.WriteRecord("Jane", "30");

                // Assert
                var result = pooledWriter.ToString();
                Assert.Contains("John", result);
                Assert.Contains("25", result);
                Assert.Contains("Jane", result);
                Assert.Contains("30", result);
            }
        }

        [Fact]
        public void CanHandleQuotedFields()
        {
            // Arrange
            var csvData = "Name,Description\r\n\"John Doe\",\"A \"\"quoted\"\" field\"";
            var reader = new CsvReader(csvData.AsSpan(), CsvOptions.Default);

            // Act
            var headerRecord = reader.ReadRecord();
            var dataRecord = reader.ReadRecord();

            // Assert
            Assert.True(reader.HasMoreData || headerRecord.LineNumber > 0);
            Assert.True(dataRecord.LineNumber > 0);
            
            var fields = new List<string>();
            foreach (var field in dataRecord)
            {
                fields.Add(field.ToString());
            }
            
            Assert.Equal(2, fields.Count);
            Assert.Equal("John Doe", fields[0]);
            Assert.Equal("A \"quoted\" field", fields[1]);
        }
    }
}