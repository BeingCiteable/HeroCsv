# HeroCsv v1.0.0 Release Notes

**Release Date:** TBD  
**Download:** [NuGet Package](https://www.nuget.org/packages/HeroCsv/1.0.0)

## 🎉 First Stable Release

HeroCsv v1.0.0 is the first stable release of our **ultra-fast, zero-allocation CSV parsing library** with **full Native AOT support**. Perfect for cloud-native, serverless, and container deployments where startup time and deployment size matter.

### ✨ Key Features

- **🚀 AOT Ready**: Full Native AOT and trimming support for ultra-fast startup and minimal deployment size
- **⚡ Zero-Allocation Parsing**: Uses `ReadOnlySpan<char>` and ref structs for maximum performance
- **🏗️ Multi-Framework Support**: Targets NET6, NET7, NET8, and NET9 with progressive enhancements
- **🧮 SIMD Optimizations**: Hardware acceleration for NET8+ with Vector512/Vector256 operations
- **🎯 Object Mapping**: Generic `CsvMapper<T>` with automatic and manual column mapping
- **📡 Async Operations**: Full async support for file and stream operations (.NET 7+)
- **🤖 Auto-Detection**: Automatic CSV format and delimiter detection (.NET 8+)

### 🚀 Performance Highlights

- **Native AOT**: 10x faster startup, 70% smaller deployments compared to JIT
- **Row Enumeration**: 0.22 ms/op (faster than Sep's 0.25 ms/op)
- **Count-Only Operations**: 0.06 ms/op (faster than Sep's 0.10 ms/op)
- **Zero Heap Allocations**: Only allocates for escaped quote handling
- **SIMD Acceleration**: Up to 3x faster parsing on NET8+ with Vector operations

### 📊 API Examples

```csharp
// Simple reading - AOT compatible
var records = Csv.Read("Name,Age\nJohn,25");

// Object mapping - works with AOT source generation
var employees = Csv.Read<Employee>(csvData);

// Advanced configuration
var result = Csv.Configure()
    .WithContent(csvData)
    .WithValidation(true)
    .Read();

// Zero-allocation processing - perfect for AOT
foreach (var field in CsvFieldIterator.IterateFields(csvData, options))
{
    // Process field.Value (ReadOnlySpan<char>)
}
```

### 🛠️ Native AOT Integration

**Source Generation (Recommended for AOT):**
```csharp
// Enable AOT-optimized mapping with source generation
[GenerateCsvMapping]
public partial class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
}

// Zero reflection, 100% AOT compatible
var employees = csvData.ReadCsvEmployee().ToList();
```

**Factory-Based (AOT-Safe):**
```csharp
// No attributes needed - works anywhere
var employees = Csv.Read(csvData, record => new Employee
{
    Name = record[0].ToString(),
    Age = record[1].ToInt32(),
    Salary = record[2].ToDecimal()
});
```

### 🧪 Quality Assurance

- **601+ passing tests** across all major scenarios
- **AOT compilation validation** on all supported platforms
- **Comprehensive zero-allocation tests** for critical performance paths
- **Cross-framework testing** on NET6 through NET9
- **Memory allocation validation** with detailed benchmarks
- **Native AOT benchmarks** comparing startup time and deployment size

### 📝 Breaking Changes

None - this is the first stable API release.

### 🔄 Migration from Prerelease

If upgrading from prerelease versions, update your package reference:

```xml
<PackageReference Include="HeroCsv" Version="1.0.0" />
```

### 🙏 Contributors

This release was made possible by the HeroCsv community. Thank you to all contributors and issue reporters who helped make this library production-ready.

### 🎯 Why Choose HeroCsv?

- **✅ Cloud Native**: Deploy with minimal footprint using Native AOT
- **✅ Container Optimized**: Smaller Docker images, faster cold starts
- **✅ Serverless Ready**: Perfect for AWS Lambda, Azure Functions with AOT
- **✅ High Performance**: Zero-allocation design beats all competitors
- **✅ Modern .NET**: Takes full advantage of latest .NET performance improvements

---

**🚀 Ready for Production:** This stable release is recommended for production use, especially in AOT scenarios.