# FastCsv

A high-performance, zero-allocation CSV parsing and writing library for .NET with multi-framework support and SIMD optimizations.

## Features

- **Zero-allocation parsing** using ref structs and spans
- **High performance** with SIMD optimizations for .NET 6+
- **Multi-framework support** (.NET Standard 2.0, .NET 6, .NET 7, .NET 8, .NET 9)
- **Advanced features** like auto-detection, vectorized operations, and memory pooling
- **Easy to use** with intuitive API design
- **Comprehensive** - handles quoted fields, escaped quotes, and custom delimiters

## Installation

```bash
dotnet add package FastCsv
```

## Quick Start

### Reading CSV

```csharp
using FastCsv;

// Basic reading
var csvData = "Name,Age,City\r\nJohn,25,\"New York\"\r\nJane,30,London";
var reader = new CsvReader(csvData);

reader.SkipHeader(); // Skip header if present

foreach (var record in reader)
{
    foreach (var field in record)
    {
        Console.Write($"'{field}' ");
    }
    Console.WriteLine();
}
```

### Writing CSV

```csharp
using FastCsv;

// Basic writing
using var pooledWriter = new PooledCsvWriter();
var writer = new CsvWriter(pooledWriter);

writer.WriteHeader("Name", "Age", "City");
writer.WriteRecord("John", "25", "New York");
writer.WriteRecord("Jane", "30", "London");

Console.WriteLine(pooledWriter.ToString());
```

### File Operations

```csharp
using FastCsv;

// Read from file
var records = CsvUtility.ReadFile("data.csv");
foreach (var record in records)
{
    Console.WriteLine($"Record from line {record.LineNumber}: {string.Join(", ", record.ToStringArray())}");
}

// Write to file
var data = new[]
{
    new[] { "John", "25", "New York" },
    new[] { "Jane", "30", "London" }
};
CsvUtility.WriteFile("output.csv", data, new[] { "Name", "Age", "City" });
```

## Advanced Features

### Auto-detection (.NET 8+)

```csharp
// Automatically detect CSV format
var records = CsvUtility.ReadFileAutoDetect("unknown_format.csv");
```

### Custom Options

```csharp
var options = new CsvOptions(
    delimiter: ';',
    quote: '"',
    hasHeader: true,
    trimWhitespace: true,
    newLine: "\r\n"
);

var reader = new CsvReader(csvData, options);
```

### Async Operations (.NET 8+)

```csharp
// Asynchronous reading with UTF-8 optimization
var records = await CsvUtility.ReadFileAsync("large_data.csv");
```

## Performance

FastCsv is designed for maximum performance:

- **Zero-allocation parsing** using ref structs and ReadOnlySpan<char>
- **SIMD optimizations** for vectorized operations on .NET 6+
- **SearchValues** for ultra-fast character scanning on .NET 8+
- **Memory pooling** to reduce garbage collection pressure
- **Aggressive inlining** for optimal JIT compilation

### Benchmarks

Performance varies by .NET version due to progressive optimizations:

- **.NET Standard 2.0**: Fallback implementations for compatibility
- **.NET 6**: Vector-based operations for large records
- **.NET 7**: Enhanced span operations
- **.NET 8**: SearchValues and FrozenCollections for maximum speed
- **.NET 9**: Vector512 support and enhanced JIT optimizations

## Architecture

The library consists of several key components:

- **CsvReader**: Zero-allocation CSV reader using ref struct
- **CsvWriter**: High-performance CSV writer with IBufferWriter<char>
- **CsvRecord**: Represents a single CSV record with field enumeration
- **CsvRecordWrapper**: Wrapper for use with IEnumerable in utility methods
- **PooledCsvWriter**: Memory-pooled buffer writer for CSV operations
- **CsvUtility**: Static utility methods for common operations

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

## Acknowledgments

This library is inspired by the need for high-performance CSV processing in .NET applications while maintaining zero-allocation principles and leveraging the latest .NET performance improvements.