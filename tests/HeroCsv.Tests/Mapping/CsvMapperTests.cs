using System.Globalization;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Mapping;

public class CsvMapperTests
{
    public class TestModels
    {
        public class Person
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public decimal Salary { get; set; }
            public DateTime? BirthDate { get; set; }
        }

        public class ComplexModel
        {
            public Guid Id { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public double Rating { get; set; }
            public byte Status { get; set; }
            public long Points { get; set; }
        }

        public class NullableModel
        {
            public int? OptionalInt { get; set; }
            public decimal? OptionalDecimal { get; set; }
            public bool? OptionalBool { get; set; }
            public DateTime? OptionalDate { get; set; }
        }
    }

    public class ManualMappingTests
    {
        [Fact]
        public void MapRecord_BasicTypes_MapsCorrectly()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", 0)
                .MapProperty("Age", 1);

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            var record = new[] { "John", "25", "ExtraField" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal(25, person.Age);
            Assert.Equal("", person.LastName); // Not mapped
        }

        [Fact]
        public void MapRecord_WithConverter_AppliesConversion()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", 0)
                .MapProperty("Age", 1, value => int.Parse(value) * 10)
                .MapProperty("IsActive", 2, value => value == "Y");

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            var record = new[] { "John", "3", "Y" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal(30, person.Age); // 3 * 10
            Assert.True(person.IsActive);
        }

        [Fact]
        public void MapRecord_OutOfBoundsIndex_UsesDefaultValue()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", 0)
                .MapProperty("Age", 10); // Out of bounds

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            var record = new[] { "John" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal(0, person.Age); // Default value
        }
    }

    public class ColumnNameMappingTests
    {
        [Fact]
        public void MapRecord_ByColumnName_RequiresHeaders()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", "given_name")
                .MapProperty("LastName", "surname");

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            mapper.SetHeaders(new[] { "given_name", "surname", "age" });

            var record = new[] { "John", "Doe", "25" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal("Doe", person.LastName);
        }

        [Fact]
        public void MapRecord_UnmatchedColumnName_UsesDefaultValue()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", "nonexistent_column");

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            mapper.SetHeaders(new[] { "name", "age" });

            var record = new[] { "John", "25" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("", person.FirstName); // Default value
        }
    }

