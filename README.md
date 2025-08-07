# HeroCsv

[![Build](https://github.com/BeingCiteable/HeroCsv/actions/workflows/build.yml/badge.svg)](https://github.com/BeingCiteable/HeroCsv/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/HeroCsv.svg)](https://www.nuget.org/packages/HeroCsv/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/HeroCsv.svg)](https://www.nuget.org/packages/HeroCsv/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![AOT Ready](https://img.shields.io/badge/AOT-Ready-green)](#aot-support)

Ultra-fast CSV reading library for .NET with **zero-allocation parsing** and **full Native AOT support**.

## What

**HeroCsv** is a high-performance CSV reading library designed for speed and minimal memory usage. It parses CSV data into objects or string arrays with zero allocations and full Native AOT compatibility.

### Key Features

- **🚀 AOT Ready** - Full Native AOT and trimming support for faster startup and smaller binaries
- **⚡ Zero Allocation** - ReadOnlySpan<char> parsing without heap allocations
- **🎯 Simple API** - One-line CSV reading for common scenarios
- **📊 Object Mapping** - Automatic property mapping with type conversions
- **🔧 SIMD Optimized** - Hardware acceleration on .NET 6+ for large datasets
- **📁 File & Stream** - Read from files, streams, or strings with async support
- **✅ Validation** - Built-in error detection and reporting

## How to Install

```bash
dotnet add package HeroCsv
```

## How to Use

### Read CSV to String Arrays

```csharp
using HeroCsv;

// Most basic usage
var csvData = """
Name,Age,City
John,25,New York
Jane,30,London
Bob,35,Paris
""";

// Read all records
foreach (var record in Csv.ReadContent(csvData))
{
    Console.WriteLine(string.Join(" | ", record));
}
// Output:
// Name | Age | City
// John | 25 | New York
// Jane | 30 | London
// Bob | 35 | Paris

// Read with custom delimiter
foreach (var record in Csv.ReadContent("Name;Age;City\nJohn;25;New York", ';'))
{
    Console.WriteLine(string.Join(" | ", record));
}

// Read with options
var options = new CsvOptions { Delimiter = ',', HasHeaders = true };
foreach (var record in Csv.ReadContent(csvData, options))
{
    Console.WriteLine(string.Join(" | ", record));
}
```

### Read CSV to Objects

```csharp
using HeroCsv;

// Define your model
public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Department { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
}

var csvData = """
FirstName,LastName,Department,Salary,HireDate
John,Doe,Engineering,75000,2020-01-15
Jane,Smith,Marketing,65000,2019-06-20
Bob,Johnson,Sales,55000,2021-03-10
""";

// Automatic mapping by property names
var employees = Csv.Read<Employee>(csvData);
foreach (var emp in employees)
{
    Console.WriteLine($"{emp.FirstName} {emp.LastName} - {emp.Department}: ${emp.Salary:N0}");
}

// Manual column mapping (type-safe with expressions)
var employeesManual = Csv.Read<Employee>(csvData, mapping =>
    mapping.Map(e => e.FirstName, 0)
           .Map(e => e.LastName, 1)
           .Map(e => e.Department, 2)
           .Map(e => e.Salary, 3, decimal.Parse)  // Type-safe converter
           .Map(e => e.HireDate, 4, DateTime.Parse));

// Manual column mapping by name with type-safe converters
var employeesByName = Csv.Read<Employee>(csvData, CsvMapping.Create<Employee>()
    .Map(e => e.FirstName, "First Name")
    .Map(e => e.LastName, "Last Name")
    .Map(e => e.Department, "Department")
    .Map(e => e.Salary, "Salary", value => decimal.Parse(value.Replace("$", "")))
    .Map(e => e.HireDate, "Hire Date", DateTime.Parse));

// Auto mapping with manual overrides
var employeesWithOverrides = Csv.ReadAutoMapWithOverrides<Employee>(csvData, mapping =>
{
    // Override specific columns while auto-mapping the rest
    mapping.Map(e => e.Salary, 3, value => decimal.Parse(value) * 1.1m); // 10% salary adjustment
});

// Mix old string-based API with new type-safe API
var mixedMapping = CsvMapping.Create<Employee>()
    .MapProperty("FirstName", 0)              // Old API (string-based)
    .Map(e => e.LastName, 1)                 // New API (type-safe)
    .Map(e => e.Salary, 3, decimal.Parse);   // New API with converter
```

### Access Fields with Type Conversion

```csharp
// Using extension methods for type conversion
foreach (var record in Csv.ReadContent(csvData))
{
    var name = record.GetField<string>(0);
    var age = record.GetField<int>(1);
    var salary = record.GetField<decimal>(3);
    var hireDate = record.GetField<DateTime>(4);
    
    // Safe conversion with TryGet
    if (record.TryGetField<decimal>(3, out var salaryValue))
    {
        Console.WriteLine($"Salary: ${salaryValue:N0}");
    }
    
    // Check for empty fields
    if (!record.IsFieldEmpty(2))
    {
        Console.WriteLine($"Department: {record.GetField<string>(2)}");
    }
}
```

### Read from Files and Streams

```csharp
using HeroCsv;

// Read from file
foreach (var record in Csv.ReadFile("data.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}

// Read from stream
using var stream = File.OpenRead("data.csv");
foreach (var record in Csv.ReadStream(stream))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}

// Async file reading (.NET 7+)
#if NET7_0_OR_GREATER
await foreach (var record in Csv.ReadFileAsync("data.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}

// Read entire file async
var allRecords = await Csv.ReadFileAsync("data.csv", CsvOptions.Default);
#endif

// Auto-detect format (.NET 8+)
#if NET8_0_OR_GREATER
foreach (var record in Csv.ReadFileAutoDetect("unknown_format.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}
#endif
```

### Configuration and Validation

```csharp
using HeroCsv;

// Custom CSV options for complex scenarios
var options = new CsvOptions
{
    Delimiter = ';',
    HasHeaders = true,
    TrimWhitespace = true,
    SkipEmptyLines = true
};

// Read with builder configuration
var builder = Csv.Configure()
    .WithContent(csvData)
    .WithOptions(options)
    .WithValidation(true)
    .WithErrorHandler(new ErrorHandler());

var reader = builder.Build();
var records = new List<string[]>();
var isValid = true;

foreach (var record in reader)
{
    records.Add(record);
}

// Check validation results
var validationResult = reader.GetValidationResult();
if (validationResult != null && !validationResult.IsValid)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"- Line {error.LineNumber}: {error.ErrorType} - {error.Message}");
    }
}
```

## Performance

HeroCsv prioritizes speed and memory efficiency:

- **Zero allocations** during parsing using ReadOnlySpan<char>
- **SIMD acceleration** on .NET 6+ for large datasets  
- **Hardware-optimized** buffer sizes and operations
- **Progressive enhancement** - newer .NET versions get more optimizations

### Benchmarks

📊 **[View Live Results](https://beingciteable.github.io/HeroCsv/benchmarks/)** - Updated with every release

Run benchmarks yourself:
```bash
dotnet run -c Release --project benchmarks/HeroCsv.Benchmarks
```





## Framework Support

- **.NET Standard 2.0, 6, 7, 8, 9** - All supported
- **Progressive enhancement** - newer versions get more optimizations

## AOT Support

HeroCsv works with .NET trimming and Native AOT compilation for faster startup and smaller binaries.

```csharp
// Basic string array parsing (no reflection)
var records = Csv.ReadContent(csvData);

// Counting records (no reflection)
var count = Csv.CountRecords(csvData);

// Field iteration (no reflection)
foreach (var field in record)
{
    Console.WriteLine(field);
}

// Factory-based mapping (AOT-safe, no reflection)
var employees = Csv.Read(csvData, record => new Employee
{
    Name = record.GetString(0),
    Age = record.GetInt32(1),
    Salary = record.GetDecimal(2)
});

// With headers for named field access (AOT-safe)
var employees = Csv.ReadWithHeaders(csvData, (headers, record) => new Employee
{
    Name = record.GetFieldByName(headers, "Name"),
    Age = record.GetInt32(headers.GetFieldIndex("Age")),
    Salary = record.GetDecimal(headers.GetFieldIndex("Salary"))
});
```

### Factory-Based Mapping (AOT-Safe)

```csharp
// AOT-safe object creation using factory functions
var employees = Csv.Read(csvData, record => new Employee
{
    Name = record.GetString(0),
    Age = record.GetInt32(1),
    Salary = record.GetDecimal(2)
});
```

### Source Generation (100% AOT-Safe)

```csharp
using HeroCsv.Attributes;

// Mark your class for code generation
[GenerateCsvMapping(HasHeaders = true)]
public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    [CsvIgnore] public string InternalId { get; set; }
}

// Generated extension methods - zero reflection!
var products = csvData.ReadCsvProduct().ToList();
```

### Publish with AOT

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r win-x64
```

## Documentation

- [📊 Live Benchmarks](https://beingciteable.github.io/HeroCsv/benchmarks/)
- [📋 Release Notes](https://github.com/BeingCiteable/HeroCsv/releases)
- [🧪 Contributing Guide](CONTRIBUTING.md)

## License

MIT License - https://github.com/BeingCiteable/HeroCsv