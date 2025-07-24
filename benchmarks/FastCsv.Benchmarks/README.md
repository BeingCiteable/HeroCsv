# FastCsv Benchmark Suite

Comprehensive benchmarking comparing FastCsv with popular .NET CSV libraries.

## 📊 Libraries Compared

| Library | Version | Description | Notes |
|---------|---------|-------------|-------|
| **FastCsv** | Current | Our zero-allocation implementation | Baseline for comparison |
| [CsvHelper](https://github.com/JoshClose/CsvHelper) | 33.0.1 | Most popular .NET CSV library | Feature-rich, widely adopted |
| [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan) | 1.3.9 | High-performance CSV reader | Optimized for speed |
| [CsvReader](https://github.com/phatcher/CsvReader) | 1.2.2 | Lightweight CSV parser | Minimal dependencies |
| [Sep](https://github.com/nietras/Sep) | 0.5.2 | Modern high-performance parser | .NET 6+ optimized |
| [ServiceStack.Text](https://github.com/ServiceStack/ServiceStack.Text) | 8.6.0 | Full-featured serialization | Part of ServiceStack |
| [Csv](https://github.com/stevehansen/csv) | 2.0.93 | Kent Boggart's CSV library | Simple API |

## 🏃 Benchmark Suites

### 1. Library Comparison (`comparison`)
- **Purpose**: Compare parsing speed across different libraries
- **Dataset Sizes**: 10, 1,000, 10,000 rows
- **Metrics**: Execution time, memory allocation, throughput
- **Focus**: Real-world CSV parsing performance

### 2. Memory Allocation Analysis (`memory`)
- **Purpose**: Analyze memory usage and allocation patterns
- **Focus**: Zero-allocation capabilities of FastCsv
- **Comparisons**: String vs Memory vs Stream approaches
- **Metrics**: Allocated bytes, GC pressure, allocation rate

### 3. File I/O Performance (`file`)
- **Purpose**: Test file reading performance
- **Scenarios**: File streams, memory-mapped files, in-memory content
- **Focus**: Real-world file processing scenarios
- **Dataset**: 5,000 rows with realistic data patterns

### 4. Original FastCsv Tests (`original`)
- **Purpose**: Internal FastCsv performance validation
- **Focus**: String vs Memory vs Span performance within FastCsv
- **Metrics**: Detailed FastCsv feature benchmarks

## 🚀 Running Benchmarks

```bash
# Show available benchmark suites
dotnet run --project benchmarks/FastCsv.Benchmarks

# Run specific benchmark suite
dotnet run --project benchmarks/FastCsv.Benchmarks -- comparison
dotnet run --project benchmarks/FastCsv.Benchmarks -- memory
dotnet run --project benchmarks/FastCsv.Benchmarks -- file
dotnet run --project benchmarks/FastCsv.Benchmarks -- original

# Run all benchmarks (takes a while)
dotnet run --project benchmarks/FastCsv.Benchmarks -- all
```

## 📈 Expected Results

### Performance Hierarchy (Predicted)
1. **Sep** - Modern, highly optimized for .NET 6+
2. **FastCsv (Memory)** - Our zero-allocation implementation
3. **Sylvan.Data.Csv** - Known high-performance library
4. **FastCsv (String)** - Our standard implementation
5. **CsvReader** - Lightweight, decent performance
6. **CsvHelper** - Feature-rich but potentially slower
7. **ServiceStack.Text** - General-purpose, not CSV-specialized
8. **Csv** - Simple implementation

### Memory Efficiency (Predicted)
1. **FastCsv (Memory)** - Zero additional allocations
2. **Sep** - Modern memory-efficient design
3. **Sylvan.Data.Csv** - Optimized memory usage
4. **FastCsv (String)** - Standard allocation patterns
5. **CsvReader** - Lightweight allocations
6. **Others** - Varying allocation patterns

## 🎯 Optimization Targets

Based on benchmark results, we can identify:

1. **Performance Gaps**: Where FastCsv trails competitors
2. **Memory Inefficiencies**: Unnecessary allocations to eliminate
3. **API Improvements**: Better APIs for common use cases
4. **Feature Parity**: Missing features that affect performance

## 📊 Analyzing Results

BenchmarkDotNet provides comprehensive output including:

- **Mean/Median**: Average execution time
- **Allocated**: Memory allocated per operation
- **Rank**: Performance ranking
- **Ratio**: Performance relative to baseline
- **Gen0/Gen1/Gen2**: Garbage collection pressure

### Key Metrics to Watch
- **Memory Allocation**: Lower is better (bytes allocated)
- **Execution Time**: Lower is better (nanoseconds)
- **Allocation Rate**: How much memory per operation
- **GC Pressure**: Generation 0/1/2 collections

## 🔧 Configuration

Benchmarks run with:
- **.NET 9.0** target framework
- **Release** configuration
- **3 warmup iterations**, **5 measurement iterations**
- **Memory diagnostics** enabled
- **Cross-platform** compatible (Windows diagnostics when available)

## 📝 Contributing Benchmark Results

When contributing benchmark results:

1. Include system specifications (CPU, RAM, OS)
2. Note any background processes or unusual conditions
3. Run benchmarks multiple times for consistency
4. Include both raw BenchmarkDotNet output and summary analysis

## 🎯 Baseline Goals

Target performance goals for FastCsv:
- **Top 3** in speed benchmarks
- **Lowest** memory allocation for Memory API
- **Competitive** with specialized high-performance libraries
- **Superior** memory efficiency for zero-allocation scenarios