# FastCsv

[![CI Build](https://github.com/BeingCiteable/FastCsv/actions/workflows/ci.yml/badge.svg)](https://github.com/BeingCiteable/FastCsv/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FastCsv.svg)](https://www.nuget.org/packages/FastCsv/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FastCsv.svg)](https://www.nuget.org/packages/FastCsv/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An **ultra-fast and low memory usage** CSV parsing library for .NET focused on **reading operations only**. Built with zero-allocation parsing using ReadOnlySpan<char> and progressive framework-specific optimizations.

## Features

- **Ultra-fast performance** with zero-allocation parsing using ReadOnlySpan<char>
- **Extremely simple API** for basic use cases with advanced configuration options
- **Generic object mapping** with automatic property mapping and custom converters
- **Comprehensive validation system** with detailed error reporting
- **Async operations** for file and stream processing (.NET 7+)
- **Auto-detection** of CSV formats and delimiters (.NET 8+)
- **Multi-framework support** (.NET Standard 2.0, .NET 6, .NET 7, .NET 8, .NET 9)
- **Rich extension methods** for type conversion and field access
- **Stream processing** with encoding support and memory efficiency
- **Reading-focused design** - no writing operations for maximum performance

## Installation

```bash
dotnet add package FastCsv
```

## Quick Start

### Basic CSV Reading (Simple API)

```csharp
using FastCsv;

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

### Object Mapping

```csharp
using FastCsv;

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

// Manual column mapping
var employeesManual = Csv.Read<Employee>(csvData, mapping =>
    mapping.Map(e => e.FirstName, 0)
           .Map(e => e.LastName, 1)
           .Map(e => e.Department, 2)
           .Map(e => e.Salary, 3)
           .Map(e => e.HireDate, 4));

// Auto mapping with manual overrides
var employeesWithOverrides = Csv.ReadAutoMapWithOverrides<Employee>(csvData, mapping =>
{
    mapping.AutoMap();  // Auto-map by header names
    mapping.Map(e => e.Salary, col => decimal.Parse(col) * 1.1m); // Custom converter
});
```

### Type Conversion and Field Access

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

### File and Stream Operations

```csharp
using FastCsv;

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

### Advanced Configuration

```csharp
using FastCsv;

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

## Performance Features

### Zero-Allocation Parsing

```csharp
// ReadOnlySpan<char> overloads for maximum performance
ReadOnlySpan<char> csvSpan = csvData.AsSpan();
var records = Csv.ReadAllRecords(csvSpan);
foreach (var record in records)
{
    // Zero-allocation parsing
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}
```

### Hardware Acceleration (.NET 6+)

```csharp
// Hardware acceleration is automatically used internally when available
// Check if your system supports hardware acceleration:
var isAccelerated = System.Numerics.Vector.IsHardwareAccelerated;

// The library automatically optimizes buffer sizes and operations
// based on available hardware acceleration
```

### Advanced Performance Features

```csharp
using FastCsv.Parsing;

// Ultra-fast field iteration without allocations
foreach (var field in CsvFieldIterator.IterateFields(csvData, options))
{
    // Process field.Value (ReadOnlySpan<char>)
    Console.WriteLine($"Row {field.RowIndex}, Field {field.FieldIndex}: {field.Value.ToString()}");
}

// Zero-allocation row enumeration using ICsvReader
var reader = Csv.CreateReader(csvData, options);
foreach (var record in reader)
{
    // Process record (string[])
    Console.WriteLine(string.Join(", ", record));
}

// Direct span-based parsing for maximum performance
var records = Csv.ReadAllRecords(csvData.AsSpan(), options);
Console.WriteLine($"Parsed {records.Count} records with zero allocations");
```

## Real-World Examples

### Processing Employee Data

```csharp
var employeeData = """
FirstName,LastName,Department,Salary,HireDate
John,Doe,Engineering,75000,2020-01-15
Jane,Smith,Marketing,65000,2019-06-20
Bob,Johnson,Sales,55000,2021-03-10
""";

// Using object mapping for strongly-typed access
var employees = Csv.Read<Employee>(employeeData);
var engineeringEmployees = employees
    .Where(emp => emp.Department == "Engineering")
    .Select(emp => new 
    {
        Name = $"{emp.FirstName} {emp.LastName}",
        Salary = emp.Salary,
        HireDate = emp.HireDate
    });

foreach (var emp in engineeringEmployees)
{
    Console.WriteLine($"{emp.Name}: ${emp.Salary:N0} (hired {emp.HireDate:MMM yyyy})");
}
```

### Error Handling and Validation

```csharp
var reader = Csv.Configure()
    .WithFile("potentially-malformed.csv")
    .WithValidation(true)
    .WithErrorHandler(new ErrorHandler())
    .Build();

var records = new List<string[]>();
foreach (var record in reader)
{
    records.Add(record);
}

var validationResult = reader.GetValidationResult();
if (validationResult != null && !validationResult.IsValid)
{
    Console.WriteLine($"Found {validationResult.Errors.Count} validation errors:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"- Line {error.LineNumber}: {error.ErrorType} - {error.Message}");
    }
}
else
{
    Console.WriteLine($"Successfully processed {records.Count} records");
    // Process the valid records
    foreach (var record in records)
    {
        // Process each record
    }
}
```

### Processing Large Files in Batches

```csharp
// For very large files, process records in batches to avoid memory issues
var largeFileRecords = Csv.ReadFile("very-large-file.csv");

var batchSize = 1000;
var batch = new List<string[]>();

foreach (var record in largeFileRecords)
{
    batch.Add(record);
    
    if (batch.Count >= batchSize)
    {
        ProcessBatch(batch);
        batch.Clear();
    }
}

// Process remaining records
if (batch.Any())
{
    ProcessBatch(batch);
}

static void ProcessBatch(List<string[]> batch)
{
    Console.WriteLine($"Processing batch of {batch.Count} records");
    // Process the batch
}
```

## Performance Philosophy

FastCsv is designed as an **ultra-fast and low memory usage** library with progressive enhancement:

- **Zero-allocation parsing** using ReadOnlySpan<char> for maximum performance
- **Framework-agnostic core** with consistent performance across all platforms
- **Progressive enhancement** with framework-specific optimizations
- **Hardware acceleration** on .NET 6+ for vectorized operations
- **SearchValues optimization** on .NET 8+ for ultra-fast character detection
- **Vector512 operations** on .NET 9+ for maximum hardware utilization
- **Pre-allocated collections** to minimize memory allocations
- **Aggressive inlining** for optimal JIT compilation
- **ReadOnly designs** to avoid unnecessary copying

### Benchmarks

Performance varies by .NET version due to progressive optimizations:

- **.NET Standard 2.0**: Fallback implementations for compatibility
- **.NET 6**: Vector-based operations for large records
- **.NET 7**: Enhanced span operations
- **.NET 8**: SearchValues and FrozenCollections for maximum speed
- **.NET 9**: Vector512 support and enhanced JIT optimizations

## Architecture

The library follows a modular architecture with core interfaces and implementations:

### Core Components
- **Csv**: Static API providing simple entry points for common operations
- **ICsvReader**: Core CSV reader interface with async support (.NET 6+)
- **ICsvRecord**: Single CSV record with field access and type conversion
- **ICsvReaderBuilder**: Fluent configuration interface for advanced scenarios
- **CsvMapper<T>**: Generic object mapping with automatic and manual mapping
- **CsvOptions**: Configuration struct for parsing behavior

### Key Classes
- **FastCsvReader**: Main CSV reader implementation with async support
- **CsvReaderBuilder**: Builder pattern implementation for configuration
- **CsvRecord**: Record implementation with extension method support
- **CsvParser**: Core parsing logic with ReadOnlySpan<char> optimization

### Supporting Features
- **Errors/**: Comprehensive error handling with IErrorHandler and specific error types
- **Validation/**: Data validation with IValidationHandler and detailed error reporting
- **Extensions**: Rich extension methods for type conversion and field access

### Progressive Enhancement
Framework-specific optimizations through partial classes:
- **Core files**: Framework-agnostic base functionality
- **net6.cs**: Hardware acceleration and async enumerable support
- **net7.cs**: Async operations and enhanced span processing
- **net8.cs**: SearchValues optimization and auto-detection features

## Framework Support

- **.NET Standard 2.0** - Basic high-performance parsing with fallback implementations
- **.NET 6.0** - Basic high-performance parsing with Vector support
- **.NET 7.0** - Enhanced span operations and UTF-8 improvements
- **.NET 8.0** - SearchValues, FrozenCollections, and maximum optimization
- **.NET 9.0** - Next-generation SIMD with Vector512 and enhanced performance

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

Repository: https://github.com/BeingCiteable/FastCsv

## Design Principles

### 1. Ultra-Fast and Low Memory Usage
- **Primary goal**: Maximum performance with minimal memory allocations
- **ReadOnlySpan<char>** for zero-allocation parsing
- **Pre-allocated collections** to reduce GC pressure
- **Hardware acceleration** through Vector operations

### 2. Simple API with Advanced Options
- **Static Csv class** for extremely simple use cases
- **Fluent builder pattern** for advanced configuration
- **ReadOnlySpan<char> overloads** for maximum performance
- **String overloads** for ease of use

### 3. Partial Class Pattern
- **All interfaces and classes** follow the partial pattern
- **Framework-specific files** contain optimizations (net6.cs, net7.cs, net8.cs, net9.cs)
- **Progressive enhancement** across .NET versions
- **Conditional compilation** for version-specific features

### 4. Framework-Agnostic Design
- **Core functionality** independent of .NET version
- **Meaningful comments** focus on functionality, not implementation
- **Single Responsibility Principle** - each interface handles one core responsibility
- **DateTimeOffset** preferred over DateTime for timezone awareness

### 5. Reading-Focused Design
- **No writing operations** for maximum performance optimization
- **Feature-based organization** (Fields, Validation, Errors, Configuration)
- **Interface-based architecture** with partial implementations

## Acknowledgments

This library demonstrates modern .NET design with the partial class pattern, progressive enhancement, and ultra-fast zero-allocation parsing while maintaining an extremely simple API for basic use cases.