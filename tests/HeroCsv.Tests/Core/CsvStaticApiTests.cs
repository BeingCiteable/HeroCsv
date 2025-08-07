using System.Text;
using HeroCsv;
using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Core;

public class CsvStaticApiTests
{
    public class TestModels
    {
        public class Person
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public string City { get; set; } = "";
        }

        public class Employee
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public decimal Salary { get; set; }
            public DateTime HireDate { get; set; }
        }
    }

    public class ReadContentTests
    {
        [Fact]
        public void ReadContent_SimpleCsv_ReturnsCorrectRecords()
        {
            // Arrange
            var csv = """
                Name,Age,City
                John,25,New York
                Jane,30,London
                Bob,35,Paris
                """;

            // Act
            var records = Csv.ReadContent(csv).ToList();

            // Assert
            Assert.Equal(3, records.Count); // Data records only (header skipped by default)
            Assert.Equal(new[] { "John", "25", "New York" }, records[0]);
            Assert.Equal(new[] { "Jane", "30", "London" }, records[1]);
            Assert.Equal(new[] { "Bob", "35", "Paris" }, records[2]);
        }

        [Fact]
        public void ReadContent_WithCustomDelimiter_ParsesCorrectly()
        {
            // Arrange
            var csv = "Name|Age|City\nJohn|25|New York";

            // Act
            var records = Csv.ReadContent(csv, '|').ToList();

            // Assert
            Assert.Single(records); // Data record only (header skipped by default)
            Assert.Equal(new[] { "John", "25", "New York" }, records[0]);
        }

        [Fact]
        public void ReadContent_WithOptions_RespectsConfiguration()
        {
            // Arrange
            var csv = "Name;Age;City\nJohn;25;New York";
            var options = new CsvOptions(delimiter: ';', hasHeader: true);

            // Act
            var records = Csv.ReadContent(csv, options).ToList();

            // Assert
            Assert.Single(records); // Data record only (header skipped by default)
            Assert.Equal(new[] { "John", "25", "New York" }, records[0]);
        }

        [Fact]
        public void ReadContent_EmptyContent_ReturnsEmptyEnumerable()
        {
            // Act
            var records = Csv.ReadContent("").ToList();

            // Assert
            Assert.Empty(records);
        }

        [Fact]
        public void ReadContent_QuotedFields_HandlesCorrectly()
        {
            // Arrange
            var csv = "Name,Description,Price\n" +
                      "\"Product A\",\"A great product with, commas\",19.99\n" +
                      "\"Product B\",\"Another \"\"quoted\"\" product\",29.99";

            // Act
            var records = Csv.ReadContent(csv).ToList();

            // Assert
            Assert.Equal(2, records.Count); // Data records only (header skipped by default)
            Assert.Equal("A great product with, commas", records[0][1]);
            Assert.Equal("Another \"quoted\" product", records[1][1]);
        }
    }

    public class ReadFileTests
    {
        [Fact]
        public void ReadFile_ValidFile_ReturnsRecords()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvContent = "Name,Age\nJohn,25\nJane,30";
            File.WriteAllText(tempFile, csvContent);

            try
            {
                // Act
                var records = Csv.ReadFile(tempFile).ToList();

                // Assert
                Assert.Equal(2, records.Count); // Data records only (header skipped by default)
                Assert.Equal(new[] { "John", "25" }, records[0]);
                Assert.Equal(new[] { "Jane", "30" }, records[1]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadFile_WithOptions_AppliesConfiguration()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvContent = "Name;Age\nJohn;25";
            File.WriteAllText(tempFile, csvContent);
            var options = new CsvOptions(delimiter: ';');

            try
            {
                // Act
                var records = Csv.ReadFile(tempFile, options).ToList();

                // Assert
                Assert.Single(records); // Data record only (header skipped by default)
                Assert.Equal(new[] { "John", "25" }, records[0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                Csv.ReadFile("nonexistent.csv").ToList());
        }
    }

    public class GenericReadTests
    {
        [Fact]
        public void Read_AutoMapping_MapsPropertiesCorrectly()
        {
            // Arrange
            var csv = """
                Name,Age,City
                John,25,New York
                Jane,30,London
                """;

            // Act
            var people = Csv.Read<TestModels.Person>(csv).ToList();

            // Assert
            Assert.Equal(2, people.Count);
            Assert.Equal("John", people[0].Name);
            Assert.Equal(25, people[0].Age);
            Assert.Equal("New York", people[0].City);
            Assert.Equal("Jane", people[1].Name);
            Assert.Equal(30, people[1].Age);
            Assert.Equal("London", people[1].City);
        }

        [Fact]
        public void Read_WithOptions_AppliesConfiguration()
        {
            // Arrange
            var csv = "Name;Age;City\nJohn;25;New York";
            var options = new CsvOptions(delimiter: ';', hasHeader: true);

            // Act
            var people = Csv.Read<TestModels.Person>(csv, options).ToList();

            // Assert
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
            Assert.Equal(25, people[0].Age);
            Assert.Equal("New York", people[0].City);
        }

        [Fact]
        public void Read_WithFluentMapping_UsesCustomMapping()
        {
            // Arrange
            var csv = "PersonName,PersonAge\nJohn,25";

            // Act
            var mapping = CsvMapping.CreateAutoMapWithOverrides<TestModels.Person>()
                .Map(p => p.Name, "PersonName")
                .Map(p => p.Age, "PersonAge");

            var people = Csv.Read<TestModels.Person>(csv, mapping).ToList();

            // Assert
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
            Assert.Equal(25, people[0].Age);
        }
    }

    public class ReadStreamTests
    {
        [Fact]
        public void ReadStream_BasicStream_ReturnsRecords()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,25\nJane,30";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var records = Csv.ReadStream(stream).ToList();

            // Assert
            Assert.Equal(2, records.Count); // Data records only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
            Assert.Equal(new[] { "Jane", "30" }, records[1]);
        }

        [Fact]
        public void ReadStream_WithOptions_AppliesConfiguration()
        {
            // Arrange
            var csvContent = "Name;Age\nJohn;25";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
            var options = new CsvOptions(delimiter: ';');

            // Act
            var records = Csv.ReadStream(stream, options).ToList();

            // Assert
            Assert.Single(records); // Data record only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
        }

        [Fact]
        public void ReadStream_LeaveOpen_PreservesStream()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,25";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var records = Csv.ReadStream(stream, leaveOpen: true).ToList();

            // Assert
            Assert.Single(records); // Data was read
            Assert.True(stream.CanRead); // Stream is still open

            stream.Dispose(); // Manual cleanup
        }

        [Fact]
        public void ReadStream_Generic_MapsToObjects()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,25";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var people = Csv.ReadStream<TestModels.Person>(stream).ToList();

            // Assert
            Assert.Single(people);
            Assert.Equal("John", people[0].Name);
            Assert.Equal(25, people[0].Age);
        }
    }

    public class CountRecordsTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("Single line", 1)]
        [InlineData("Line1\nLine2", 2)]
        [InlineData("Line1\nLine2\n", 2)]
        [InlineData("Header\nData1\nData2", 3)]
        public void CountRecords_VariousInputs_ReturnsCorrectCount(string content, int expectedCount)
        {
            // Act - Use hasHeader: false for raw line counting
            var options = new CsvOptions(hasHeader: false);
            var count = Csv.CountRecords(content, options);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        [Fact]
        public void CountRecords_WithHeader_SubtractsHeader()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25\nJane,30";
            var options = new CsvOptions(hasHeader: true);

            // Act
            var count = Csv.CountRecords(csv, options);

            // Assert
            Assert.Equal(2, count); // 3 lines - 1 header = 2 data records
        }

        [Fact]
        public void CountRecords_ReadOnlySpan_WorksCorrectly()
        {
            // Arrange
            var csv = "Line1\nLine2\nLine3";
            var options = new CsvOptions(hasHeader: false);

            // Act
            var count = Csv.CountRecords(csv.AsSpan(), options);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void CountRecords_ReadOnlyMemory_WorksCorrectly()
        {
            // Arrange
            var csv = "Line1\nLine2\nLine3";
            var options = new CsvOptions(hasHeader: false);

            // Act
            var count = Csv.CountRecords(csv.AsMemory(), options);

            // Assert
            Assert.Equal(3, count);
        }
    }

    public class ReadAllRecordsTests
    {
        [Fact]
        public void ReadAllRecords_String_ReturnsReadOnlyList()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25\nJane,30";

            // Act
            var records = Csv.ReadAllRecords(csv);

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<string[]>>(records);
            Assert.Equal(2, records.Count); // Data records only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
        }

        [Fact]
        public void ReadAllRecords_ReadOnlySpan_WorksCorrectly()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25";

            // Act
            var records = Csv.ReadAllRecords(csv.AsSpan());

            // Assert
            Assert.Single(records); // Data record only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
        }

        [Fact]
        public void ReadAllRecords_ReadOnlyMemory_WorksCorrectly()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25";

            // Act
            var records = Csv.ReadAllRecords(csv.AsMemory());

            // Assert
            Assert.Single(records); // Data record only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
        }
    }

    public class CreateReaderTests
    {
        [Fact]
        public void CreateReader_String_ReturnsWorkingReader()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25";

            // Act
            using var reader = Csv.CreateReader(csv);

            // Assert
            Assert.True(reader.TryReadRecord(out var record));
            Assert.Equal(new[] { "Name", "Age" }, record.ToArray());
        }

        [Fact]
        public void CreateReader_StringWithOptions_AppliesOptions()
        {
            // Arrange
            var csv = "Name;Age\nJohn;25";
            var options = new CsvOptions(delimiter: ';');

            // Act
            using var reader = Csv.CreateReader(csv, options);

            // Assert
            Assert.True(reader.TryReadRecord(out var record));
            Assert.Equal(new[] { "Name", "Age" }, record.ToArray());
        }

        [Fact]
        public void CreateReader_ReadOnlyMemory_WorksCorrectly()
        {
            // Arrange
            var csv = "Name,Age\nJohn,25";

            // Act
            using var reader = Csv.CreateReader(csv.AsMemory());

            // Assert
            Assert.True(reader.TryReadRecord(out var record));
            Assert.Equal(new[] { "Name", "Age" }, record.ToArray());
        }

        [Fact]
        public void CreateReader_Stream_WorksCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,25";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            using var reader = Csv.CreateReader(stream);

            // Assert
            Assert.True(reader.TryReadRecord(out var record));
            Assert.Equal(new[] { "Name", "Age" }, record.ToArray());
        }

        [Fact]
        public void CreateReaderFromFile_ValidFile_WorksCorrectly()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvContent = "Name,Age\nJohn,25";
            File.WriteAllText(tempFile, csvContent);

            try
            {
                // Act
                using var reader = Csv.CreateReaderFromFile(tempFile);

                // Assert
                Assert.True(reader.TryReadRecord(out var record));
                Assert.Equal(new[] { "Name", "Age" }, record.ToArray());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }

    public class ConfigureTests
    {
        [Fact]
        public void Configure_ReturnsBuilder()
        {
            // Act
            var builder = Csv.Configure();

            // Assert
            Assert.NotNull(builder);
        }
    }

