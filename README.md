# FastCsv

An **ultra-fast and low memory usage** CSV parsing library for .NET focused on **reading operations only**. Built with zero-allocation parsing using ReadOnlySpan<char> and progressive framework-specific optimizations.

## Features

- **Ultra-fast performance** with zero-allocation parsing using ReadOnlySpan<char>
- **Extremely simple API** for basic use cases with advanced configuration options
- **Partial class pattern** for framework-specific optimizations
- **Progressive enhancement** across .NET versions (6+ → 7+ → 8+ → 9+)
- **Hardware acceleration** with Vector and Vector512 operations
- **SearchValues optimization** for ultra-fast character detection (.NET 8+)
- **Multi-framework support** (.NET Standard 2.0, .NET 6, .NET 7, .NET 8, .NET 9)
- **DateTimeOffset support** for timezone-aware timestamps
- **Feature-based organization** (Fields, Validation, Errors, Configuration)
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
foreach (var record in Csv.Read(csvData))
{
    Console.WriteLine(string.Join(" | ", record));
}
// Output:
// Name | Age | City
// John | 25 | New York
// Jane | 30 | London
// Bob | 35 | Paris

// Read with custom delimiter
foreach (var record in Csv.Read("Name;Age;City\nJohn;25;New York", ';'))
{
    Console.WriteLine(string.Join(" | ", record));
}

// Read with headers as dictionary
foreach (var record in Csv.ReadWithHeaders(csvData))
{
    Console.WriteLine($"Name: {record["Name"]}, Age: {record["Age"]}, City: {record["City"]}");
}
```

### File Operations

```csharp
using FastCsv;

// Read from file
foreach (var record in Csv.ReadFile("data.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}

// Async file reading (.NET 7+)
foreach (var record in await Csv.ReadFileAsync("data.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}

// Auto-detect format (.NET 8+)
foreach (var record in Csv.ReadFileAutoDetect("unknown_format.csv"))
{
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}
```

### Advanced Configuration

```csharp
using FastCsv;

// Fluent builder pattern for complex scenarios
var result = Csv.Configure()
    .WithFile("data.csv")
    .WithDelimiter(';')
    .WithHeaders(true)
    .WithValidation(true)
    .WithErrorTracking(true)
    .WithHardwareAcceleration(true)  // .NET 6+
    .WithProfiling(true)            // .NET 9+
    .ReadAdvanced();

Console.WriteLine($"Processed {result.TotalRecords} records in {result.ProcessingTime}");
Console.WriteLine($"Valid: {result.IsValid}");

if (result.ValidationErrors.Any())
{
    Console.WriteLine("Validation errors:");
    foreach (var error in result.ValidationErrors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

## Performance Features

### Zero-Allocation Parsing

```csharp
// ReadOnlySpan<char> overloads for maximum performance
ReadOnlySpan<char> csvSpan = csvData.AsSpan();
foreach (var record in Csv.Read(csvSpan))
{
    // Zero-allocation parsing
    Console.WriteLine($"Record: {string.Join(", ", record)}");
}
```

### Hardware Acceleration (.NET 6+)

```csharp
// Vector operations for large datasets
var fieldCount = Csv.CountFields(csvLine.AsSpan(), ',');
var isAccelerated = Csv.IsHardwareAccelerated;
var optimalSize = Csv.GetOptimalBufferSize();
```

### Advanced Vector Operations (.NET 9+)

```csharp
// Vector512 operations for maximum performance
var fieldCount = Csv.CountFieldsVector512(csvLine.AsSpan(), ',');
var isSupported = Csv.IsVector512Supported;
var optimalSize = Csv.GetOptimalVector512BufferSize();

// Performance profiling
var (records, metrics) = Csv.ReadWithProfiling(csvData.AsSpan(), options, enableProfiling: true);
Console.WriteLine($"Processing time: {metrics["ProcessingTime"]}");
Console.WriteLine($"Vector512 supported: {metrics["Vector512Supported"]}");
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

var employees = Csv.ReadWithHeaders(employeeData);
var engineeringEmployees = employees
    .Where(emp => emp["Department"] == "Engineering")
    .Select(emp => new 
    {
        Name = $"{emp["FirstName"]} {emp["LastName"]}",
        Salary = decimal.Parse(emp["Salary"]),
        HireDate = DateTime.Parse(emp["HireDate"])
    });

foreach (var emp in engineeringEmployees)
{
    Console.WriteLine($"{emp.Name}: ${emp.Salary:N0} (hired {emp.HireDate:MMM yyyy})");
}
```

### Error Handling and Validation

```csharp
var result = Csv.Configure()
    .WithFile("potentially-malformed.csv")
    .WithValidation(true)
    .WithErrorTracking(true)
    .ReadAdvanced();

if (!result.IsValid)
{
    Console.WriteLine($"Found {result.ValidationErrors.Count} validation errors:");
    foreach (var error in result.ValidationErrors)
    {
        Console.WriteLine($"- {error}");
    }
}
else
{
    Console.WriteLine($"Successfully processed {result.TotalRecords} records");
    // Process the valid records
    foreach (var record in result.Records)
    {
        // Process each record
    }
}
```

### Processing Large Files in Batches

```csharp
// For very large files, process records in batches to avoid memory issues
var largeFileRecords = Csv.Configure()
    .WithFile("very-large-file.csv")
    .WithHeaders(true)
    .Read();

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

The library follows a modular interface-based architecture:

### Core Interfaces
- **ICsvReader**: Zero-allocation CSV reader with progressive enhancements
- **ICsvRecord**: Single CSV record with field access capabilities
- **IFieldHandler**: Field parsing and processing operations
- **ICsvValidator**: CSV data structure and content validation
- **IErrorHandler**: Error management and reporting
- **ICsvErrorReporter**: Specialized error reporting and statistics
- **IConfigurationHandler**: CSV configuration and format detection
- **IPositionHandler**: Position tracking and navigation
- **IValidationHandler**: Structure and field validation

### Organization
- **Fields/**: Field handling and processing
- **Errors/**: Error handling and reporting
- **Validation/**: Data validation
- **Configuration/**: Configuration management
- **Navigation/**: Position tracking

### Partial Interface Pattern
Each interface uses framework-specific partial files:
- **Core interface**: Framework-agnostic base functionality
- **net6.cs**: Hardware acceleration features
- **net7.cs**: Fast parsing and type conversion
- **net8.cs**: Optimized collections and character detection
- **net9.cs**: Advanced hardware acceleration

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