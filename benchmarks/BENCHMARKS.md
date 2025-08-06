# HeroCsv Performance Benchmarks

## Full Transparency Initiative

HeroCsv is committed to complete transparency in performance claims. We provide comprehensive benchmarks comparing our library against all major CSV parsing libraries in the .NET ecosystem.

## Benchmark Categories

### 1. Quick Benchmarks (`quick`)
Fast benchmarks for CI/CD and rapid testing:
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

### 4. Real Data Benchmarks (`realdata`)
Tests with actual CSV files of various sizes and complexities.

### 5. Library Comparisons (`library`)
Direct head-to-head comparisons with specific libraries.

### 6. Performance Analysis (`perf`)
Deep performance profiling and analysis.

## Running Benchmarks

### Local Development
```bash
cd benchmarks/HeroCsv.Benchmarks

# Run specific benchmark type
dotnet run -c Release -- quick         # Fast CI/CD benchmarks
dotnet run -c Release -- features      # All HeroCsv features
dotnet run -c Release -- competitors   # Library comparisons
dotnet run -c Release -- realdata      # Real CSV files

# List all available benchmarks
dotnet run -c Release -- list
```

### CI/CD
Benchmarks run automatically on:
- Pull requests (quick benchmarks)
- Releases (features and competitor comparisons)
- Manual workflow dispatch

## Results

### Viewing Results
- **Local**: Results are saved in `BenchmarkDotNet.Artifacts/results/`
- **CI**: Available as artifacts in GitHub Actions
- **Published**: Available on GitHub Pages after releases

### Result Formats
- **JSON**: Machine-readable detailed results
- **CSV**: For spreadsheet analysis
- **HTML**: Interactive web reports
- **Markdown**: Human-readable summaries

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