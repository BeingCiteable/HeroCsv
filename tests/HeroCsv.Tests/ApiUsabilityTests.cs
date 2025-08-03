using System;
using System.Globalization;
using HeroCsv.Mapping;
using HeroCsv.Mapping.Attributes;
using HeroCsv.Mapping.Converters;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests to ensure the public API is intuitive and easy to use
/// </summary>
public class ApiUsabilityTests
{
    #region Test Models
    
    // Model with attributes
    public class Employee
    {
        [CsvColumn("Employee ID", Index = 0)]
        public int Id { get; set; }
        
        [CsvColumn("Full Name")]
        public string Name { get; set; } = "";
        
        [CsvColumn("Department", Default = "Unknown")]
        public string Department { get; set; } = "";
        
        [CsvColumn("Hire Date", Format = "yyyy-MM-dd")]
        [CsvConverter(typeof(DateTimeConverter))]
        public DateTime HireDate { get; set; }
        
        [CsvColumn("Is Active")]
        [CsvConverter(typeof(BooleanConverter))]
        public bool IsActive { get; set; }
        
        [CsvColumn("Salary", Ignore = true)]
        public decimal Salary { get; set; }
    }
    
    // Model for fluent mapping
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public ProductCategory Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool InStock { get; set; }
    }
    
    public enum ProductCategory
    {
        Electronics,
        Clothing,
        Food,
        Books
    }
    
    #endregion
    
    [Fact]
    public void TestAttributeBasedMapping()
    {
        var csv = @"Employee ID,Full Name,Department,Hire Date,Is Active,Salary
1,John Doe,Engineering,2020-01-15,yes,75000
2,Jane Smith,,2021-03-20,true,85000
3,Bob Johnson,Marketing,2019-11-01,false,65000";
        
        var employees = Csv.Read<Employee>(csv).ToList();
        
        Assert.Equal(3, employees.Count);
        
        // First employee
        Assert.Equal(1, employees[0].Id);
        Assert.Equal("John Doe", employees[0].Name);
        Assert.Equal("Engineering", employees[0].Department);
        Assert.Equal(new DateTime(2020, 1, 15), employees[0].HireDate);
        Assert.True(employees[0].IsActive);
        Assert.Equal(0m, employees[0].Salary); // Ignored property
        
        // Second employee - test default value
        Assert.Equal("Unknown", employees[1].Department);
        
        // Third employee - test boolean converter
        Assert.False(employees[2].IsActive);
    }
    
    [Fact]
    public void TestFluentMappingBuilder()
    {
        var csv = @"ID,Product Name,Cost,Type,Date,Available
1,Laptop,999.99,Electronics,2023-01-15,1
2,T-Shirt,19.99,Clothing,2023-02-20,yes
3,Coffee,12.50,Food,2023-03-10,0";
        
        var products = Csv.Read<Product>(csv, builder => {
            builder.Map(p => p.ProductId, "ID");
            builder.Map(p => p.Name, "Product Name");
            builder.Map(p => p.Price, "Cost");
            builder.Map(p => p.Category, "Type")
                .WithConverter<EnumConverter>();
            builder.Map(p => p.CreatedDate, "Date")
                .WithFormat("yyyy-MM-dd");
            builder.Map(p => p.InStock, "Available")
                .WithConverter(value => value == "1" || value.ToLower() == "yes");
            return builder.Build();
        }).ToList();
        
        Assert.Equal(3, products.Count);
        
        // First product
        Assert.Equal(1, products[0].ProductId);
        Assert.Equal("Laptop", products[0].Name);
        Assert.Equal(999.99m, products[0].Price);
        Assert.Equal(ProductCategory.Electronics, products[0].Category);
        Assert.Equal(new DateTime(2023, 1, 15), products[0].CreatedDate);
        Assert.True(products[0].InStock);
        
        // Test custom boolean converter
        Assert.True(products[1].InStock);
        Assert.False(products[2].InStock);
    }
    
    [Fact]
    public void TestMixedMapping()
    {
        var csv = @"ProductId,Name,Price,Category
1,Widget,25.50,Electronics
2,Gadget,15.75,";
        
        var products = Csv.Read<Product>(csv, builder => {
            builder.AutoMap(); // Use auto-mapping for matching property names
            builder.Map(p => p.Category, "Category") // Override with custom handling
                .WithDefault(ProductCategory.Electronics);
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, products.Count);
        
        // Properties mapped automatically
        Assert.Equal(1, products[0].ProductId);
        Assert.Equal("Widget", products[0].Name);
        Assert.Equal(25.50m, products[0].Price);
        
        // Test default value for empty field
        Assert.Equal(ProductCategory.Electronics, products[1].Category);
    }
    
    [Fact]
    public void TestCustomConverterClass()
    {
        var csv = @"Date,Value
2023-01-15 14:30:00,100
2023-02-20 09:15:30,200";
        
        var records = Csv.Read<DateRecord>(csv).ToList();
        
        Assert.Equal(2, records.Count);
        Assert.Equal(new DateTime(2023, 1, 15, 14, 30, 0), records[0].Date);
    }
    
    [Fact]
    public void TestEnumConverterWithFlags()
    {
        var csv = @"Name,Permissions
Admin,Read|Write|Delete
User,Read
Guest,";
        
        var users = Csv.Read<User>(csv).ToList();
        
        Assert.Equal(3, users.Count);
        Assert.Equal(Permission.Read | Permission.Write | Permission.Delete, users[0].Permissions);
        Assert.Equal(Permission.Read, users[1].Permissions);
        Assert.Equal(Permission.None, users[2].Permissions);
    }
    
    #region Additional Test Models
    
    public class DateRecord
    {
        [CsvConverter(typeof(DateTimeConverter))]
        [CsvColumn("Date", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime Date { get; set; }
        
        public int Value { get; set; }
    }
    
    [Flags]
    public enum Permission
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 4
    }
    
    public class User
    {
        public string Name { get; set; } = "";
        
        [CsvConverter(typeof(EnumConverter))]
        [CsvColumn("Permissions", Format = "Flags", Default = Permission.None)]
        public Permission Permissions { get; set; }
    }
    
    #endregion
}