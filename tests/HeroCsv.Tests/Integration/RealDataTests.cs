using System.IO;
using System.Text;
using HeroCsv.Models;
using HeroCsv.Tests.Utilities;
using Xunit;

namespace HeroCsv.Tests.Integration;

public class RealDataTests
{
    [Fact]
    public void SimpleFile_ReadCorrectly()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Simple), options);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("John", records[0][0]);
        Assert.Equal("30", records[0][1]);
        Assert.Equal("New York", records[0][2]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("Bob", records[2][0]);
    }

    [Fact]
    public void EmployeesFile_ReadCorrectly()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Employees), options);

        // Assert
        Assert.Equal(10, records.Count);
        Assert.Equal("John Smith", records[0][1]); // Name column
        Assert.Equal("Engineering", records[0][2]); // Department column
        Assert.Equal("75000", records[0][3]); // Salary column
        Assert.Equal("true", records[0][5]); // IsActive column
    }

    [Fact]
    public void ProductsFile_HandlesQuotedFields()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Products), options);

        // Assert
        Assert.Equal(10, records.Count);

        // Check quoted field with comma inside
        Assert.Equal("Electronics, Computers", records[0][2]); // Category

        // Check quoted field with escaped quotes
        Assert.Equal("High-performance laptop with \"cutting-edge\" technology", records[0][5]); // Description
    }

    [Fact]
    public void SpecialCharactersFile_HandlesUnicode()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.SpecialCharacters), options);

        // Assert
        Assert.Equal(5, records.Count);
        Assert.Equal("José García", records[0][0]);
        Assert.Equal("李小明", records[1][0]);
        Assert.Equal("François Dubois", records[2][0]);
        Assert.Equal("Müller Schmidt", records[3][0]);
        Assert.Equal("Анна Иванова", records[4][0]);
    }

    [Fact]
    public void EmptyFile_ReturnsEmptyResults()
    {
        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Empty));

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void HeaderOnlyFile_ReturnsEmptyWithHeader()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.HeaderOnly), options);

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void WithEmptyLinesFile_SkipsEmptyLines()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.WithEmptyLines), options);

        // Assert
        Assert.Equal(3, records.Count); // Should skip empty lines
        Assert.Equal("John", records[0][0]);
        Assert.Equal("Jane", records[1][0]);
        Assert.Equal("Bob", records[2][0]);
    }

    [Theory]
    [InlineData(TestDataHelper.Files.Simple, 3)]
    [InlineData(TestDataHelper.Files.Employees, 10)]
    [InlineData(TestDataHelper.Files.Products, 10)]
    [InlineData(TestDataHelper.Files.MixedDataTypes, 10)]
    [InlineData(TestDataHelper.Files.SalesData, 10)]
    public void VariousFiles_CountRecordsCorrectly(string fileName, int expectedCount)
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var recordCount = Csv.CountRecords(TestDataHelper.ReadTestFile(fileName), options);

        // Assert
        Assert.Equal(expectedCount, recordCount);
    }

    [Fact]
    public void LargeDataset_ReadAllRecords()
    {
        // Skip if file doesn't exist (might not be generated)
        if (!TestDataHelper.TestFileExists(TestDataHelper.Files.MediumDataset))
        {
            return;
        }

        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.MediumDataset), options);

        // Assert
        Assert.Equal(1000, records.Count);

        // Verify structure
        Assert.Equal(10, records[0].Length); // Should have 10 columns
        Assert.Contains("@company.com", records[0][3]); // Email format
    }

    [Fact]
    public void DifferentDelimiters_SemicolonDelimited()
    {
        // Arrange
        var options = new CsvOptions(delimiter: ';', hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.DifferentDelimiters), options);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("John Smith", records[0][0]);
        Assert.Equal("30", records[0][1]);
        Assert.Equal("Engineering", records[0][2]);
    }

    [Fact]
    public void PipeDelimited_ReadCorrectly()
    {
        // Arrange
        var options = new CsvOptions(delimiter: '|', hasHeader: true);

        // Act
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.PipeDelimited), options);

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("John Smith", records[0][0]);
        Assert.Equal("30", records[0][1]);
        Assert.Equal("Engineering", records[0][2]);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task AsyncMethods_WithRealFiles_WorkCorrectly()
    {
        // Arrange
        var filePath = TestDataHelper.GetTestFilePath(TestDataHelper.Files.Employees);
        var options = new CsvOptions(hasHeader: true);

        // Act
        var asyncRecords = await Csv.ReadFileAsync(filePath, options, null, CancellationToken.None);
        var syncRecords = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Employees), options);

        // Assert
        Assert.Equal(syncRecords.Count, asyncRecords.Count);

        for (int i = 0; i < syncRecords.Count; i++)
        {
            Assert.Equal(syncRecords[i].Length, asyncRecords[i].Length);
            for (int j = 0; j < syncRecords[i].Length; j++)
            {
                Assert.Equal(syncRecords[i][j], asyncRecords[i][j]);
            }
        }
    }

    [Fact]
    public async Task AsyncEnumerable_WithLargeFile_StreamsCorrectly()
    {
        // Skip if file doesn't exist
        if (!TestDataHelper.TestFileExists(TestDataHelper.Files.MediumDataset))
        {
            return;
        }

        // Arrange
        var filePath = TestDataHelper.GetTestFilePath(TestDataHelper.Files.MediumDataset);
        var options = new CsvOptions(hasHeader: true);

        // Act
        var records = new List<string[]>();
        await foreach (var record in Csv.ReadFileAsyncEnumerable(filePath, options, null, CancellationToken.None))
        {
            records.Add(record);

            // Only process first 100 records for test speed
            if (records.Count >= 100)
                break;
        }

        // Assert
        Assert.Equal(100, records.Count);
        Assert.Equal(10, records[0].Length); // Should have 10 columns
    }
#endif

    [Fact]
    public void TestDataHelper_CanAccessAllFiles()
    {
        // Act
        var files = TestDataHelper.GetAllTestFiles();

        // Assert
        Assert.True(files.Length >= 10); // Should have at least our basic test files

        // Verify some key files exist
        Assert.True(TestDataHelper.TestFileExists(TestDataHelper.Files.Simple));
        Assert.True(TestDataHelper.TestFileExists(TestDataHelper.Files.Employees));
        Assert.True(TestDataHelper.TestFileExists(TestDataHelper.Files.Empty));
    }

    [Fact]
    public void TestDataHelper_GetFileSize_ReturnsCorrectSize()
    {
        // Act
        var size = TestDataHelper.GetTestFileSize(TestDataHelper.Files.Empty);

        // Assert
        Assert.Equal(0, size); // Empty file should be 0 bytes

        // Check non-empty file has positive size
        var employeesSize = TestDataHelper.GetTestFileSize(TestDataHelper.Files.Employees);
        Assert.True(employeesSize > 0);
    }

    [Fact]
    public void MalformedFile_HandlesGracefully()
    {
        // Arrange
        var options = new CsvOptions(hasHeader: true);

        // Act & Assert - Should not throw, but may have inconsistent field counts
        var records = Csv.ReadAllRecords(TestDataHelper.ReadTestFile(TestDataHelper.Files.Malformed), options);

        Assert.True(records.Count > 0); // Should still read some records

        // First record should have correct number of fields
        Assert.Equal(4, records[0].Length);
    }
}