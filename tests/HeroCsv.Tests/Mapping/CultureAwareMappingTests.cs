using System.Globalization;
using HeroCsv.Mapping;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Mapping;

public class CultureAwareMappingTests
{
    public class TestModels
    {
        public class LocalizedData
        {
            public decimal Price { get; set; }
            public double Rating { get; set; }
            public int Quantity { get; set; }
            public DateTime Date { get; set; }
            public DateTimeOffset Timestamp { get; set; }
#if NET6_0_OR_GREATER
            public DateOnly EventDate { get; set; }
            public TimeOnly EventTime { get; set; }
#endif
        }
    }

    public class NumberParsingTests
    {
        [Fact]
        public void ConvertValue_DecimalWithCurrentCulture_ParsesCorrectly()
        {
            // Arrange - Use French culture which uses comma as decimal separator
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), frenchCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Price))!;

            // Act
            var result = mapper.ConvertValue("123,45", property); // French format

            // Assert
            Assert.Equal(123.45m, result);
        }

        [Fact]
        public void ConvertValue_DoubleWithGermanCulture_ParsesCorrectly()
        {
            // Arrange - German culture uses comma for decimal and period for thousands
            var germanCulture = CultureInfo.GetCultureInfo("de-DE");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), germanCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Rating))!;

            // Act
            var result = mapper.ConvertValue("4,75", property);

            // Assert
            Assert.Equal(4.75, result);
        }

        [Fact]
        public void ConvertValue_IntegerWithEnglishCulture_ParsesCorrectly()
        {
            // Arrange
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Quantity))!;

            // Act
            var result = mapper.ConvertValue("1,234", property); // English thousands separator

            // Assert
            Assert.Equal(1234, result);
        }
    }

    public class FieldSpecificCultureTests
    {
        [Fact]
        public void SetFieldCulture_OverridesMapperCulture()
        {
            // Arrange - Mapper uses English, but price field uses French
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            
            mapper.SetFieldCulture(nameof(TestModels.LocalizedData.Price), frenchCulture);

            var priceProperty = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Price))!;
            var quantityProperty = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Quantity))!;

            // Act
            var priceResult = mapper.ConvertValue("123,45", priceProperty); // French format
            var quantityResult = mapper.ConvertValue("1234", quantityProperty); // English format

            // Assert
            Assert.Equal(123.45m, priceResult); // Parsed with French culture
            Assert.Equal(1234, quantityResult); // Parsed with English culture
        }

        [Fact]
        public void SetCulture_UpdatesMapperDefaultCulture()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions());
            var germanCulture = CultureInfo.GetCultureInfo("de-DE");
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Rating))!;

            // Act
            mapper.SetCulture(germanCulture);
            var result = mapper.ConvertValue("3,14", property);

            // Assert
            Assert.Equal(3.14, result);
        }
    }

    public class DateTimeParsingTests
    {
        [Fact]
        public void ConvertValue_DateTimeWithCulture_ParsesCorrectly()
        {
            // Arrange - German date format
            var germanCulture = CultureInfo.GetCultureInfo("de-DE");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), germanCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Date))!;

            // Act
            var result = mapper.ConvertValue("15.01.2023", property); // German date format

            // Assert
            var dateTime = (DateTime)result!;
            Assert.Equal(new DateTime(2023, 1, 15), dateTime);
        }

        [Fact]
        public void ConvertValue_DateTimeOffsetWithCulture_ParsesCorrectly()
        {
            // Arrange
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Timestamp))!;

            // Act
            var result = mapper.ConvertValue("1/15/2023 10:30:00 AM +00:00", property);

            // Assert
            var dto = (DateTimeOffset)result!;
            Assert.Equal(new DateTimeOffset(2023, 1, 15, 10, 30, 0, TimeSpan.Zero), dto);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void ConvertValue_DateOnlyWithCulture_ParsesCorrectly()
        {
            // Arrange - French date format
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), frenchCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.EventDate))!;

            // Act
            var result = mapper.ConvertValue("15/01/2023", property); // French date format

            // Assert
            var dateOnly = (DateOnly)result!;
            Assert.Equal(new DateOnly(2023, 1, 15), dateOnly);
        }

        [Fact]
        public void ConvertValue_TimeOnlyWithCulture_ParsesCorrectly()
        {
            // Arrange
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.EventTime))!;

            // Act
            var result = mapper.ConvertValue("2:30:45 PM", property);

            // Assert
            var timeOnly = (TimeOnly)result!;
            Assert.Equal(new TimeOnly(14, 30, 45), timeOnly);
        }
