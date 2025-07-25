# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains FastCsv, a high-performance CSV parsing library for .NET focused on **reading operations only**. The library is designed for **ultra-fast and low memory usage** through zero-allocation parsing using ReadOnlySpan<char> and spans, with progressive framework-specific optimizations.

## Project Vision

Build an **ultra fast and low memory usage** CSV reading library that:
- Provides extremely simple API for basic use cases
- Offers advanced configuration for complex scenarios  
- Follows strict architectural patterns for maintainability
- Achieves zero-allocation parsing through span-based processing
- Implements progressive performance enhancements across .NET versions

## Architecture

The library follows a modular interface-based architecture with feature-based organization and **partial class pattern**:

### Core Interfaces (Current Implementation)
- **ICsvReader**: Core CSV reader with async enumerable support (.NET 6+)
- **ICsvRecord**: Single CSV record with field access and type conversion capabilities
- **ICsvReaderBuilder**: Fluent builder interface for advanced configuration scenarios
- **IErrorHandler**: Error handling and reporting with customizable implementations
- **IValidationHandler**: CSV validation with detailed error tracking and reporting

### Current Organization
The library is organized with the following key features:
- **Core Static API**: `Csv` class provides simple entry points for common operations
- **Object Mapping**: `CsvMapper<T>` with automatic property mapping and custom converters
- **Error Handling**: `Errors/` directory with `IErrorHandler` interface and implementations
- **Validation**: `Validation/` directory with `IValidationHandler` and detailed error reporting
- **Extensions**: Rich extension methods in `ExtensionsToICsvRecord` for type conversion and field access

### Current Partial Implementation Pattern
The library implements the partial class pattern for key components:
- **Core files**: Framework-agnostic base functionality
- **.net6.cs files**: Async enumerable support and hardware acceleration options
- **.net7.cs files**: Async file operations (only for Csv class)
- **.net8.cs files**: SearchValues optimization, auto-detection, and frozen collections

**Note**: The partial pattern is selectively implemented where it provides clear benefits. Not all classes need framework-specific versions.

### Performance Features
- **Multi-target framework support**: NET6+, NET7+, NET8+ with conditional compilation
- **Zero-allocation parsing**: Uses ref structs and spans to avoid heap allocations
- **SIMD optimizations**: Vector512/Vector256 operations for NET8+/NET9+
- **SearchValues optimization**: Fast character searching in NET8+
- **Escaped quote handling**: Special path for fields with escaped quotes (only allocation point)

### Key Features (Current Implementation)
- **Generic Object Mapping**: Comprehensive `CsvMapper<T>` with automatic property mapping, manual column mapping, and custom converters
- **Three Mapping Modes**: Auto (by property names), Manual (by column index), and Mixed (combination of both)
- **Rich Extension Methods**: `ExtensionsToICsvRecord` provides type conversion, field validation, and direct object mapping
- **Comprehensive Error Handling**: Detailed error types with `CsvValidationResult` and `CsvValidationError` for specific error tracking
- **Async Operations**: Full async support for file and stream operations (.NET 7+)
- **Auto-Detection**: Automatic CSV format and delimiter detection (.NET 8+)
- **Builder Pattern**: `ICsvReaderBuilder` for complex configuration scenarios
- **Partial Class Pattern**: Framework-specific enhancements (partially implemented)
- **DateTimeOffset Usage**: All timestamp fields use DateTimeOffset for timezone awareness
- Extensive use of `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for performance
- Conditional compilation directives (`#if NET8_0_OR_GREATER`) for version-specific optimizations
- ref struct enumerators to avoid allocations
- ArrayPool usage for memory efficiency

## Development Commands

This is a full .NET solution with MSBuild support. Common commands:

### Building
```bash
dotnet build                    # Build all projects
dotnet build -c Release        # Build in release mode
dotnet build --framework net8.0 # Build specific framework
```

### Testing
```bash
dotnet test                     # Run all tests
dotnet test --logger trx        # Run tests with TRX logger
dotnet test --collect:"XPlat Code Coverage" # Run with coverage
```

### Packaging
```bash
dotnet pack -c Release          # Create NuGet package
dotnet pack --no-build -c Release # Pack without building
```

### Running
```bash
dotnet run --project tests/FastCsv.Tests # Run test project
```

## Version-Specific Features

### NET6+
- Vector-based field counting for large records
- Hardware acceleration checks

### NET7+
- System.Buffers.Text usage

### NET8+
- SearchValues for ultra-fast character scanning
- FrozenSet/FrozenDictionary for preset configurations
- Auto-detection of CSV formats
- Async file reading with UTF-8 optimization

### NET9+
- Vector512 SIMD operations
- Enhanced vectorized field counting
- Advanced JIT optimizations

## Important Implementation Details

