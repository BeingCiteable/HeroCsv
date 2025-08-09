using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Utilities;

namespace HeroCsv.Benchmarks;

[Config(typeof(AllocationConfig))]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class AllocationBenchmark
{
    private class AllocationConfig : ManualConfig
    {
        public AllocationConfig()
        {
            AddJob(Job.Default.WithGcMode(new GcMode { Force = false }));
            AddDiagnoser(MemoryDiagnoser.Default);
            // AddColumn(BenchmarkDotNet.Columns.AllocatedColumn.Default); // Not available in older versions
        }
    }

    private string _simpleCsvLine = null!;
    private string _complexCsvLine = null!;
    private string _largeCsvContent = null!;
    private CsvOptions _options;
    private StringPool _stringPool = null!;
    private ReadOnlyMemory<char> _csvMemory;

    [GlobalSetup]
    public void Setup()
    {
        _simpleCsvLine = "field1,field2,field3,field4,field5,field6,field7,field8,field9,field10";
        _complexCsvLine = "\"quoted,field\",normal,\"escaped\"\"quote\",123,45.67,true,2024-01-01,\"multi\nline\",last";

        // Generate large CSV content
        var lines = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            lines.Add($"row{i},value{i},data{i},{i},{i * 1.5},true,2024-01-{(i % 28) + 1:D2}");
        }
        _largeCsvContent = string.Join("\n", lines);
        _csvMemory = _largeCsvContent.AsMemory();

        _stringPool = new StringPool();
        _options = new CsvOptions(',', '"', false, stringPool: _stringPool);
    }

    [Benchmark(Description = "Parse Simple Line - No Allocations")]
    public string[] ParseSimpleLine()
    {
        return CsvParser.ParseLine(_simpleCsvLine.AsSpan(), _options);
    }

    [Benchmark(Description = "Parse with StringPool - Minimal Allocations")]
    public string[] ParseWithStringPool()
    {
        var span = _simpleCsvLine.AsSpan();
        return CsvParser.ParseLine(span, _options);
    }

#if NET9_0_OR_GREATER
    [Benchmark(Description = "Vector512 Parse - Zero Extra Allocations")]
    public string[] ParseVector512()
    {
        if (System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
        {
            var result = CsvParser.ParseLineVector512(_simpleCsvLine.AsSpan(), ',', _stringPool);
            return result.ToArray();
        }
        return [];
    }
#endif

    [Benchmark(Description = "Parse Whole Buffer - Zero Allocation Enumeration")]
    public int ParseWholeBufferZeroAlloc()
    {
        var count = 0;
        foreach (var row in CsvParser.ParseWholeBuffer(_csvMemory.Span, _options))
        {
            count += row.FieldCount;
        }
        return count;
    }

    [Benchmark(Description = "CsvFieldIterator - True Zero Allocation")]
    public int IterateFieldsZeroAlloc()
    {
        var count = 0;
        foreach (var field in CsvFieldIterator.IterateFields(_csvMemory.Span, _options))
        {
            count++;
            // Access field.Value which is ReadOnlySpan<char> - no allocation
            _ = field.Value.Length;
        }
        return count;
    }

    [Benchmark(Description = "ArrayPool Buffer Parse")]
    public List<string> ParseWithArrayPool()
    {
        return CsvParser.ParseLineWithArrayPool(_simpleCsvLine.AsSpan(), ',', _stringPool);
    }

    [Benchmark(Baseline = true, Description = "Traditional String.Split")]
    public string[] TraditionalSplit()
    {
        return _simpleCsvLine.Split(',');
    }
}

/// <summary>
/// Benchmark specifically for measuring GC pressure
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[GcServer(true)]
[GcConcurrent(true)]
[MemoryDiagnoser]
public class GcPressureBenchmark
{
    private string _csvContent = null!;
    private CsvOptions _options;

    [Params(100, 1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var lines = new List<string>();
        for (int i = 0; i < RowCount; i++)
        {
            lines.Add($"field1_{i},field2_{i},field3_{i},field4_{i},field5_{i}");
        }
        _csvContent = string.Join("\n", lines);
        _options = CsvOptions.Default;
    }

    [Benchmark(Description = "HeroCsv Parse - Measure GC")]
    public int HeroCsvParse()
    {
        var count = 0;
        foreach (var row in Csv.ReadContent(_csvContent))
        {
            count += row.Length;
        }
        return count;
    }

    [Benchmark(Description = "Zero Alloc Iterator - Measure GC")]
    public int ZeroAllocIterator()
    {
        var count = 0;
        foreach (var field in CsvFieldIterator.IterateFields(_csvContent.AsSpan(), _options))
        {
            count++;
        }
        return count;
    }
}