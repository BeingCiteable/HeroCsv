# HeroCsv Performance Benchmarks

[![Benchmarks](https://img.shields.io/badge/Benchmarks-Live%20Results-success)](https://github.com/BeingCiteable/HeroCsv/actions/workflows/performance-benchmarks.yml)
[![AOT Performance](https://img.shields.io/badge/AOT-2--5x%20Faster-green)](#aot-benchmarks-aot)

## 🎯 Full Transparency Initiative

HeroCsv provides **complete transparency** in performance claims with comprehensive benchmarks against **major .NET CSV libraries**. Every performance claim is backed by reproducible benchmarks.

## Benchmark Categories

### 1. Quick Benchmarks (`quick`)

Fast benchmarks for rapid testing:

- Small (100 rows) and medium (1000 rows) datasets
- Core operations: string arrays, counting, object mapping
- Advanced features: validation, auto-detection
- Runs on every pull request for regression detection

### 2. Feature Benchmarks (`features`)

Comprehensive testing of all HeroCsv features:

- **Core**: String arrays, counting, zero-allocation field iteration
- **Mapping**: Auto mapping, manual mapping with custom converters
- **I/O**: File and stream operations (sync and async)
- **Advanced**: Validation, error tracking, auto-detection (.NET 8+)

### 3. Competitor Benchmarks (`competitors`)

Transparent performance comparison with major libraries:

- **CsvHelper** - Most popular CSV library
- **Sylvan.Data.Csv** - High-performance CSV reader
- **Sep** - Modern high-performance parser

Tests multiple dataset sizes (100, 1000, 10000 rows) for:

- String array parsing
- Object mapping
- Count-only operations

### 4. AOT Benchmarks (`aot`)

Compares reflection-based vs AOT-safe mapping approaches:

- **Reflection Mapping** - Traditional reflection-based object mapping (baseline)
- **Factory Mapping** - AOT-safe factory functions (2-3x faster)
- **Source Generation** - Compile-time code generation (3-5x faster)
- **Mapping Overhead** - Micro-benchmarks for pure mapping performance

Expected improvements:

- 2-5x faster object mapping
- 60-70% memory reduction
- Full Native AOT compatibility

### 5. Large Dataset Benchmarks (`large`)

Performance testing with large datasets:

- **10K rows** - Small-to-medium dataset performance
- **100K rows** - Large dataset performance with competitor comparison
- **1M rows** - Extreme performance testing for scalability
- Memory efficiency analysis
- Stream vs in-memory processing comparison

### 6. Wide Dataset Benchmarks (`wide`)

Performance testing with many columns and diverse data types:

- **50 columns** - Moderate width dataset
- **100 columns** - Wide dataset performance
- **200 columns** - Extra-wide dataset handling
- **Mixed data types** - Complex headers and diverse field types including:
  - Integers, decimals, booleans, dates, GUIDs
  - Emails, phone numbers, IP addresses
  - Quoted values with embedded commas
  - Empty fields and special characters
- **Field access patterns** - Performance of accessing specific columns

### 7. Real Data Benchmarks (`realdata`)

Tests with actual CSV files of various sizes and complexities.

### 8. Library Comparisons (`library`)

Direct head-to-head comparisons with specific libraries.

### 9. Performance Analysis (`perf`)

Deep performance profiling and analysis.

## Running Benchmarks

### Local Development

```bash
cd benchmarks/HeroCsv.Benchmarks

# Run specific benchmark type
dotnet run -c Release -- quick         # Fast CI/CD benchmarks
dotnet run -c Release -- features      # All HeroCsv features
dotnet run -c Release -- competitors   # Library comparisons
dotnet run -c Release -- aot           # AOT mapping comparisons
dotnet run -c Release -- large         # Large datasets (10k, 100k, 1M rows)
dotnet run -c Release -- wide          # Wide datasets (50, 100, 200+ columns)
dotnet run -c Release -- realdata      # Real CSV files

# List all available benchmarks
dotnet run -c Release -- list
```

### CI/CD

Benchmarks run automatically on:

- **Pull requests**: Quick benchmarks with automatic comparison against master
- **Push to master**: Stores benchmark results for future comparisons
- **Releases**: Comprehensive features and competitor benchmarks on all platforms
- **Weekly schedule**: Sunday 2 AM UTC for performance tracking
- **Manual dispatch**: On-demand benchmark runs

## Results

### Viewing Results

- **Local**: Results are saved in `BenchmarkResults/BenchmarkDotNet/{benchmark-type}/`
- **CI**: Available as artifacts in GitHub Actions
- **Published**: Available on GitHub Pages after releases
- **PR Comments**: Automatic performance comparison comments on pull requests

### Result Formats

- **JSON**: Machine-readable detailed results (Brief and Full formats)
- **CSV**: For spreadsheet analysis
- **HTML**: Interactive web reports with charts
- **Markdown**: Human-readable summaries

### Automatic Comparisons

The CI/CD pipeline automatically:

1. **Compares PR benchmarks** against the master branch baseline
2. **Alerts on regressions** when performance degrades by >5% (PRs) or >10% (master)
3. **Comments on PRs** with detailed performance comparison
4. **Stores historical data** for trend analysis on GitHub Pages
5. **Creates issues** for significant performance regressions

## Performance Goals

HeroCsv aims for:

1. **Zero-allocation parsing** for most scenarios
2. **Fastest parsing speed** for common CSV formats
3. **Low memory footprint** even for large files
4. **Competitive object mapping** performance

## Contributing

When submitting performance improvements:

1. Run benchmarks before and after changes
2. Include benchmark results in PR description
3. Focus on real-world performance, not micro-optimizations
4. Ensure no regression in other scenarios

## Benchmark Integrity

To ensure fair comparisons:

- All libraries use their recommended/optimal configuration
- Same test data for all libraries
- Multiple runs with statistical analysis
- Tests on multiple platforms (Windows, Linux, macOS)
- No cherry-picking of results

## FAQ

### Why full transparency?

We believe users should make informed decisions based on real performance data, not marketing claims.

### How often are benchmarks updated?

- CI benchmarks: Every PR
- Comprehensive benchmarks: Every release
- Competitor comparisons: Every release and on-demand

### Can I trust these benchmarks?

- All benchmark code is open source
- Results are automatically generated
- No manual editing of results
- Multiple platforms tested
- Statistical analysis included

### My use case isn't covered

Please open an issue! We're happy to add more benchmark scenarios.
