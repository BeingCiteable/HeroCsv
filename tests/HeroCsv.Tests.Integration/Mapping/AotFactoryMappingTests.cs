using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using HeroCsv.Models;

namespace HeroCsv.Tests.Integration.Mapping;

/// <summary>
/// Tests for AOT-safe factory-based mapping methods that work without reflection
/// </summary>
public class AotFactoryMappingTests
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

        // Act - Using AOT-safe factory approach
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

        // Act - Using AOT-safe factory with headers
        var people = Csv.ReadWithHeaders(csv, (headers, record) =>
        {
            var nameIdx = Array.IndexOf(headers, "Name");
            var ageIdx = Array.IndexOf(headers, "Age");
            var salaryIdx = Array.IndexOf(headers, "Salary");
            var activeIdx = Array.IndexOf(headers, "IsActive");
            var hireDateIdx = Array.IndexOf(headers, "HireDate");

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
    public void Factory_WithCustomOptions_WorksCorrectly()
    {
        // Arrange
        var csv = """
            Product1;10;19.99
            Product2;5;29.99
            """;

        var options = new CsvOptions(';', '"', false);

        // Act
        var products = Csv.Read(csv, options, record => new
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
    public void GetFieldByName_WithHeaders_ReturnsCorrectValue()
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
    public void ComplexMapping_WithCalculatedFields_WorksCorrectly()
    {
        // Arrange
        var csv = """
            OrderId,ProductName,Quantity,Price,OrderDate
            1001,Laptop,2,999.99,2024-01-15
            1002,Mouse,5,25.50,2024-01-16
            """;

        // Act
        var orders = Csv.ReadWithHeaders(csv, (headers, record) =>
        {
            var qtyIdx = Array.IndexOf(headers, "Quantity");
            var priceIdx = Array.IndexOf(headers, "Price");

            var quantity = record.GetInt32(qtyIdx);
            var unitPrice = record.GetDecimal(priceIdx);

            return new
            {
                OrderId = record.GetInt32(Array.IndexOf(headers, "OrderId")),
                Product = record.GetString(Array.IndexOf(headers, "ProductName")),
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = quantity * unitPrice,
                OrderDate = record.GetDateTime(Array.IndexOf(headers, "OrderDate"))
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

        var order2 = orders[1];
        Assert.Equal(1002, order2.OrderId);
        Assert.Equal("Mouse", order2.Product);
        Assert.Equal(127.50m, order2.TotalPrice);
    }
}