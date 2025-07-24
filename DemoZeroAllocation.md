# Zero-Allocation CSV Parsing with FastCsv

The FastCsv library now supports true zero-allocation parsing through a unified data source provider pattern that handles strings, spans, and streams efficiently.

## Key Improvements

### 1. Unified Data Source Pattern
We've implemented `ICsvDataSource` interface with three implementations:
- `StringDataSource`: For traditional string-based CSV content
- `MemoryDataSource`: For `ReadOnlyMemory<char>` - enables zero-allocation parsing
- `StreamDataSource`: For file and network stream processing

### 2. Zero-Allocation API
```csharp
// Using ReadOnlyMemory<char> for zero allocations
ReadOnlyMemory<char> csvMemory = csvString.AsMemory();
var records = Csv.ReadAllRecords(csvMemory);

// Create a reader that supports reset
using var reader = Csv.CreateReader(csvMemory);
var count = reader.CountRecords();
reader.Reset(); // Supported for Memory and String sources
```

### 3. Architecture Benefits

#### Before (String Allocation)
```csharp
public static IReadOnlyList<string[]> ReadAllRecords(ReadOnlySpan<char> content, CsvOptions options = default)
{
    using var reader = CreateReader(content.ToString(), options); // ❌ Allocation!
    return reader.ReadAllRecords();
}
```

#### After (Zero Allocation)
```csharp
public static IReadOnlyList<string[]> ReadAllRecords(ReadOnlyMemory<char> content, CsvOptions options = default)
{
    var dataSource = new MemoryDataSource(content); // ✅ No allocation
    using var reader = new FastCsvReader(dataSource, options);
    return reader.ReadAllRecords();
}
```

## Implementation Details

### ICsvDataSource Interface
```csharp
internal interface ICsvDataSource : IDisposable
{
    bool TryReadLine(out ReadOnlySpan<char> line, out int lineNumber);
    void Reset();
    bool SupportsReset { get; }
    bool HasMoreData { get; }
}
```

### FastCsvReader Refactoring
- Removed direct string/stream storage
- Uses data source abstraction for all reading operations
- Maintains line number tracking through the data source
- Supports all three input types uniformly

### Performance Characteristics
1. **String Input**: Traditional heap allocation, full reset support
2. **Memory Input**: Zero additional allocations, full reset support
3. **Stream Input**: Buffered reading, reset only for seekable streams

## Usage Examples

### High-Performance Parsing
```csharp
// For maximum performance with large files
var csvData = File.ReadAllText("large.csv");
var memory = csvData.AsMemory();

// Count records without allocating field arrays
var count = Csv.CountRecords(memory);

// Parse with zero additional allocations
var records = Csv.ReadAllRecords(memory);
```

### Stream Processing
```csharp
using var stream = File.OpenRead("data.csv");
using var reader = Csv.CreateReader(stream);

// Process records one at a time
while (reader.TryReadRecord(out var record))
{
    ProcessRecord(record);
}
```

## Technical Notes

1. **Why ReadOnlyMemory instead of ReadOnlySpan?**
   - Spans cannot be stored in fields or used with delegates
   - Memory provides safe span access through the `.Span` property
   - Allows the reader to maintain state across method calls

2. **Allocation Points**
   - Field arrays are still allocated (one per record)
   - Quoted fields with escaped quotes require StringBuilder
   - All other parsing is allocation-free

3. **Future Enhancements**
   - Add async support to ICsvDataSource for true async streaming
   - Implement ArrayPool for field array allocations
   - Add specialized zero-copy APIs for specific use cases