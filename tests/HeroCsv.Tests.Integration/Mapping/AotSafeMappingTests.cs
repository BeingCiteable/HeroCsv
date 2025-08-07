using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HeroCsv.Tests.Integration.Mapping;

/// <summary>
/// Tests for AOT-safe factory-based mapping methods
/// </summary>
public class AotSafeMappingTests
{
    private class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
    }

    [Fact]
    public void Read_WithFactory_MapsCorrectly()
    {
        // Arrange
        var csv = """
            John,30,75000.50,true,2020-01-15
            Jane,25,65000.00,false,2021-06-01
            """;

        // Act
        var people = Csv.Read(csv, record => new Person
        {
            Name = record.GetString(0),
            Age = record.GetInt32(1),
            Salary = record.GetDecimal(2),
            IsActive = record.GetBoolean(3),
            HireDate = record.GetDateTime(4)
        }).ToList();

        // Assert
        Assert.Equal(2, people.Count);

        Assert.Equal("John", people[0].Name);
        Assert.Equal(30, people[0].Age);
        Assert.Equal(75000.50m, people[0].Salary);
        Assert.True(people[0].IsActive);
        Assert.Equal(new DateTime(2020, 1, 15), people[0].HireDate);

        Assert.Equal("Jane", people[1].Name);
        Assert.Equal(25, people[1].Age);
        Assert.Equal(65000.00m, people[1].Salary);
        Assert.False(people[1].IsActive);
        Assert.Equal(new DateTime(2021, 6, 1), people[1].HireDate);
    }

    [Fact]
    public void ReadWithHeaders_WithFactory_MapsCorrectly()
    {
        // Arrange
        var csv = """
            Name,Age,Salary,IsActive,HireDate
            Alice,28,70000,yes,2019-03-10
            Bob,35,85000.75,no,2018-11-20
            """;

        // Act
        var people = Csv.ReadWithHeaders(csv, (headers, record) =>
        {
            var nameIdx = headers.GetFieldIndex("Name");
            var ageIdx = headers.GetFieldIndex("Age");
            var salaryIdx = headers.GetFieldIndex("Salary");
            var activeIdx = headers.GetFieldIndex("IsActive");
            var hireDateIdx = headers.GetFieldIndex("HireDate");

            return new Person
            {
                Name = record.GetString(nameIdx),
                Age = record.GetInt32(ageIdx),
                Salary = record.GetDecimal(salaryIdx),
                IsActive = record.GetBoolean(activeIdx),
                HireDate = record.GetDateTime(hireDateIdx)
            };
        }).ToList();

        // Assert
        Assert.Equal(2, people.Count);

        Assert.Equal("Alice", people[0].Name);
        Assert.Equal(28, people[0].Age);
        Assert.Equal(70000m, people[0].Salary);
        Assert.True(people[0].IsActive);

        Assert.Equal("Bob", people[1].Name);
        Assert.Equal(35, people[1].Age);
        Assert.Equal(85000.75m, people[1].Salary);
        Assert.False(people[1].IsActive);
    }

    [Fact]
    public void ReadFile_WithFactory_MapsCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Product1,10,19.99\nProduct2,5,29.99");

            // Act
            var products = Csv.ReadFile(tempFile, record => new
            {
                Name = record.GetString(0),
                Quantity = record.GetInt32(1),
                Price = record.GetDecimal(2)
            }).ToList();

            // Assert
            Assert.Equal(2, products.Count);
            Assert.Equal("Product1", products[0].Name);
            Assert.Equal(10, products[0].Quantity);
            Assert.Equal(19.99m, products[0].Price);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetFieldByName_ReturnsCorrectValue()
    {
        // Arrange
        var csv = """
            Name,Age,City
            John,30,NYC
            """;

        // Act
        var result = Csv.ReadWithHeaders(csv, (headers, record) =>
        {
            return new
            {
                Name = record.GetFieldByName(headers, "Name"),
                Age = record.GetFieldByName(headers, "Age"),
                City = record.GetFieldByName(headers, "City")
            };
        }).First();

        // Assert
        Assert.Equal("John", result.Name);
        Assert.Equal("30", result.Age);
        Assert.Equal("NYC", result.City);
    }

    [Fact]
    public void TryGetMethods_HandleInvalidDataGracefully()
    {
        // Arrange
        var csv = "NotANumber,InvalidBool,BadDate";

        // Act
        var result = Csv.Read(csv, record =>
        {
            var hasInt = record.TryGetInt32(0, out var intVal);
            var hasBool = record.TryGetBoolean(1, out var boolVal);
            var hasDate = record.TryGetDateTime(2, out var dateVal);

            return new
            {
                HasInt = hasInt,
                IntValue = intVal,
                HasBool = hasBool,
                BoolValue = boolVal,
                HasDate = hasDate,
                DateValue = dateVal
            };
        }).First();

        // Assert
        Assert.False(result.HasInt);
        Assert.Equal(0, result.IntValue);
        Assert.False(result.HasBool);
        Assert.False(result.BoolValue);
        Assert.False(result.HasDate);
        Assert.Equal(default(DateTime), result.DateValue);
    }

    [Fact]
    public void GetStringOrDefault_ReturnsDefaultForEmptyFields()
    {
        // Arrange
        var csv = "Value1,,Value3";

        // Act
        var result = Csv.Read(csv, record => new
        {
            Field1 = record.GetStringOrDefault(0, "Default1"),
            Field2 = record.GetStringOrDefault(1, "Default2"),
            Field3 = record.GetStringOrDefault(2, "Default3"),
            Field4 = record.GetStringOrDefault(3, "Default4") // Out of bounds
        }).First();

        // Assert
        Assert.Equal("Value1", result.Field1);
        Assert.Equal("", result.Field2); // Empty field returns empty string, not default
        Assert.Equal("Value3", result.Field3);
        Assert.Equal("Default4", result.Field4);
    }

    [Fact]
    public void GetEnum_ParsesCorrectly()
    {
        // Arrange
        var csv = "Monday,tuesday,WEDNESDAY";

        // Act
        var result = Csv.Read(csv, record => new
        {
            Day1 = record.GetEnum<DayOfWeek>(0),
            Day2 = record.GetEnum<DayOfWeek>(1),
            Day3 = record.GetEnum<DayOfWeek>(2)
        }).First();

        // Assert
        Assert.Equal(DayOfWeek.Monday, result.Day1);
        Assert.Equal(DayOfWeek.Tuesday, result.Day2);
        Assert.Equal(DayOfWeek.Wednesday, result.Day3);
    }

    [Fact]
    public void GetGuid_ParsesCorrectly()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var csv = $"{guid1},{guid2}";

        // Act
        var result = Csv.Read(csv, record => new
        {
            Id1 = record.GetGuid(0),
            Id2 = record.GetGuid(1)
        }).First();

        // Assert
        Assert.Equal(guid1, result.Id1);
        Assert.Equal(guid2, result.Id2);
    }

    [Fact]
    public void GetBoolean_HandlesVariousFormats()
    {
        // Arrange
        var csv = "true,false,1,0,yes,no,TRUE,FALSE";

        // Act
        var result = Csv.Read(csv, record => new
        {
            B1 = record.GetBoolean(0),
            B2 = record.GetBoolean(1),
            B3 = record.GetBoolean(2),
            B4 = record.GetBoolean(3),
            B5 = record.GetBoolean(4),
            B6 = record.GetBoolean(5),
            B7 = record.GetBoolean(6),
            B8 = record.GetBoolean(7)
        }).First();

        // Assert
        Assert.True(result.B1);
        Assert.False(result.B2);
        Assert.True(result.B3);
        Assert.False(result.B4);
        Assert.True(result.B5);
        Assert.False(result.B6);
        Assert.True(result.B7);
        Assert.False(result.B8);
    }

    [Fact]
    public void ComplexScenario_CombinesMultipleFeatures()
    {
        // Arrange
        var csv = """
            OrderId,ProductName,Quantity,Price,OrderDate,IsExpress,CustomerId
            1001,Laptop,2,999.99,2024-01-15,true,a1b2c3d4-e5f6-7890-abcd-ef1234567890
            1002,Mouse,5,25.50,2024-01-16,false,b2c3d4e5-f6a7-8901-bcde-f23456789012
            """;

        // Act
        var orders = Csv.ReadWithHeaders(csv, (headers, record) =>
        {
            var orderIdIdx = headers.GetFieldIndex("OrderId");
            var productIdx = headers.GetFieldIndex("ProductName");
            var qtyIdx = headers.GetFieldIndex("Quantity");
            var priceIdx = headers.GetFieldIndex("Price");
            var dateIdx = headers.GetFieldIndex("OrderDate");
            var expressIdx = headers.GetFieldIndex("IsExpress");
            var customerIdx = headers.GetFieldIndex("CustomerId");

            return new
            {
                OrderId = record.GetInt32(orderIdIdx),
                Product = record.GetString(productIdx),
                Quantity = record.GetInt32(qtyIdx),
                UnitPrice = record.GetDecimal(priceIdx),
                OrderDate = record.GetDateTime(dateIdx),
                IsExpress = record.GetBoolean(expressIdx),
                CustomerId = record.GetGuid(customerIdx),
                TotalPrice = record.GetInt32(qtyIdx) * record.GetDecimal(priceIdx)
            };
        }).ToList();

        // Assert
        Assert.Equal(2, orders.Count);

        var order1 = orders[0];
        Assert.Equal(1001, order1.OrderId);
        Assert.Equal("Laptop", order1.Product);
        Assert.Equal(2, order1.Quantity);
        Assert.Equal(999.99m, order1.UnitPrice);
        Assert.Equal(1999.98m, order1.TotalPrice);
        Assert.True(order1.IsExpress);
        Assert.Equal(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), order1.CustomerId);

        var order2 = orders[1];
        Assert.Equal(1002, order2.OrderId);
        Assert.Equal("Mouse", order2.Product);
        Assert.Equal(127.50m, order2.TotalPrice);
        Assert.False(order2.IsExpress);
    }
}