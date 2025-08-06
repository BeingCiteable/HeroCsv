using System;
using System.Linq;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Mapping;

/// <summary>
/// Tests specifically for Csv.Read<T>() generic object mapping
/// </summary>
public class CsvReadGenericTests
{
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    [Fact]
    public void ReadGeneric_SimpleMapping_MapsPropertiesByName()
    {
        var csv = "Name,Age,City\nJohn,25,NYC\nJane,30,LA";
        var people = Csv.Read<Person>(csv).ToList();
        
        Assert.Equal(2, people.Count);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadGeneric_WithOptions_RespectsConfiguration()
    {
        var csv = "Name;Age;City\nJohn;25;NYC";
        var options = new CsvOptions(delimiter: ';');
        var people = Csv.Read<Person>(csv, options).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
    }

    [Fact]
    public void ReadGeneric_TypeConversion_ConvertsBasicTypes()
    {
        var csv = "Id,Name,Price,InStock\n1,Widget,19.99,true\n2,Gadget,29.99,false";
        var products = Csv.Read<Product>(csv).ToList();
        
        Assert.Equal(2, products.Count);
        Assert.Equal(1, products[0].Id);
        Assert.Equal(19.99m, products[0].Price);
        Assert.True(products[0].InStock);
        Assert.False(products[1].InStock);
    }

    [Fact]
    public void ReadGeneric_CaseInsensitiveMapping_MatchesProperties()
    {
        var csv = "name,AGE,CITY\nJohn,25,NYC";
        var people = Csv.Read<Person>(csv).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("NYC", people[0].City);
    }

    [Fact]
    public void ReadGeneric_MissingColumns_LeavesPropertiesAsDefault()
    {
        var csv = "Name,Age\nJohn,25"; // Missing City column
        var people = Csv.Read<Person>(csv).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("", people[0].City); // Default value
    }

    [Fact]
    public void ReadGeneric_ExtraColumns_IgnoresUnmappedData()
    {
        var csv = "Name,Age,City,Country,Zip\nJohn,25,NYC,USA,10001";
        var people = Csv.Read<Person>(csv).ToList();
        
        Assert.Single(people);
        Assert.Equal("John", people[0].Name);
        Assert.Equal(25, people[0].Age);
        Assert.Equal("NYC", people[0].City);
        // Country and Zip are ignored
    }

    [Fact]
    public void ReadGeneric_EmptyFields_UsesTypeDefaults()
    {
        var csv = "Id,Name,Price,InStock\n1,,0,\n2,Gadget,,true";
        var products = Csv.Read<Product>(csv).ToList();
        
        Assert.Equal(2, products.Count);
        Assert.Null(products[0].Name); // Empty fields are mapped as null
        Assert.Equal(0m, products[0].Price);
        Assert.False(products[0].InStock); // Default for bool
        Assert.Equal(0m, products[1].Price); // Default for decimal
    }
}