- The library uses ref structs extensively, which means they cannot be used in async methods or stored in collections
- Only fields with escaped quotes require allocation (StringBuilder usage)
- CSV reading is forward-only and cannot seek backwards
- The CsvReader maintains position and line number state internally
- Memory pooling is used in PooledCsvWriter to reduce GC pressure
- CsvRecordWrapper is provided for utility methods that need to return IEnumerable

## High-Performance Features

### Zero-Allocation Architecture
- **CsvFieldIterator**: Direct buffer access with no intermediate allocations
- **CsvRow**: Ref struct that stores field positions instead of string copies
- **RowEnumerable**: Zero-allocation row enumeration using ref structs
- **CsvFieldEnumerator**: Field-by-field parsing without string allocation

### Performance Comparison (1000 rows, 100 iterations)
- **FastCsv Row Enumeration**: 0.22 ms/op (beats Sep's 0.25 ms/op)
- **FastCsv Count-Only**: 0.06 ms/op (beats Sep's 0.10 ms/op)
- **FastCsv Field Iterator**: 0.31 ms/op (unique feature, no comparison)

### Usage Examples
```csharp
// Ultra-fast field iteration (best for processing)
foreach (var field in CsvFieldIterator.IterateFields(csvData, options))
{
    // Process field.Value (ReadOnlySpan<char>)
    // Access field.RowIndex and field.FieldIndex
}

// Zero-allocation row enumeration
foreach (var row in reader.EnumerateRows())
{
    var enumerator = row.GetFieldEnumerator();
    while (enumerator.TryGetNextField(out var field))
    {
        // Process field span
    }
}
```

## Current Project Structure

```
FastCsv/
├── src/
│   └── FastCsv/
│       ├── Csv.cs                  # Core static API
│       ├── Csv.net7.cs            # Async operations (ReadFileAsync, ReadStreamAsync)
│       ├── Csv.net8.cs            # Auto-detection features
│       ├── ICsvReader.cs          # Core CSV reader interface
│       ├── ICsvReader.net6.cs     # Async enumerable support
│       ├── ICsvReaderBuilder.cs   # Core builder interface
│       ├── ICsvReaderBuilder.net6.cs # Hardware acceleration options
│       ├── ICsvRecord.cs          # Core record interface
│       ├── FastCsvReader.cs       # Main reader implementation
│       ├── FastCsvReader.net6.cs  # Hardware acceleration enhancements
│       ├── CsvReaderBuilder.cs    # Builder implementation
│       ├── CsvOptions.cs          # Configuration struct
│       ├── CsvReadResult.cs       # Results container
│       ├── CsvValidationResult.cs # Validation results
│       ├── CsvRecord.cs           # Record implementation
│       ├── CsvParser.cs           # Core parsing logic
│       ├── CsvMapper.cs           # Generic object mapping
│       ├── ExtensionsToICsvRecord.cs # Extension methods
│       ├── CsvFieldIterator.cs    # Ultra-fast field iteration
│       ├── CsvRow.cs              # Zero-allocation row struct
│       ├── RowEnumerable.cs       # Zero-allocation row enumeration
│       ├── CsvFieldEnumerator.cs  # Field-by-field parser
│       ├── ICsvDataSource.cs      # Data source abstraction
│       ├── Errors/
│       │   ├── IErrorHandler.cs   # Core error handling interface
│       │   ├── ErrorHandler.cs    # Default error handler
│       │   └── NullErrorHandler.cs # Null object pattern
│       ├── Validation/
│       │   ├── IValidationHandler.cs # Core validation interface
│       │   └── ValidationHandler.cs  # Default validation handler
│       └── FastCsv.csproj         # Project file
├── tests/
│   └── FastCsv.Tests/
│       ├── UnitTest1.cs           # Basic tests
│       └── FastCsv.Tests.csproj   # Test project
├── benchmarks/
│   └── FastCsv.Benchmarks/
│       ├── QuickBenchmark.cs      # Performance comparison tests
│       └── FastCsv.Benchmarks.csproj # Benchmark project
├── .vscode/                       # VS Code configuration
├── FastCsv.sln                    # Solution file
├── README.md                      # Project documentation
├── CLAUDE.md                      # This file
└── .gitignore                     # Git ignore rules
```

## NuGet Package Information

The project is configured as a NuGet package with:
- Multi-framework targeting (net6.0, net7.0, net8.0, net9.0)
- Source Link support for debugging
- XML documentation generation
- Symbol packages (.snupkg) for debugging

## Design Principles

### 1. Partial Interface Pattern
- Each interface is split into core functionality and framework-specific enhancements
- Core interfaces contain framework-agnostic methods and properties
- Framework-specific partial files add optimizations for specific .NET versions
- Conditional compilation (`#if NET8_0_OR_GREATER`) enables progressive enhancement
- **Applies to both interfaces AND classes** (e.g., `Csv.cs`, `Csv.net6.cs`, `Csv.net7.cs`, etc.)

### 2. Framework-Agnostic Design
- Interface comments focus on functionality, not implementation details
- Method names describe purpose rather than technical implementation
- No version-specific language in core interface documentation
- DateTimeOffset preferred over DateTime for timezone awareness

### 3. Single Responsibility Principle
- Each interface handles one core responsibility
- Error handling separated from reading operations
- Validation separated from parsing
- Configuration management isolated from data processing

### 4. Meaningful Comments
- Comments describe **WHAT** the code does, not **HOW** it's implemented
- Focus on user perspective, not developer perspective
- Avoid pattern-focused language ("facades", "delegates", "SRP")
- Use concrete benefits instead of abstract concepts
- Example: ✅ "Parses CSV content and returns each row" ❌ "Delegates to interface implementations"

### 5. Performance-First Design
- **Ultra-fast and low memory usage** is the primary goal
- Use ReadOnlySpan<char> for zero-allocation parsing
- Pre-allocate collections where possible
- Implement hardware acceleration through Vector operations
- Progressive enhancement: NET6+ → NET7+ → NET8+ → NET9+

### 6. Simple API with Advanced Options
- **Static Csv class** provides simple methods for common use cases
- **ICsvReaderBuilder** offers fluent configuration for advanced scenarios
- **ReadOnlySpan<char> overloads** for maximum performance
- **String overloads** for ease of use

## Critical Implementation Rules

### Partial Class Pattern
- **ALL interfaces and classes** must follow the partial pattern
- Core file contains framework-agnostic functionality
- Framework-specific files contain optimizations (net6.cs, net7.cs, net8.cs, net9.cs)
- Use `#if NET6_0_OR_GREATER` conditional compilation

### Architecture Relationships (CRITICAL)

**ICsvReader vs Csv Class - DO NOT CONFUSE THESE:**

**`ICsvReader`** (Interface):
- **Purpose**: Core CSV reading contract
- **Responsibility**: Define reading operations interface
- **Scope**: Technical implementation contract
- **Usage**: Used by implementations and builders

**`Csv`** (Static Class):
- **Purpose**: Simple API entry point (facade pattern)
- **Responsibility**: Provide easy-to-use static methods
- **Scope**: User-facing convenience API
- **Usage**: Primary entry point for simple operations

**Relationship**: 
```csharp
// Csv class delegates to ICsvReader implementations
public static IEnumerable<string[]> Read(string content)
{
    var reader = CreateReader(content, CsvOptions.Default); // Returns ICsvReader
    return ExtractRecords(reader);                          // Uses ICsvReader interface
}
```

### Builder Pattern Relationships

**`ICsvReaderBuilder`** (Interface):
- **Purpose**: Fluent configuration contract
- **Responsibility**: Define builder operations interface
- **Scope**: Configuration API contract
- **Usage**: Returned by `Csv.Configure()`

**`CsvReaderBuilder`** (Class):
- **Purpose**: Implementation of builder pattern
- **Responsibility**: Execute configuration and create readers
- **Scope**: Internal implementation
- **Usage**: Hidden behind interface

**Relationship**:
```csharp
// Interface defines the contract
public static ICsvReaderBuilder Configure() => new CsvReaderBuilder();

// Implementation handles the complexity
internal class CsvReaderBuilder : ICsvReaderBuilder
{
    // Uses ICsvReader, IFieldHandler, IValidationHandler, etc.
}
```

### Comment Guidelines
- ❌ **Bad**: "Follows Single Responsibility Principle: Simple API entry point only"
- ✅ **Good**: "Provides convenient methods for reading CSV data from strings and files"
- ❌ **Bad**: "Enable hardware acceleration"
- ✅ **Good**: "Enables CPU vector instructions for faster parsing of large files"

### Memory Optimization
- Always use ReadOnlySpan<char> in internal methods
- Pre-allocate collections with expected size
- Avoid string concatenation in hot paths
- Use ArrayPool for temporary allocations

### Progressive Enhancement
- **NET6+**: Vector operations, hardware acceleration
- **NET7+**: Async operations, advanced type parsing
- **NET8+**: SearchValues, auto-detection, frozen collections
- **NET9+**: Vector512 operations, advanced profiling

## File Organization

### Actual Current Structure
```
src/FastCsv/
├── Csv.cs                  # Core static API (partial)
├── Csv.net7.cs            # Async file operations
├── Csv.net8.cs            # Auto-detection features
├── ICsvReader.cs          # Core reader interface (partial)
├── ICsvReader.net6.cs     # Async enumerable support
├── ICsvReaderBuilder.cs   # Core builder interface (partial)
├── ICsvReaderBuilder.net6.cs # Hardware acceleration options
├── CsvMapper.cs           # Core mapper implementation (partial)
├── CsvMapper.net6.cs      # Async mapping support
├── CsvMapper.net8.cs      # Frozen collections optimization
├── FastCsvReader.cs       # Main reader implementation (partial)
├── FastCsvReader.net6.cs  # Async support
├── CsvRecord.cs           # Record implementation
├── CsvRow.cs              # Zero-allocation row struct
├── CsvParser.cs           # Core parsing logic
├── CsvFieldIterator.cs    # Field-by-field iteration
├── CsvFieldEnumerator.cs  # Field enumeration
├── CsvOptions.cs          # Configuration struct
├── CsvReadResult.cs       # Read results container
├── CsvValidationResult.cs # Validation results
├── ExtensionsToICsvRecord.cs # Extension methods
├── FastCsvParser.cs       # Optimized parser with SearchValues
├── RowEnumerable.cs       # Zero-allocation row enumeration
├── ICsvDataSource.cs      # Data source abstraction
├── ICsvRecord.cs          # Record interface
├── CsvReaderBuilder.cs    # Builder implementation
├── Errors/
│   ├── IErrorHandler.cs   # Error handling interface
│   ├── ErrorHandler.cs    # Default error handler
│   └── NullErrorHandler.cs # Null object pattern
└── Validation/
    ├── IValidationHandler.cs # Validation interface
    └── ValidationHandler.cs  # Default validation handler
```

### Framework-Specific Files (Partial Pattern)
The library uses the partial class pattern, but not as extensively as originally planned:
- **Csv**: Has .net7.cs and .net8.cs for async and auto-detection
- **ICsvReader**: Has .net6.cs for async enumerable
- **ICsvReaderBuilder**: Has .net6.cs for hardware options
- **CsvMapper**: Has .net6.cs and .net8.cs for async and frozen collections
- **FastCsvReader**: Has .net6.cs for async support

## Cleanup Notes

### Files to Remove
- `Meaningful_Comments_Example.md` - Content integrated into this file
- Any other random .md files created during development
- Only keep `CLAUDE.md` and `README.md` for documentation

### Development Workflow
1. Always follow the partial pattern for new interfaces/classes
2. Write meaningful comments focused on functionality
3. Test across multiple .NET versions
4. Optimize for zero-allocation parsing
5. Use TodoWrite tool to track progress on complex tasks

## Current API Design

### Progressive Disclosure Pattern (Current Implementation)
FastCsv implements a **progressive enhancement** approach with three main entry points:

1. **Simple Static API**: `Csv.Read()` for basic string array operations
2. **Object Mapping API**: `Csv.Read<T>()` for typed object mapping
3. **Builder API**: `Csv.Configure()` for advanced validation and error handling

### Current Usage Patterns
```csharp
// Level 1: Simple string array reading
var records = Csv.Read("Name,Age\nJohn,25\nJane,30");

// Level 2: Object mapping (major feature)
var employees = Csv.Read<Employee>(csvData);
var employeesWithMapping = Csv.ReadWithMapping<Employee>(csvData, 
    mapping => mapping.Map(e => e.Name, 0).Map(e => e.Age, 1));

// Level 3: Advanced with validation
var result = Csv.Configure()
    .WithContent(csvData)
    .WithValidation(true)
    .Read();

// Level 4: Async operations (.NET 7+)
await foreach (var record in Csv.ReadFileAsync("data.csv"))
{
    // Process each record
}
```

### Why This Design Works

1. **Progressive Disclosure**: Start simple, add complexity as needed
2. **Framework Enhancement**: Basic functionality works on all .NET versions, advanced features automatically available
3. **Type Safety**: Strongly typed options, IntelliSense-friendly fluent API
4. **Performance Conscious**: Simple methods have minimal overhead, advanced features only activated when requested

### Interface Composition Rules
Builder uses multiple interfaces for different concerns:
```csharp
private ICsvReader CreateReader(string content)
{
    var reader = // Create ICsvReader
    var validator = // Create IValidationHandler (if validation enabled)
    var errorHandler = // Create IErrorHandler (if error tracking enabled)
    var fieldHandler = // Create IFieldHandler
    
    // Compose them together
    return new CompositeReader(reader, validator, errorHandler, fieldHandler);
}
```

### Code Review Checklist (Current Standards)
- [ ] Does this class/interface have ONE clear responsibility?
- [ ] Are framework-specific features in partial files where appropriate?
- [ ] Do comments describe functionality, not implementation?
- [ ] Does the API follow simple → object mapping → advanced progression?
- [ ] Are extension methods properly organized in `ExtensionsToXXX` classes?
- [ ] Is the naming framework-agnostic and consistent?
- [ ] Are ReadOnlySpan<char> overloads provided for performance?
- [ ] Does object mapping support all three modes (Auto, Manual, Mixed)?
- [ ] Are validation errors properly typed and detailed?