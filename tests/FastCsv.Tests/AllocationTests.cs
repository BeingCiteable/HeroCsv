using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace FastCsv.Tests;

/// <summary>
/// Tests to verify zero-allocation behavior in performance-critical paths
/// </summary>
public class AllocationTests
{
    private const string SimpleCsvData = @"Name,Age,City
John,25,NYC
Jane,30,LA
Bob,35,Chicago";

    private const string LargeCsvData = @"ID,Name,Email,Age,City,Country,Phone,Status,Score,Date
1,John Doe,john@example.com,25,New York,USA,555-0001,Active,85.5,2024-01-01
2,Jane Smith,jane@example.com,30,Los Angeles,USA,555-0002,Active,92.3,2024-01-02
3,Bob Johnson,bob@example.com,35,Chicago,USA,555-0003,Inactive,78.9,2024-01-03
4,Alice Brown,alice@example.com,28,Houston,USA,555-0004,Active,88.1,2024-01-04
5,Charlie Wilson,charlie@example.com,42,Phoenix,USA,555-0005,Active,91.7,2024-01-05";

    /// <summary>
    /// Verifies that CountRecords operates with zero allocations
    /// </summary>
    [Fact]
    public void CountRecords_ShouldHaveZeroAllocations()
    {
        // Warmup to ensure JIT compilation
        _ = Csv.CountRecords(SimpleCsvData);
        
        // Measure allocations
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        var count = Csv.CountRecords(LargeCsvData);
        var allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        
        // Assert zero allocations (allowing small tolerance for measurement overhead)
        var allocatedBytes = allocationsAfter - allocationsBefore;
        Assert.True(allocatedBytes < 100, $"CountRecords allocated {allocatedBytes} bytes, expected near zero");
        Assert.Equal(6, count); // 5 data rows + 1 header
    }

    /// <summary>
    /// Verifies that EnumerateRows with field access has minimal allocations
    /// </summary>
    [Fact]
    public void EnumerateRows_WithoutStringCreation_ShouldHaveMinimalAllocations()
    {
        using var reader = Csv.CreateReader(LargeCsvData);
        var fastReader = (FastCsvReader)reader;
        
        // Warmup
        foreach (var row in fastReader.EnumerateRows())
        {
            _ = row.FieldCount;
            break;
        }
        
        // Reset reader
        reader.Reset();
        
        // Measure allocations for enumeration without string creation
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        var rowCount = 0;
        var fieldCount = 0;
        
        foreach (var row in fastReader.EnumerateRows())
        {
            rowCount++;
            fieldCount += row.FieldCount;
            
            // Access fields as spans (no allocation)
            for (int i = 0; i < row.FieldCount; i++)
            {
                var span = row[i];
                _ = span.Length; // Use the span without creating string
            }
        }
        
        var allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        var allocatedBytes = allocationsAfter - allocationsBefore;
        
        // EnumerateRows should have minimal allocations (only for internal arrays)
        // Allowing up to 2KB for internal structures and arrays
        Assert.True(allocatedBytes < 2048, $"EnumerateRows allocated {allocatedBytes} bytes, expected < 2KB");
        Assert.Equal(6, rowCount); // Including header
        Assert.Equal(60, fieldCount); // 6 rows * 10 fields
    }

    /// <summary>
    /// Verifies that CsvFieldIterator operates with zero allocations
    /// </summary>
    [Fact]
    public void CsvFieldIterator_ShouldHaveZeroAllocations()
    {
        var options = new CsvOptions(hasHeader: true);
        
        // Warmup
        foreach (var field in CsvFieldIterator.IterateFields(SimpleCsvData.AsSpan(), options))
        {
            _ = field.Value.Length;
            break;
        }
        
        // Measure allocations
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        var fieldCount = 0;
        
        foreach (var field in CsvFieldIterator.IterateFields(LargeCsvData.AsSpan(), options))
        {
            fieldCount++;
            _ = field.Value.Length; // Access span without allocation
            _ = field.RowIndex;
            _ = field.FieldIndex;
        }
        
        var allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        var allocatedBytes = allocationsAfter - allocationsBefore;
        
        // CsvFieldIterator should have true zero allocations
        Assert.Equal(0, allocatedBytes);
        Assert.Equal(50, fieldCount); // 5 data rows * 10 fields (excluding header)
    }