#endif
    }

    public class FormatSpecificParsingTests
    {
        [Fact]
        public void ConvertValue_DateTimeWithCustomFormat_ParsesCorrectly()
        {
            // Arrange
            var mapping = CsvMapping.Create<TestModels.LocalizedData>();
            mapping.SetFormat(nameof(TestModels.LocalizedData.Date), "dd-MM-yyyy");
            
            var germanCulture = CultureInfo.GetCultureInfo("de-DE");
            var mapper = new CsvMapper<TestModels.LocalizedData>(mapping, germanCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Date))!;

            // Act
            var result = mapper.ConvertValue("25-12-2023", property);

            // Assert
            var dateTime = (DateTime)result!;
            Assert.Equal(new DateTime(2023, 12, 25), dateTime);
        }

        [Fact]
        public void ConvertValue_DecimalWithFieldCultureAndFormat_ParsesCorrectly()
        {
            // Arrange
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            
            // Set French culture specifically for the Price field
            mapper.SetFieldCulture(nameof(TestModels.LocalizedData.Price), frenchCulture);

            var priceProperty = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Price))!;
            var ratingProperty = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Rating))!;

            // Act
            var priceResult = mapper.ConvertValue("1 234,56", priceProperty); // French format with space thousands separator
            var ratingResult = mapper.ConvertValue("4.75", ratingProperty); // English format

            // Assert
            Assert.Equal(1234.56m, priceResult); // French parsing
            Assert.Equal(4.75, ratingResult); // English parsing
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void MapRecord_MixedCultures_ParsesAllFieldsCorrectly()
        {
            // Arrange
            var englishCulture = CultureInfo.GetCultureInfo("en-US");
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions(), englishCulture);
            mapper.SetFieldCulture(nameof(TestModels.LocalizedData.Price), frenchCulture);
            mapper.SetFieldCulture(nameof(TestModels.LocalizedData.Date), frenchCulture);
            
            mapper.SetHeaders(new[] { "Price", "Rating", "Quantity", "Date" });

            // Act
            var record = new[] { "1 234,56", "4.75", "1234", "15/01/2023" };
            var result = mapper.MapRecord(record);

            // Assert
            Assert.Equal(1234.56m, result.Price); // French culture
            Assert.Equal(4.75, result.Rating); // English culture
            Assert.Equal(1234, result.Quantity); // English culture
            Assert.Equal(new DateTime(2023, 1, 15), result.Date); // French culture
        }

        [Fact]
        public void MapRecord_DefaultCurrentCulture_UsesSystemCulture()
        {
            // Arrange - This test demonstrates that the mapper defaults to current culture
            var originalCulture = CultureInfo.CurrentCulture;
            
            try
            {
                // Set current culture to German
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                
                var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions()); // No culture specified
                mapper.SetHeaders(new[] { "Price", "Rating" });

                var record = new[] { "123,45", "4,75" }; // German format

                // Act
                var result = mapper.MapRecord(record);

                // Assert
                Assert.Equal(123.45m, result.Price);
                Assert.Equal(4.75, result.Rating);
            }
            finally
            {
                // Restore original culture
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }

    public class ErrorHandlingTests
    {
        [Fact]
        public void ConvertValue_InvalidFormatWithCulture_ThrowsInformativeException()
        {
            // Arrange
            var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
            var mapping = CsvMapping.Create<TestModels.LocalizedData>();
            mapping.SetFormat(nameof(TestModels.LocalizedData.Date), "dd/MM/yyyy");
            
            var mapper = new CsvMapper<TestModels.LocalizedData>(mapping, frenchCulture);
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Date))!;

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => 
                mapper.ConvertValue("invalid-date", property));
            
            Assert.Contains("dd/MM/yyyy", exception.Message);
            Assert.Contains("fr-FR", exception.Message);
        }

        [Fact]
        public void ConvertValue_InvalidNumber_ThrowsFormatException()
        {
            // Arrange
            var mapper = new CsvMapper<TestModels.LocalizedData>(new CsvOptions());
            var property = typeof(TestModels.LocalizedData).GetProperty(nameof(TestModels.LocalizedData.Price))!;

            // Act & Assert - Now ConvertValue throws for invalid input
            Assert.Throws<FormatException>(() => mapper.ConvertValue("not-a-number", property));
        }
    }
}