# HeroCsv v1.0.0 Release Notes

## 🎉 First Stable Release

We're excited to announce the first stable release of HeroCsv, an ultra-fast, low memory CSV parsing library for .NET.

## ✨ Key Features

### Performance
- **Zero-allocation parsing** using ReadOnlySpan<char>
- **SIMD optimizations** for .NET 8+ and .NET 9+
- **SearchValues optimization** for ultra-fast character scanning (.NET 8+)
- **Hardware acceleration** with Vector256/Vector512 operations

### Core Functionality
- **Simple static API** - `Csv.Read()` for quick string array operations
- **Generic object mapping** - `Csv.Read<T>()` with automatic property mapping
- **Manual column mapping** - Map by index with `CsvMapper<T>`
- **Custom type converters** - Support for any data type
- **Comprehensive validation** - Detailed error reporting with line/column info

### Advanced Features
- **Async operations** - File and stream processing (.NET 7+)
- **Auto-detection** - Automatic CSV format and delimiter detection (.NET 8+)
- **Fluent builder API** - Advanced configuration with `Csv.Configure()`
- **Multi-framework support** - .NET Standard 2.0, .NET 6-9

### Design Philosophy
- **Reading-focused** - Optimized specifically for CSV parsing (no writing)
- **Progressive enhancement** - Better performance on newer .NET versions
- **Simple by default** - Complex only when needed

## 📊 Performance

Benchmarked against popular CSV libraries:
- **Row enumeration**: 0.22 ms/op (1000 rows, 100 iterations)
- **Count-only**: 0.06 ms/op 
- **Field iteration**: 0.31 ms/op (unique zero-allocation feature)

## 🚀 Getting Started

```csharp
// Simple usage
var records = Csv.Read("Name,Age\nJohn,25\nJane,30");

// Object mapping
var employees = Csv.Read<Employee>(csvData);

// Advanced configuration
var result = Csv.Configure()
    .WithDelimiter(';')
    .WithValidation(true)
    .Read<Product>();
```

## 📦 NuGet Package

```bash
dotnet add package HeroCsv
```

## 🙏 Acknowledgments

Special thanks to all contributors who helped shape this first release!

---

For detailed documentation and examples, visit our [GitHub repository](https://github.com/BeingCiteable/HeroCsv).