#if NET6_0_OR_GREATER
    public class AsyncTests
    {
        [Fact]
        public async Task ReadFileAsync_ValidFile_ReturnsRecords()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvContent = "Name,Age\nJohn,25\nJane,30";
            await File.WriteAllTextAsync(tempFile, csvContent, TestContext.Current.CancellationToken);

            try
            {
                // Act
                var records = await Csv.ReadFileAsync(tempFile, cancellationToken: TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(2, records.Count); // Data records only (header skipped by default)
                Assert.Equal(new[] { "John", "25" }, records[0]);
                Assert.Equal(new[] { "Jane", "30" }, records[1]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ReadFileAsyncEnumerable_ValidFile_YieldsRecords()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvContent = "Name,Age\nJohn,25\nJane,30";
            await File.WriteAllTextAsync(tempFile, csvContent, TestContext.Current.CancellationToken);
            var records = new List<string[]>();

            try
            {
                // Act
                await foreach (var record in Csv.ReadFileAsyncEnumerable(tempFile, cancellationToken: TestContext.Current.CancellationToken))
                {
                    records.Add(record);
                }

                // Assert
                Assert.Equal(2, records.Count); // Data records only (header skipped by default)
                Assert.Equal(new[] { "John", "25" }, records[0]);
                Assert.Equal(new[] { "Jane", "30" }, records[1]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ReadStreamAsync_BasicStream_ReturnsRecords()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,25\nJane,30";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var records = await Csv.ReadStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, records.Count); // Data records only (header skipped by default)
            Assert.Equal(new[] { "John", "25" }, records[0]);
            Assert.Equal(new[] { "Jane", "30" }, records[1]);
        }
    }
#endif
}