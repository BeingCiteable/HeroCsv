using HeroCsv;
using HeroCsv.Mapping;
using Xunit;

namespace HeroCsv.Tests.Mapping;

public class TypeSafeMappingTests
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTimeOffset BirthDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
    }

    [Fact]
    public void TypeSafeMapping_WithExpressions()
    {
        var csv = "Name,Age,BirthDate,Active,Salary\nJohn,25,1998-01-01,true,50000.50";
        
        // Old way - string based, no compile-time checking
        var oldMapping = CsvMapping.Create<Person>()
            .MapProperty("Name", 0)
            .MapProperty("Age", 1, value => int.Parse(value));
        
        // New way - type-safe with expressions
        var newMapping = CsvMapping.Create<Person>()
            .Map(p => p.Name, 0)
            .Map(p => p.Age, 1, int.Parse)
            .Map(p => p.BirthDate, 2, value => DateTimeOffset.Parse(value + " +00:00"))
            .Map(p => p.IsActive, 3, bool.Parse)
            .Map(p => p.Salary, 4, decimal.Parse);
        
        var people = Csv.Read(csv, newMapping);
        var person = people.First();
        
        Assert.Equal("John", person.Name);
        Assert.Equal(25, person.Age);
        Assert.Equal(1998, person.BirthDate.Year);
        Assert.Equal(1, person.BirthDate.Month);
        Assert.Equal(1, person.BirthDate.Day);
        Assert.True(person.IsActive);
        Assert.Equal(50000.50m, person.Salary);
    }
    
    [Fact]
    public void TypeSafeMapping_ByColumnName()
    {
        var csv = "PersonName,PersonAge\nJane,30";
        
        var mapping = CsvMapping.Create<Person>()
            .Map(p => p.Name, "PersonName")
            .Map(p => p.Age, "PersonAge", int.Parse);
        
        var people = Csv.Read(csv, mapping);
        var person = people.First();
        
        Assert.Equal("Jane", person.Name);
        Assert.Equal(30, person.Age);
    }
    
    [Fact]
    public void TypeSafeMapping_MixedWithOldStyle()
    {
        var csv = "Name,Age,Status\nBob,40,Active";
        
        // Can mix both styles for backward compatibility
        var mapping = CsvMapping.Create<Person>()
            .Map(p => p.Name, 0)                    // Type-safe
            .MapProperty("Age", 1, value => int.Parse(value))  // Old style
            .Map(p => p.IsActive, 2, value => value == "Active");  // Type-safe with custom logic
        
        var people = Csv.Read(csv, mapping);
        var person = people.First();
        
        Assert.Equal("Bob", person.Name);
        Assert.Equal(40, person.Age);
        Assert.True(person.IsActive);
    }
    
    [Fact]
    public void TypeSafeMapping_ComplexConverter()
    {
        var csv = "Name,Salary\nAlice,USD 75000";
        
        var mapping = CsvMapping.Create<Person>()
            .Map(p => p.Name, 0)
            .Map(p => p.Salary, 1, value =>
            {
                // Complex parsing logic with type safety
                var amount = value.Replace("USD ", "").Trim();
                return decimal.Parse(amount);
            });
        
        var people = Csv.Read(csv, mapping);
        var person = people.First();
        
        Assert.Equal("Alice", person.Name);
        Assert.Equal(75000m, person.Salary);
    }
    
    [Fact]
    public void TypeSafeMapping_CompileTimeErrors()
    {
        // This test shows compile-time safety
        // The following would not compile:
        
        // CsvMapping.Create<Person>()
        //     .Map(p => p.Age, 0, value => value)  // Error: cannot convert string to int
        //     .Map(p => p.Salary, 1, int.Parse);   // Error: cannot convert int to decimal
        
        // But this compiles fine:
        var mapping = CsvMapping.Create<Person>()
            .Map(p => p.Age, 0, int.Parse)
            .Map(p => p.Salary, 1, decimal.Parse);
        
        Assert.NotNull(mapping);
    }
}