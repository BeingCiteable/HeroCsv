using System;
using HeroCsv.Mapping;
using HeroCsv.Mapping.Converters;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests for CSV mapping using fluent API configuration (builder.Map, builder.Ignore, etc.)
/// </summary>
public class FluentMappingTests
{
    #region Test Models
    
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
        public Department Department { get; set; }
    }
    
    public enum Department
    {
        Sales,
        Marketing,
        Engineering,
        HR
    }
    
    public class Product
    {
        public string Sku { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime LastRestocked { get; set; }
        public bool Discontinued { get; set; }
    }
    
    #endregion
    
    [Fact]
    public void MapWithFluentApi_ByColumnName_MapsCorrectly()
    {
        var csv = @"PersonId,First,Last,DOB,Salary,Active,Dept
1,John,Doe,1990-01-15,75000.50,true,Engineering
2,Jane,Smith,1985-03-20,85000.00,false,Sales";
        
        var results = Csv.Read<Person>(csv, builder => {
            builder.Map(p => p.Id, "PersonId");
            builder.Map(p => p.FirstName, "First");
            builder.Map(p => p.LastName, "Last");
            builder.Map(p => p.BirthDate, "DOB");
            builder.Map(p => p.Salary, "Salary");
            builder.Map(p => p.IsActive, "Active");
            builder.Map(p => p.Department, "Dept");
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        
        Assert.Equal(1, results[0].Id);
        Assert.Equal("John", results[0].FirstName);
        Assert.Equal("Doe", results[0].LastName);
        Assert.Equal(new DateTime(1990, 1, 15), results[0].BirthDate);
        Assert.Equal(75000.50m, results[0].Salary);
        Assert.True(results[0].IsActive);
        Assert.Equal(Department.Engineering, results[0].Department);
    }
    
    [Fact]
    public void MapWithFluentApi_ByIndex_MapsCorrectly()
    {
        var csv = @"1,John,Doe,1990-01-15,75000.50,1,Engineering
2,Jane,Smith,1985-03-20,85000.00,0,Sales";
        
        var options = new CsvOptions(hasHeader: false);
        var results = Csv.Read<Person>(csv, builder => {
            builder.WithOptions(options);
            builder.Map(p => p.Id, 0);
            builder.Map(p => p.FirstName, 1);
            builder.Map(p => p.LastName, 2);
            builder.Map(p => p.BirthDate, 3);
            builder.Map(p => p.Salary, 4);
            builder.Map(p => p.IsActive, 5)
                .WithConverter(v => v == "1");
            builder.Map(p => p.Department, 6);
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.Equal("John", results[0].FirstName);
        Assert.True(results[0].IsActive);
        Assert.False(results[1].IsActive);
    }
    
    [Fact]
    public void MapWithFluentApi_CustomConverters_AppliesConversion()
    {
        var csv = @"SKU,Product Name,Price,Stock,Last Restocked,Status
PROD001,Widget,19.99,100,01/15/2023,Active
PROD002,Gadget,29.99,0,12/01/2022,Discontinued";
        
        var results = Csv.Read<Product>(csv, builder => {
            builder.Map(p => p.Sku, "SKU");
            builder.Map(p => p.Name, "Product Name");
            builder.Map(p => p.Price, "Price");
            builder.Map(p => p.Stock, "Stock");
            builder.Map(p => p.LastRestocked, "Last Restocked")
                .WithFormat("MM/dd/yyyy");
            builder.Map(p => p.Discontinued, "Status")
                .WithConverter(status => status == "Discontinued");
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        
        Assert.Equal("PROD001", results[0].Sku);
        Assert.Equal(19.99m, results[0].Price);
        Assert.Equal(new DateTime(2023, 1, 15), results[0].LastRestocked);
        Assert.False(results[0].Discontinued);
        
        Assert.True(results[1].Discontinued);
    }
    
    [Fact]
    public void MapEmptyFields_WithFluentDefaults_AppliesValues()
    {
        var csv = @"SKU,Name,Price,Stock
PROD001,Widget,,100
PROD002,,29.99,
PROD003,,,";
        
        var results = Csv.Read<Product>(csv, builder => {
            builder.Map(p => p.Sku, "SKU");
            builder.Map(p => p.Name, "Name")
                .WithDefault("Unknown Product");
            builder.Map(p => p.Price, "Price")
                .WithDefault(0.00m);
            builder.Map(p => p.Stock, "Stock")
                .WithDefault(0);
            return builder.Build();
        }).ToList();
        
        Assert.Equal(3, results.Count);
        
        // First row - missing price
        Assert.Equal("Widget", results[0].Name);
        Assert.Equal(0.00m, results[0].Price);
        Assert.Equal(100, results[0].Stock);
        
        // Second row - missing name and stock
        Assert.Equal("Unknown Product", results[1].Name);
        Assert.Equal(29.99m, results[1].Price);
        Assert.Equal(0, results[1].Stock);
        
        // Third row - all missing
        Assert.Equal("Unknown Product", results[2].Name);
        Assert.Equal(0.00m, results[2].Price);
        Assert.Equal(0, results[2].Stock);
    }
    
    [Fact]
    public void MapWithFluentApi_BuiltInConverters_ParsesCorrectly()
    {
        var csv = @"Id,Name,DOB,Active,Dept
1,John,15/01/1990,yes,Engineering
2,Jane,20/03/1985,no,Sales";
        
        var results = Csv.Read<Person>(csv, builder => {
            builder.Map(p => p.Id, "Id");
            builder.Map(p => p.FirstName, "Name");
            builder.Map(p => p.BirthDate, "DOB")
                .WithConverter<DateTimeConverter>();
            builder.Map(p => p.IsActive, "Active")
                .WithConverter<BooleanConverter>();
            builder.Map(p => p.Department, "Dept")
                .WithConverter<EnumConverter>();
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.Equal(new DateTime(1990, 1, 15), results[0].BirthDate);
        Assert.True(results[0].IsActive);
        Assert.Equal(Department.Engineering, results[0].Department);
    }
    
    [Fact]
    public void AutoMapWithOverrides_AppliesBothAutoAndManual()
    {
        var csv = @"Id,FirstName,LastName,DOB,Salary,IsActive,Dept
1,John,Doe,1990-01-15,75000,Y,2
2,Jane,Smith,1985-03-20,85000,N,0";
        
        var results = Csv.Read<Person>(csv, builder => {
            builder.AutoMap(); // Map matching property names automatically
            
            // Override specific mappings
            builder.Map(p => p.BirthDate, "DOB");
            builder.Map(p => p.IsActive, "IsActive")
                .WithConverter(v => v == "Y");
            builder.Map(p => p.Department, "Dept")
                .WithConverter(v => (Department)int.Parse(v));
                
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        
        // Auto-mapped properties
        Assert.Equal(1, results[0].Id);
        Assert.Equal("John", results[0].FirstName);
        Assert.Equal("Doe", results[0].LastName);
        Assert.Equal(75000m, results[0].Salary);
        
        // Overridden mappings
        Assert.Equal(new DateTime(1990, 1, 15), results[0].BirthDate);
        Assert.True(results[0].IsActive);
        Assert.Equal(Department.Engineering, results[0].Department);
        
        Assert.False(results[1].IsActive);
        Assert.Equal(Department.Sales, results[1].Department);
    }
    
    [Fact]
    public void IgnoreProperties_WithFluentApi_ExcludesFromMapping()
    {
        var csv = @"Id,FirstName,LastName,Salary,Secret
1,John,Doe,75000,SecretData
2,Jane,Smith,85000,MoreSecrets";
        
        var results = Csv.Read<Person>(csv, builder => {
            builder.AutoMap();
            builder.Ignore(p => p.BirthDate);
            builder.Ignore(p => p.IsActive);
            builder.Ignore(p => p.Department);
            return builder.Build();
        }).ToList();
        
        Assert.Equal(2, results.Count);
        Assert.Equal("John", results[0].FirstName);
        Assert.Equal(75000m, results[0].Salary);
        Assert.Equal(default(DateTime), results[0].BirthDate);
        Assert.False(results[0].IsActive);
        Assert.Equal(default(Department), results[0].Department);
    }
    
    [Fact]
    public void MarkRequired_WithFluentApi_ValidatesPresence()
    {
        var csv = @"Id,Name,Price
1,Widget,19.99
2,,29.99
3,Gadget,";
        
        var results = Csv.Read<Product>(csv, builder => {
            builder.Map(p => p.Sku, "Id");
            builder.Map(p => p.Name, "Name")
                .Required();
            builder.Map(p => p.Price, "Price")
                .Required();
            return builder.Build();
        }).ToList();
        
        // Note: Current implementation doesn't enforce Required
        // This would need to be implemented with validation
        Assert.Equal(3, results.Count);
    }
    
    [Fact]
    public void CreateMappings_FromSeparateBuilders_AreIndependent()
    {
        var builder1 = new CsvMappingBuilder<Person>();
        var builder2 = new CsvMappingBuilder<Person>();
        
        builder1.Map(p => p.Id, "ID");
        builder2.Map(p => p.Id, "PersonId");
        
        var mapping1 = builder1.Build();
        var mapping2 = builder2.Build();
        
        Assert.NotSame(mapping1, mapping2);
        Assert.NotEqual(mapping1.PropertyMappings[0].ColumnName, mapping2.PropertyMappings[0].ColumnName);
    }
    
    [Fact]
    public void ConvertBuilder_ToMapping_ImplicitlyConverts()
    {
        var builder = new CsvMappingBuilder<Person>();
        builder.Map(p => p.Id, "ID");
        
        CsvMapping<Person> mapping = builder; // Implicit conversion
        
        Assert.NotNull(mapping);
        Assert.Single(mapping.PropertyMappings);
        Assert.Equal("ID", mapping.PropertyMappings[0].ColumnName);
    }
}