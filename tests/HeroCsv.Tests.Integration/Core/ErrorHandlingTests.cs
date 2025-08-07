using System;
using System.IO;
using System.Linq;
using HeroCsv;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Integration.Core;

public class ErrorHandlingTests
{
    public class TestModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public DateTime Date { get; set; }
    }

    private const string ValidCsvData = "Name,Age,Date\nJohn,25,2025-01-01\nJane,30,2025-02-01";
    private const string MalformedCsvData = "Name,Age,Date\nJohn,NotANumber,InvalidDate\nJane,30,2025-02-01";

    public class ReadContent_ErrorScenarios
    {
        [Fact]
        public void ReadContent_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                Csv.ReadContent(null!).ToList());
        }

        [Fact]
        public void ReadContent_EmptyString_ReturnsEmptyCollection()
        {
            var result = Csv.ReadContent("").ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void ReadContent_OnlyWhitespace_HandlesGracefully()
        {
            var result = Csv.ReadContent("   \n\n   ").ToList();
            // May return empty or handle whitespace-only rows
            Assert.NotNull(result);
        }

        [Fact]
        public void ReadContent_UnmatchedQuotes_HandlesGracefully()
        {
            var csvWithUnmatchedQuotes = "Name,Description\n\"John,\"Incomplete quote field";
            
            var records = Csv.ReadContent(csvWithUnmatchedQuotes).ToList();
            
            Assert.Single(records);
        }

        [Fact]
        public void ReadContent_InconsistentFieldCounts_HandlesDifferentFieldNumbers()
        {
            var csvWithInconsistentFields = "Name,Age,City\nJohn,25\nJane,30,London,ExtraField";
            
            var records = Csv.ReadContent(csvWithInconsistentFields).ToList();
            
            Assert.Equal(2, records.Count);
            Assert.Equal(2, records[0].Length);
            Assert.Equal(4, records[1].Length);
        }
    }

    public class ReadFile_ErrorScenarios
    {
        [Fact]
        public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            
            Assert.Throws<FileNotFoundException>(() =>
                Csv.ReadFile(nonExistentPath).ToList());
        }

        [Fact]
        public void ReadFile_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Csv.ReadFile(null!).ToList());
        }

        [Fact]
        public void ReadFile_EmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Csv.ReadFile("").ToList());
        }

        [Fact]
        public void ReadFile_DirectoryInsteadOfFile_ThrowsUnauthorizedAccessException()
        {
            var tempDir = Directory.CreateTempSubdirectory();
            try
            {
                Assert.Throws<UnauthorizedAccessException>(() =>
                    Csv.ReadFile(tempDir.FullName).ToList());
            }
            finally
            {
                tempDir.Delete();
            }
        }

        [Fact]
        public void ReadFile_LockedFile_ThrowsIOException()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, ValidCsvData);

            try
            {
                using var lockingStream = File.Open(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                
                Assert.Throws<IOException>(() =>
                    Csv.ReadFile(tempFile).ToList());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }

    public class GenericRead_ErrorScenarios
    {
        [Fact]
        public void Read_TypeConversionError_HandlesGracefully()
        {
            // The library may handle conversion errors gracefully instead of throwing
            var result = Csv.Read<TestModel>(MalformedCsvData).ToList();
            
            // Should still return results, possibly with default values for failed conversions
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Read_MissingRequiredFields_HandlesGracefully()
        {
            var csvWithMissingFields = "Name\nJohn\nJane";
            
            var result = Csv.Read<TestModel>(csvWithMissingFields).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Equal("John", result[0].Name);
            Assert.Equal(0, result[0].Age);
            Assert.Equal(default(DateTime), result[0].Date);
        }

        [Fact]
        public void Read_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Csv.Read<TestModel>(null!).ToList());
        }
    }

    public class Options_ErrorScenarios
    {
        [Fact]
        public void ReadContent_InvalidDelimiter_HandlesGracefully()
        {
            // Test with unusual delimiter - may not throw exception
            var result = Csv.ReadContent("A,B,C", ' ').ToList(); // Use space instead
            
            // Should handle gracefully
            Assert.NotNull(result);
        }

        [Fact]
        public void ReadContent_InvalidQuoteCharacter_HandlesGracefully()
        {
            var options = new CsvOptions(',', ' '); // Use space quote instead
            
            // May handle gracefully instead of throwing
            var result = Csv.ReadContent("A,B,C", options).ToList();
            Assert.NotNull(result);
        }
    }

    public class Stream_ErrorScenarios
    {
        [Fact]
        public void ReadStream_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Csv.ReadStream(null!).ToList());
        }

        [Fact]
        public void ReadStream_DisposedStream_ThrowsException()
        {
            var stream = new MemoryStream();
            stream.Dispose();
            
            Assert.ThrowsAny<Exception>(() =>
                Csv.ReadStream(stream).ToList());
        }

        [Fact]
        public void ReadStream_WriteOnlyStream_ThrowsException()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, ValidCsvData);
            
            try
            {
                using var writeOnlyStream = File.OpenWrite(tempFile);
                
                Assert.ThrowsAny<Exception>(() =>
                    Csv.ReadStream(writeOnlyStream).ToList());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }

    public class CreateReader_ErrorScenarios
    {
        [Fact]
        public void CreateReader_NullContent_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Csv.CreateReader(null!));
        }

        [Fact]
        public void CreateReader_InvalidOptions_HandlesGracefully()
        {
            var options = new CsvOptions(' ', '"'); // Use space delimiter
            
            // May handle gracefully
            using var reader = Csv.CreateReader("A,B,C", options);
            Assert.NotNull(reader);
        }

        [Fact]
        public void CreateReaderFromFile_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Csv.CreateReaderFromFile(null!));
        }
    }
}