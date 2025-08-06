using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Mapping;

public class CsvMappingTests
{
    public class TestModels
    {
        public class Person
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int Age { get; set; }
            public DateTime? BirthDate { get; set; }
            public bool IsActive { get; set; }
            public decimal Salary { get; set; }
        }

        public class SimpleModel
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }
    }

    public class CreateTests
    {
        [Fact]
        public void Create_ReturnsEmptyMapping()
        {
            // Act
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Assert
            Assert.NotNull(mapping);
            Assert.Empty(mapping.PropertyMappings);
            Assert.False(mapping.UseAutoMapWithOverrides);
        }

        [Fact]
        public void CreateAutoMapWithOverrides_SetsFlag()
        {
            // Act
            var mapping = CsvMapping.CreateAutoMapWithOverrides<TestModels.Person>();

            // Assert
            Assert.NotNull(mapping);
            Assert.True(mapping.UseAutoMapWithOverrides);
        }
    }

    public class MapPropertyTests
    {
        public class ByName
        {
            [Fact]
            public void MapProperty_ByName_AddsMapping()
            {
                // Arrange
                var mapping = CsvMapping.Create<TestModels.Person>();

                // Act
                mapping.MapProperty("FirstName", "first_name")
                       .MapProperty("LastName", "last_name");

                // Assert
                Assert.Equal(2, mapping.PropertyMappings.Count);
                Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
                Assert.Equal("first_name", mapping.PropertyMappings[0].ColumnName);
                Assert.Null(mapping.PropertyMappings[0].ColumnIndex);
            }

            [Fact]
            public void MapProperty_WithConverter_StoresConverter()
            {
                // Arrange
                var mapping = CsvMapping.Create<TestModels.Person>();

                // Act
                mapping.MapProperty("Age", "age", value => int.Parse(value) * 2);

                // Assert
                Assert.Single(mapping.PropertyMappings);
                Assert.NotNull(mapping.PropertyMappings[0].Converter);
                
                // Test the converter
                var result = mapping.PropertyMappings[0].Converter!("10");
                Assert.Equal(20, result);
            }
        }

        public class ByIndex
        {
            [Fact]
            public void MapProperty_ByIndex_AddsMapping()
            {
                // Arrange
                var mapping = CsvMapping.Create<TestModels.Person>();

                // Act
                mapping.MapProperty("FirstName", 0)
                       .MapProperty("LastName", 1);

                // Assert
                Assert.Equal(2, mapping.PropertyMappings.Count);
                Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
                Assert.Equal(0, mapping.PropertyMappings[0].ColumnIndex);
                Assert.Null(mapping.PropertyMappings[0].ColumnName);
            }

            [Fact]
            public void MapProperty_WithConverter_HandlesEmptyValue()
            {
                // Arrange
                var mapping = CsvMapping.Create<TestModels.Person>();

                // Act
                mapping.MapProperty("Age", 2, value => string.IsNullOrEmpty(value) ? 0 : int.Parse(value));

                // Assert
                Assert.Single(mapping.PropertyMappings);
                Assert.NotNull(mapping.PropertyMappings[0].Converter);
                
                // Test the converter with empty value
                var result = mapping.PropertyMappings[0].Converter!("");
                Assert.Equal(0, result);
            }
        }
    }

    public class TypeSafeMapTests
    {
        [Fact]
        public void Map_ByExpression_WithColumnName()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.Map(p => p.FirstName, "first_name")
                   .Map(p => p.Age, "age");

            // Assert
            Assert.Equal(2, mapping.PropertyMappings.Count);
            Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
            Assert.Equal("first_name", mapping.PropertyMappings[0].ColumnName);
        }

        [Fact]
        public void Map_ByExpression_WithColumnIndex()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.Map(p => p.FirstName, 0)
                   .Map(p => p.Age, 1);

            // Assert
            Assert.Equal(2, mapping.PropertyMappings.Count);
            Assert.Equal("FirstName", mapping.PropertyMappings[0].PropertyName);
            Assert.Equal(0, mapping.PropertyMappings[0].ColumnIndex);
        }

        [Fact]
        public void Map_WithTypeSafeConverter()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.Map(p => p.Age, 0, int.Parse)
                   .Map(p => p.Salary, 1, decimal.Parse)
                   .Map(p => p.IsActive, 2, bool.Parse);

            // Assert
            Assert.Equal(3, mapping.PropertyMappings.Count);
            
            // All converters should be set
            Assert.All(mapping.PropertyMappings, m => Assert.NotNull(m.Converter));
        }
    }

    public class ConfigurationTests
    {
        [Fact]
        public void EnableAutoMapWithOverrides_SetsFlag()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            var result = mapping.EnableAutoMapWithOverrides();

            // Assert
            Assert.True(mapping.UseAutoMapWithOverrides);
            Assert.Same(mapping, result); // Fluent interface
        }

        [Fact]
        public void WithOptions_SetsOptions()
        {
            // Arrange
            var options = new CsvOptions(delimiter: '|');
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.Options = options;

            // Assert
            Assert.Equal('|', mapping.Options.Delimiter);
        }

        [Fact]
        public void FluentInterface_ChainsCorrectly()
        {
            // Act
            var mapping = CsvMapping.Create<TestModels.Person>()
                .EnableAutoMapWithOverrides()
                .MapProperty("FirstName", 0)
                .Map(p => p.LastName, 1)
                .Map(p => p.Age, 2, int.Parse);

            // Assert
            Assert.True(mapping.UseAutoMapWithOverrides);
            Assert.Equal(3, mapping.PropertyMappings.Count);
        }
    }

    public class PropertyManagementTests
    {
        [Fact]
        public void IgnoreProperty_AddsToIgnoredSet()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.IgnoreProperty("FirstName");
            mapping.IgnoreProperty("LastName");

            // Assert
            Assert.True(mapping.IsPropertyIgnored("FirstName"));
            Assert.True(mapping.IsPropertyIgnored("LastName"));
            Assert.False(mapping.IsPropertyIgnored("Age"));
        }

        [Fact]
        public void SetDefault_StoresDefaultValue()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.SetDefault("Age", 18);
            mapping.SetDefault("IsActive", true);

            // Assert
            Assert.True(mapping.TryGetDefault("Age", out var ageDefault));
            Assert.Equal(18, ageDefault);
            
            Assert.True(mapping.TryGetDefault("IsActive", out var activeDefault));
            Assert.Equal(true, activeDefault);
        }

        [Fact]
        public void SetRequired_MarksPropertyAsRequired()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.SetRequired("FirstName", true);
            mapping.SetRequired("LastName", true);
            mapping.SetRequired("Age", false);

            // Assert
            Assert.True(mapping.IsPropertyRequired("FirstName"));
            Assert.True(mapping.IsPropertyRequired("LastName"));
            Assert.False(mapping.IsPropertyRequired("Age"));
        }

        [Fact]
        public void SetFormat_StoresFormatString()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.SetFormat("BirthDate", "yyyy-MM-dd");
            mapping.SetFormat("Salary", "C2");

            // Assert
            Assert.True(mapping.TryGetFormat("BirthDate", out var dateFormat));
            Assert.Equal("yyyy-MM-dd", dateFormat);
            
            Assert.True(mapping.TryGetFormat("Salary", out var salaryFormat));
            Assert.Equal("C2", salaryFormat);
        }

        [Fact]
        public void SetConverter_UpdatesExistingMapping()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();
            mapping.MapProperty("Age", 0);

            // Act
            mapping.SetConverter("Age", value => int.Parse(value) * 10);

            // Assert
            var ageMapping = mapping.PropertyMappings.Single(m => m.PropertyName == "Age");
            Assert.NotNull(ageMapping.Converter);
            Assert.Equal(100, ageMapping.Converter!("10"));
        }

        [Fact]
        public void SetConverter_CreatesNewMappingIfNotExists()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.Person>();

            // Act
            mapping.SetConverter("Age", value => int.Parse(value) * 10);

            // Assert
            Assert.Single(mapping.PropertyMappings);
            var ageMapping = mapping.PropertyMappings[0];
            Assert.Equal("Age", ageMapping.PropertyName);
            Assert.NotNull(ageMapping.Converter);
        }
    }
}