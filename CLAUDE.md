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

### Core Interfaces
- **ICsvReader**: Zero-allocation CSV reader with framework-specific enhancements
- **ICsvRecord**: Represents a single CSV record with field access capabilities
- **IFieldHandler**: Handles field parsing and processing operations
- **ICsvValidator**: Validates CSV data structure and content
- **IErrorHandler**: Manages error reporting and statistics
- **ICsvErrorReporter**: Specialized error reporting and management
- **IConfigurationHandler**: Handles CSV configuration and format detection
- **IPositionHandler**: Manages position tracking and navigation
- **IValidationHandler**: Validates CSV structure and fields

### Interface Organization
Interfaces are organized by feature area:
- **Fields/**: Field handling and processing
- **Records/**: Record access and manipulation
- **Errors/**: Error handling and reporting
- **Validation/**: Data validation
- **Configuration/**: Configuration management
- **Navigation/**: Position tracking and navigation

### Partial Interface Pattern
Each interface uses the partial interface pattern with framework-specific enhancements:
- **Core interface**: Framework-agnostic base functionality
- **net6.cs**: Hardware acceleration features
- **net7.cs**: Fast parsing and type conversion
- **net8.cs**: Optimized collections and character detection
- **net9.cs**: Advanced hardware acceleration

### Performance Features
- **Multi-target framework support**: NET6+, NET7+, NET8+ with conditional compilation
- **Zero-allocation parsing**: Uses ref structs and spans to avoid heap allocations
- **SIMD optimizations**: Vector512/Vector256 operations for NET8+/NET9+
- **SearchValues optimization**: Fast character searching in NET8+
- **Escaped quote handling**: Special path for fields with escaped quotes (only allocation point)

### Key Patterns
- **Partial Interface Pattern**: Framework-specific enhancements through partial interfaces
- **Feature-based Organization**: Interfaces grouped by functionality (Fields, Errors, Validation, etc.)
- **Single Responsibility Principle**: Each interface handles one core responsibility
- **Framework-agnostic Design**: Core interfaces focus on functionality, not implementation details
- **Progressive Enhancement**: Framework-specific optimizations through conditional compilation
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

## Project Structure

```
FastCsv/
├── src/
│   └── FastCsv/
│       ├── ICsvReader.cs           # Core CSV reader interface
│       ├── ICsvReader.net6.cs      # Hardware acceleration enhancements
│       ├── ICsvReader.net7.cs      # Fast parsing enhancements
│       ├── ICsvReader.net8.cs      # Optimized collections enhancements
│       ├── ICsvReader.net9.cs      # Advanced hardware acceleration
│       ├── ICsvRecord.cs           # Core record interface
│       ├── ICsvRecord.net7.cs      # Type conversion enhancements
│       ├── ICsvRecord.net8.cs      # Named field access enhancements
│       ├── ICsvRecord.net9.cs      # Advanced field operations
│       ├── Fields/
│       │   ├── IFieldHandler.cs    # Core field handling
│       │   ├── IFieldHandler.net6.cs # Hardware acceleration
│       │   ├── IFieldHandler.net8.cs # Character detection optimization
│       │   └── IFieldHandler.net9.cs # Advanced acceleration
│       ├── Errors/
│       │   ├── IErrorHandler.cs    # Core error handling
│       │   ├── IErrorHandler.net6.cs # Error statistics
│       │   ├── IErrorHandler.net8.cs # Advanced collections
│       │   ├── ICsvErrorReporter.cs # Core error reporting
│       │   ├── ICsvErrorReporter.net6.cs # Statistics
│       │   └── ICsvErrorReporter.net8.cs # Collections
│       ├── Validation/
│       │   ├── ICsvValidator.cs    # Core validation
│       │   ├── ICsvValidator.net6.cs # Hardware acceleration
│       │   ├── ICsvValidator.net8.cs # Character detection
│       │   ├── ICsvValidator.net9.cs # Advanced validation
│       │   ├── IValidationHandler.cs # Core validation handling
│       │   ├── IValidationHandler.net6.cs # Acceleration
│       │   └── IValidationHandler.net8.cs # Detection
│       ├── Configuration/
│       │   ├── IConfigurationHandler.cs # Core configuration
│       │   └── IConfigurationHandler.net8.cs # Advanced config
│       ├── Navigation/
│       │   ├── IPositionHandler.cs # Core position tracking
│       │   ├── IPositionHandler.net6.cs # Performance tracking
│       │   ├── IPositionHandler.net8.cs # Line counting
│       │   └── IPositionHandler.net9.cs # Advanced counting
│       └── FastCsv.csproj          # Project file
├── tests/
│   └── FastCsv.Tests/
│       ├── UnitTest1.cs            # Basic tests
│       └── FastCsv.Tests.csproj    # Test project
├── .vscode/                        # VS Code configuration
├── FastCsv.sln                     # Solution file
├── README.md                       # Project documentation
├── CLAUDE.md                       # This file
└── .gitignore                      # Git ignore rules
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

```
src/FastCsv/
├── Csv.cs                  # Core static API (partial)
├── Csv.net6.cs            # Hardware acceleration
├── Csv.net7.cs            # Async operations
├── Csv.net8.cs            # SearchValues optimization
├── Csv.net9.cs            # Vector512 operations
├── ICsvReader.cs          # Core reader interface
├── ICsvReader.net6.cs     # Hardware acceleration
├── ICsvReader.net7.cs     # Advanced parsing
├── ICsvReader.net8.cs     # Optimized collections
├── ICsvReader.net9.cs     # Advanced acceleration
├── ICsvReaderBuilder.cs   # Core builder interface
├── ICsvReaderBuilder.net6.cs # Hardware options
├── ICsvReaderBuilder.net9.cs # Profiling options
├── CsvReaderBuilder.cs    # Builder implementation
├── Fields/
│   ├── IFieldHandler.cs   # Core field handling
│   ├── IFieldHandler.net6.cs
│   ├── IFieldHandler.net8.cs
│   └── IFieldHandler.net9.cs
├── Errors/
│   ├── IErrorHandler.cs
│   ├── IErrorHandler.net6.cs
│   ├── IErrorHandler.net8.cs
│   ├── ICsvErrorReporter.cs
│   ├── ICsvErrorReporter.net6.cs
│   └── ICsvErrorReporter.net8.cs
├── Validation/
├── Configuration/
└── Navigation/
```

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

## API Design Philosophy

### Progressive Disclosure Pattern
FastCsv follows a **progressive enhancement** approach:

1. **Simple by default**: `Csv.Read()` for basic operations (90% of cases)
2. **Fluent when needed**: `Csv.Configure()` for advanced scenarios (10% of cases)
3. **Framework-aware**: Automatically uses best features for your .NET version

### Usage Progression
```csharp
// Level 1: Simple (90% of cases)
var records = Csv.Read("Name,Age\nJohn,25\nJane,30");

// Level 2: Intermediate (custom options)
var records = Csv.Read(csvData, ';'); // Custom delimiter
var records = Csv.ReadWithHeaders(csvData); // Headers as dictionary

// Level 3: Advanced (complex scenarios)
var result = Csv.Configure()
    .WithFile("data.csv")
    .WithValidation(true)
    .WithErrorTracking(true)
    .ReadAdvanced();
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

### Code Review Checklist
- [ ] Does this class/interface have ONE clear responsibility?
- [ ] Are framework-specific features in partial files?
- [ ] Do comments describe functionality, not implementation?
- [ ] Does the API follow simple → advanced progression?
- [ ] Are interfaces composed, not inherited?
- [ ] Is the naming framework-agnostic?
- [ ] Are ReadOnlySpan<char> overloads provided for performance?