    public class AutoMappingTests
    {
        [Fact]
        public void MapRecord_AutoMapping_MapsMatchingProperties()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "FirstName", "LastName", "Age", "IsActive", "Salary" });

            var record = new[] { "John", "Doe", "30", "true", "50000.50" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal("Doe", person.LastName);
            Assert.Equal(30, person.Age);
            Assert.True(person.IsActive);
            Assert.Equal(50000.50m, person.Salary);
        }

        [Fact]
        public void MapRecord_AutoMapping_CaseInsensitiveMatch()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "firstname", "lastname", "AGE" });

            var record = new[] { "John", "Doe", "25" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal("Doe", person.LastName);
            Assert.Equal(25, person.Age);
        }
    }

    public class AutoMapWithOverridesTests
    {
        [Fact]
        public void MapRecord_AutoMapWithOverrides_AppliesOverrides()
        {
            // Arrange
            var mapping = CsvMapping.CreateAutoMapWithOverrides<TestModels.Person>()
                .MapProperty("Age", 2); // Override age to different position

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            mapper.SetHeaders(new[] { "FirstName", "LastName", "Age" });

            var record = new[] { "John", "Doe", "30" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.NotNull(person);
            Assert.Equal(30, person.Age); // Should be mapped to index 2
        }

        [Fact]
        public void MapRecord_AutoMapWithConverters_AppliesCustomLogic()
        {
            // Arrange
            var mapping = CsvMapping.CreateAutoMapWithOverrides<TestModels.Person>()
                .MapProperty("Salary", "Salary", value => decimal.Parse(value) * 1.1m); // 10% raise

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            mapper.SetHeaders(new[] { "FirstName", "Salary" });

            var record = new[] { "John", "50000" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal("John", person.FirstName);
            Assert.Equal(55000m, person.Salary); // 50000 * 1.1
        }
    }

    public class TypeConversionTests
    {
        [Theory]
        [InlineData("123", 123)]
        [InlineData("0", 0)]
        [InlineData("-456", -456)]
        public void ConvertValue_IntegerConversion(string input, int expected)
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            var property = typeof(TestModels.Person).GetProperty(nameof(TestModels.Person.Age));

            // Act
            var result = mapper.ConvertValue(input, property!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("True", true)]
        [InlineData("FALSE", false)]
        public void ConvertValue_BooleanConversion(string input, bool expected)
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            var property = typeof(TestModels.Person).GetProperty(nameof(TestModels.Person.IsActive));

            // Act
            var result = mapper.ConvertValue(input, property!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123.45", 123.45)]
        [InlineData("0", 0)]
        [InlineData("-67.89", -67.89)]
        public void ConvertValue_DecimalConversion(string input, decimal expected)
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            var property = typeof(TestModels.Person).GetProperty(nameof(TestModels.Person.Salary));

            // Act
            var result = mapper.ConvertValue(input, property!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertValue_DateTimeConversion()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            var property = typeof(TestModels.Person).GetProperty(nameof(TestModels.Person.BirthDate));
            var dateString = "2023-01-15";

            // Act
            var result = mapper.ConvertValue(dateString, property!);

            // Assert
            Assert.NotNull(result);
            var date = (DateTime?)result;
            Assert.Equal(new DateTime(2023, 1, 15), date);
        }

        [Fact]
        public void ConvertValue_ComplexTypes()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.ComplexModel>(CsvOptions.Default);
            var guidString = "12345678-1234-5678-1234-567812345678";
            var dateTimeOffsetString = "2023-01-15T10:30:00+00:00";

            // Act
            var guidResult = mapper.ConvertValue(guidString, 
                typeof(TestModels.ComplexModel).GetProperty(nameof(TestModels.ComplexModel.Id))!);
            var dateResult = mapper.ConvertValue(dateTimeOffsetString, 
                typeof(TestModels.ComplexModel).GetProperty(nameof(TestModels.ComplexModel.CreatedAt))!);

            // Assert
            Assert.Equal(Guid.Parse(guidString), guidResult);
            Assert.Equal(DateTimeOffset.Parse(dateTimeOffsetString), dateResult);
        }
    }

    public class NullableTypeTests
    {
        [Fact]
        public void MapRecord_NullableTypes_HandlesEmptyValues()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.NullableModel>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "OptionalInt", "OptionalDecimal", "OptionalBool", "OptionalDate" });

            var record = new[] { "", "", "", "" }; // All empty

            // Act
            var model = mapper.MapRecord(record);

            // Assert
            Assert.Null(model.OptionalInt);
            Assert.Null(model.OptionalDecimal);
            Assert.Null(model.OptionalBool);
            Assert.Null(model.OptionalDate);
        }

        [Fact]
        public void MapRecord_NullableTypes_HandlesValues()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.NullableModel>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "OptionalInt", "OptionalDecimal", "OptionalBool", "OptionalDate" });

            var record = new[] { "42", "123.45", "true", "2023-01-15" };

            // Act
            var model = mapper.MapRecord(record);

            // Assert
            Assert.Equal(42, model.OptionalInt);
            Assert.Equal(123.45m, model.OptionalDecimal);
            Assert.True(model.OptionalBool);
            Assert.Equal(new DateTime(2023, 1, 15), model.OptionalDate);
        }
    }

    public class ErrorHandlingTests
    {
        [Fact]
        public void MapRecord_InvalidConversion_UsesDefaultValue()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "Age", "Salary" });

            var record = new[] { "not-a-number", "invalid-decimal" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal(0, person.Age); // Default int
            Assert.Equal(0m, person.Salary); // Default decimal
        }

        [Fact]
        public void MapRecord_ConverterThrows_UsesDefaultValue()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("Age", 0, value => int.Parse(value) / int.Parse("0")); // Will throw

            var mapper = new CsvMapper<TestModels.Person>(mapping);
            var record = new[] { "10" };

            // Act
            var person = mapper.MapRecord(record);

            // Assert
            Assert.Equal(0, person.Age); // Default value after exception
        }
    }

    public class PerformanceOptimizationTests
    {
        [Fact]
        public void SetHeaders_UpdatesIndexMappings()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>()
                .MapProperty("FirstName", "first_name")
                .MapProperty("LastName", "last_name");

            var mapper = new CsvMapper<TestModels.Person>(mapping);

            // Act
            mapper.SetHeaders(new[] { "first_name", "last_name", "age" });

            // Assert - internal state should be optimized
            // The mapper should have converted column names to indices
            var record = new[] { "John", "Doe", "30" };
            var person = mapper.MapRecord(record);
            
            Assert.Equal("John", person.FirstName);
            Assert.Equal("Doe", person.LastName);
        }

        [Fact]
        public void MapRecord_ReusesMapperInstance_EfficientMapping()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.Person>(CsvOptions.Default);
            mapper.SetHeaders(new[] { "FirstName", "Age" });

            var records = new[]
            {
                new[] { "John", "25" },
                new[] { "Jane", "30" },
                new[] { "Bob", "35" }
            };

            // Act
            var people = records.Select(r => mapper.MapRecord(r)).ToList();

            // Assert
            Assert.Equal(3, people.Count);
            Assert.All(people, p => Assert.NotNull(p));
            Assert.Equal("Jane", people[1].FirstName);
            Assert.Equal(35, people[2].Age);
        }
    }
}