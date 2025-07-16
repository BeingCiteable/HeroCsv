# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains FastCsv, a high-performance CSV parsing and writing library for .NET. The library is designed for zero-allocation parsing using ref structs and spans, with performance optimizations for different .NET versions.

## Architecture

The library follows a modular interface-based architecture with feature-based organization:

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

### Partial Interface Pattern
- Each interface is split into core functionality and framework-specific enhancements
- Core interfaces contain framework-agnostic methods and properties
- Framework-specific partial files add optimizations for specific .NET versions
- Conditional compilation (`#if NET8_0_OR_GREATER`) enables progressive enhancement

### Framework-Agnostic Design
- Interface comments focus on functionality, not implementation details
- Method names describe purpose rather than technical implementation
- No version-specific language in core interface documentation
- DateTimeOffset preferred over DateTime for timezone awareness

### Single Responsibility Principle
- Each interface handles one core responsibility
- Error handling separated from reading operations
- Validation separated from parsing
- Configuration management isolated from data processing