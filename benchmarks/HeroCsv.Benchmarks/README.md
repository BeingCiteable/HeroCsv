# HeroCsv Benchmarks

Comprehensive performance testing suite for the HeroCsv CSV parsing library, designed to ensure consistent high performance and detect regressions.

## Quick Start

```bash
# Fast performance check for development (runs in ~1-2 minutes)
dotnet run -c Release -- ci

# Compare HeroCsv against other popular CSV libraries
dotnet run -c Release -- library simple

# Test with real-world CSV files of various sizes
dotnet run -c Release -- realdata

# View all available benchmark options
dotnet run -c Release -- list
```

## Benchmark Categories

### CI Benchmarks (`ci`)
Fast-running benchmarks designed for continuous integration pipelines. These tests verify core parsing performance hasn't regressed while keeping execution time minimal.

**Key features:**
- Completes in 1-2 minutes for rapid feedback
- Tests essential operations: parsing, counting, object mapping
- Uses synthetic data (100-1000 rows) for consistent results
- Automatically runs on all pull requests

### Library Comparison (`library`)
Head-to-head performance comparisons with other popular .NET CSV parsing libraries.

**Subcommands:**
- `simple` - Comprehensive comparison with CsvHelper, Sylvan.Data.Csv, Sep, and others
- `direct` - Focused comparison between HeroCsv and Sep (the current performance leader)

**What's measured:**
- Parse time for various file sizes
- Memory allocations
- Throughput (rows/second)

### Real Data Benchmarks (`realdata`)
Performance testing with actual CSV files to ensure the library performs well with real-world data patterns.

**Test files include:**
- Small files (< 1MB): Configuration data, lookup tables
- Medium files (1-50MB): Typical business data exports
- Large files (> 50MB): Big data exports, logs

### Performance Analysis (`perf`)
Deep-dive performance analysis for optimization work.

**Subcommands:**
- `quick` - Rapid performance overview with key metrics
- `internal` - Component-level benchmarks (parser, field iterator, memory pools)

## CI/CD Integration

### Automatic Triggers
Benchmarks run automatically on:
- **Pull Requests**: CI benchmarks only, comparing against master baseline
- **Master Push**: Full CI suite, results stored as performance baseline
- **Manual Dispatch**: Any benchmark type via GitHub Actions UI

### Performance Gates
- **Pull Requests**: Warning at >5% regression, helps catch issues early
- **Master Branch**: Fails at >10% regression, prevents performance degradation

### Results Storage
- Benchmark artifacts stored for 30 days
- JSON reports enable historical comparison
- Performance trends tracked over time

## Running Locally

### Basic Usage
```bash
# Run with default settings
dotnet run -c Release -- [command]

# Specify custom output directory
dotnet run -c Release -- ci --output ./my-results

# Enable detailed logging
dotnet run -c Release -- ci --verbose
```

### Development Workflow
1. Make performance-related changes
2. Run `ci` benchmarks for quick validation
3. Run relevant specific benchmarks (e.g., `library` if changing parser)
4. Compare results against baseline

## Understanding Results

### Key Metrics
- **Mean**: Average execution time (lower is better)
- **Error**: Standard error of measurements
- **StdDev**: Standard deviation (lower means more consistent)
- **Allocated**: Memory allocated per operation (aim for zero)

### Example Output
```
| Method              | Mean     | Error   | StdDev  | Allocated |
|-------------------- |---------:|--------:|--------:|----------:|
| ReadContent_100Rows | 23.45 μs | 0.12 μs | 0.10 μs |      2 KB |
```

## Writing New Benchmarks

### Guidelines
- Use meaningful method names that describe what's being tested
- Include setup data in `[GlobalSetup]` to avoid measuring initialization
- Add `[MemoryDiagnoser]` to track allocations
- Keep individual benchmark execution under 100ms for CI suite

### Example
```csharp
[MemoryDiagnoser]
[JsonExporter("*-report.json")] // Enables CI comparison
public class MyNewBenchmark
{
    private string _csvData;
    
    [GlobalSetup]
    public void Setup()
    {
        // Prepare test data once, outside of measurements
        _csvData = GenerateRealisticCsvData(rows: 1000);
    }
    
    [Benchmark]
    public int ParseWithValidation()
    {
        // Test specific functionality
        var result = Csv.Configure()
            .WithContent(_csvData)
            .WithValidation(true)
            .Read();
            
        return result.RecordCount;
    }
}
```

## Troubleshooting

### Common Issues

**"No benchmarks found"**
- Ensure your benchmark class is public
- Verify methods have `[Benchmark]` attribute

**High variance in results**
- Close other applications
- Disable CPU throttling
- Run with `--job long` for more iterations

**Out of memory**
- Reduce test data size
- Check for memory leaks in setup

## Contributing

When submitting performance improvements:
1. Run relevant benchmarks before and after changes
2. Include benchmark results in PR description
3. Explain any tradeoffs (e.g., memory vs speed)
4. Add new benchmarks for new features