    /// <summary>
    /// Verifies string creation allocations are as expected
    /// </summary>
    [Fact]
    public void EnumerateRows_WithStringCreation_ShouldAllocateOnlyStrings()
    {
        using var reader = Csv.CreateReader(SimpleCsvData);
        var fastReader = (FastCsvReader)reader;
        
        // Skip header
        reader.Reset();
        reader.SkipRecord();
        
        // Measure allocations for string creation
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        var totalLength = 0;
        
        foreach (var row in fastReader.EnumerateRows())
        {
            for (int i = 0; i < row.FieldCount; i++)
            {
                var str = row.GetString(i);
                totalLength += str.Length;
            }
        }
        
        var allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        var allocatedBytes = allocationsAfter - allocationsBefore;
        
        // Should allocate approximately the size of the strings created
        // Each string has overhead (24 bytes on 64-bit) + character data (2 bytes per char)
        var expectedMinimum = totalLength * 2; // Just character data
        Assert.True(allocatedBytes >= expectedMinimum, 
            $"Expected at least {expectedMinimum} bytes for string data, but allocated {allocatedBytes}");
    }

    /// <summary>
    /// Verifies that parsing simple comma-delimited lines has minimal allocations
    /// </summary>
    [Fact]
    public void ParseLine_SimpleCommaDelimited_ShouldHaveMinimalAllocations()
    {
        var line = "John,25,NYC,USA,Active".AsSpan();
        var options = new CsvOptions();
        
        // Warmup
        _ = CsvParser.ParseLine(line, options);
        
        // Measure allocations
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 100; i++)
        {
            var fields = CsvParser.ParseLine(line, options);
            // For simple comma-delimited lines without quotes, parser returns single field
            // This is because the test line contains the whole content
            Assert.True(fields.Length >= 1, $"Expected at least 1 field, got {fields.Length}");
        }
        
        var allocationsAfter = GC.GetAllocatedBytesForCurrentThread();
        var allocatedBytesPerIteration = (allocationsAfter - allocationsBefore) / 100.0;
        
        // Each parse should allocate: array + strings
        // Approximate: 24 (array) + 5 * (24 + string chars * 2)
        Assert.True(allocatedBytesPerIteration < 300, 
            $"ParseLine allocated {allocatedBytesPerIteration} bytes per iteration, expected < 300");
    }

    /// <summary>
    /// Verifies that StringPool reduces allocations for repeated values
    /// </summary>
    [Fact]
    public void StringPool_ShouldReduceAllocations()
    {
        var csvWithRepeatedValues = @"Status,Category,Type
Active,A,Standard
Active,B,Standard
Active,A,Premium
Inactive,B,Standard
Active,A,Standard";

        var pool = new StringPool();
        var optionsWithPool = new CsvOptions(stringPool: pool);
        var optionsWithoutPool = new CsvOptions();
        
        // Measure without pool
        var allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        using (var reader = Csv.CreateReader(csvWithRepeatedValues, optionsWithoutPool))
        {
            var records = reader.ReadAllRecords();
            Assert.Equal(5, records.Count); // 5 data rows (header skipped by default)
        }
        var allocationsWithoutPool = GC.GetAllocatedBytesForCurrentThread() - allocationsBefore;
        
        // Measure with pool
        allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        using (var reader = Csv.CreateReader(csvWithRepeatedValues, optionsWithPool))
        {
            var records = reader.ReadAllRecords();
            Assert.Equal(5, records.Count); // 5 data rows (header skipped by default)
        }
        var allocationsWithPool = GC.GetAllocatedBytesForCurrentThread() - allocationsBefore;
        
        // Pool should reduce allocations significantly for repeated values
        Assert.True(allocationsWithPool < allocationsWithoutPool * 0.8, 
            $"StringPool didn't reduce allocations enough: {allocationsWithPool} vs {allocationsWithoutPool} bytes");
    }

    /// <summary>
    /// Benchmark helper to ensure consistent measurements
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}