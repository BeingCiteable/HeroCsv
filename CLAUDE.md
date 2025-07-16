# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains FastCsv, a high-performance CSV parsing and writing library for .NET. The library is designed for zero-allocation parsing using ref structs and spans, with performance optimizations for different .NET versions.

## Architecture

The library consists of a single file `src/FastCsv/FastCsv.cs` containing:

### Core Components
- **CsvOptions**: Configuration struct with delimiter, quote character, header settings, and .NET 8+ SearchValues optimizations
- **CsvReader**: Zero-allocation CSV reader using ref struct and ReadOnlySpan<char>
- **CsvRecord**: Represents a single CSV record with field enumeration
- **CsvFieldEnumerator**: Enumerates fields within a record
- **CsvWriter**: High-performance CSV writer using IBufferWriter<char>
- **PooledCsvWriter**: Memory-pooled buffer writer for CSV operations
- **CsvUtility**: Static utility methods for common operations

### Performance Features
- **Multi-target framework support**: NET6+, NET7+, NET8+ with conditional compilation
- **Zero-allocation parsing**: Uses ref structs and spans to avoid heap allocations
- **SIMD optimizations**: Vector512/Vector256 operations for NET8+/NET9+
- **SearchValues optimization**: Fast character searching in NET8+
- **Escaped quote handling**: Special path for fields with escaped quotes (only allocation point)

### Key Patterns
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
│       ├── FastCsv.cs          # Main library code
│       └── FastCsv.csproj      # Project file
├── tests/
│   └── FastCsv.Tests/
│       ├── UnitTest1.cs        # Basic tests
│       └── FastCsv.Tests.csproj # Test project
├── .vscode/                    # VS Code configuration
├── FastCsv.sln                 # Solution file
├── README.md                   # Project documentation
├── CLAUDE.md                   # This file
└── .gitignore                  # Git ignore rules
```

## NuGet Package Information

The project is configured as a NuGet package with:
- Multi-framework targeting (net6.0, net7.0, net8.0)
- Source Link support for debugging
- XML documentation generation
- Symbol packages (.snupkg) for debugging