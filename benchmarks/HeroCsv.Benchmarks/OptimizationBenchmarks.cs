using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Utilities;

namespace HeroCsv.Benchmarks;

/// <summary>
/// Benchmarks specifically for the new optimization features
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
public class OptimizationBenchmarks
{
    private string _csvWithCommonValues = null!;
    private string _csvWithMixedValues = null!;
    private string _longCsvLine = null!;
    private string _csvWithQuotes = null!;
    private CsvOptions _optionsWithPool = null!;
    private CsvOptions _optionsWithoutPool = null!;
    private StringPool _stringPool = null!;

    [GlobalSetup]
    public void Setup()
    {
        // CSV with many repeated common values (good for StringPool)
        var commonLines = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            commonLines.Add($"true,false,null,YES,NO,Active,Inactive,{i % 10}");
        }
        _csvWithCommonValues = string.Join("\n", commonLines);

        // CSV with mostly unique values (less benefit from StringPool)
        var mixedLines = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            mixedLines.Add($"unique_{i},value_{i},data_{i},{Guid.NewGuid()},{DateTime.Now.AddDays(i)}");
        }
        _csvWithMixedValues = string.Join("\n", mixedLines);

        // Very long single line (good for SearchValues)
        var longFields = Enumerable.Range(0, 500).Select(i => $"field_{i}");
        _longCsvLine = string.Join(",", longFields);

        // CSV with quoted fields
        _csvWithQuotes = @"""Name"",""Address"",""City"",""State""
""John Doe"",""123 Main St, Apt 4"",""New York"",""NY""
""Jane Smith"",""456 Oak Ave"",""Los Angeles"",""CA""
""Bob Johnson"",""789 Pine Rd"",""Chicago"",""IL""";

        _stringPool = new StringPool();
        _optionsWithPool = new CsvOptions(',', '"', false, stringPool: _stringPool);
        _optionsWithoutPool = new CsvOptions(',', '"', false);
    }

    [Benchmark(Description = "StringPool with Common Values")]
    public int ParseWithStringPoolCommon()
    {
        var count = 0;
        foreach (var row in Csv.ReadContent(_csvWithCommonValues, _optionsWithPool))
        {
            count += row.Length;
        }
        return count;
    }

    [Benchmark(Description = "No StringPool with Common Values")]
    public int ParseWithoutStringPoolCommon()
    {
        var count = 0;
        foreach (var row in Csv.ReadContent(_csvWithCommonValues, _optionsWithoutPool))
        {
            count += row.Length;
        }
        return count;
    }

    [Benchmark(Description = "StringPool with Unique Values")]
    public int ParseWithStringPoolUnique()
    {
        var count = 0;
        foreach (var row in Csv.ReadContent(_csvWithMixedValues, _optionsWithPool))
        {
            count += row.Length;
        }
        return count;
    }

#if NET8_0_OR_GREATER
    [Benchmark(Description = "SearchValues - Long Line")]
    public string[] ParseLongLineWithSearchValues()
    {
        return CsvParser.ParseLine(_longCsvLine.AsSpan(), _optionsWithoutPool);
    }

    [Benchmark(Description = "FrozenSet StringPool Lookup")]
    public void StringPoolFrozenSetLookup()
    {
        var pool = new StringPool();
        var testValues = new[] { "true", "false", "null", "YES", "NO", "0", "1" };
        
        for (int i = 0; i < 1000; i++)
        {
            foreach (var value in testValues)
            {
                _ = pool.GetOrAdd(value.AsSpan());
            }
        }
    }
#endif

#if NET9_0_OR_GREATER
    [Benchmark(Description = "Vector512 vs Regular Parse")]
    public void CompareVector512Parsing()
    {
        if (System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
        {
            var line = _longCsvLine.AsSpan();
            _ = CsvParser.ParseLineVector512(line, ',', _stringPool);
        }
        else
        {
            var line = _longCsvLine.AsSpan();
            _ = CsvParser.ParseLine(line, _optionsWithPool);
        }
    }
#endif

    [Benchmark(Description = "ArrayPool Buffer Reuse")]
    public void ArrayPoolBufferReuse()
    {
        for (int i = 0; i < 100; i++)
        {
            var buffer = ArrayPool<char>.Shared.Rent(4096);
            try
            {
                // Simulate some work with the buffer
                buffer[0] = 'A';
                buffer[buffer.Length - 1] = 'Z';
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer, clearArray: false);
            }
        }
    }

    [Benchmark(Description = "BufferPool Parse")]
    public List<string> ParseWithBufferPool()
    {
        return CsvParser.ParseLineWithArrayPool(_longCsvLine.AsSpan(), ',', _stringPool);
    }

    [Benchmark(Description = "Parse Quoted with Optimizations")]
    public int ParseQuotedFieldsOptimized()
    {
        var count = 0;
        foreach (var row in Csv.ReadContent(_csvWithQuotes, _optionsWithPool))
        {
            count += row.Length;
        }
        return count;
    }

    [Benchmark(Baseline = true, Description = "Traditional Approach")]
    public int TraditionalParse()
    {
        var count = 0;
        var lines = _csvWithCommonValues.Split('\n');
        foreach (var line in lines)
        {
            var fields = line.Split(',');
            count += fields.Length;
        }
        return count;
    }
}

/// <summary>
/// Micro-benchmarks for specific optimization features
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class MicroOptimizationBenchmarks
{
    private readonly string _testString = "test,value,here";
    private readonly StringPool _pool = new();

    [Benchmark(Description = "StringPool GetOrAdd")]
    public string StringPoolGetOrAdd()
    {
        return _pool.GetOrAdd(_testString.AsSpan());
    }

    [Benchmark(Description = "String Allocation")]
    public string DirectStringAllocation()
    {
        return new string(_testString.AsSpan());
    }

#if NET8_0_OR_GREATER
    [Benchmark(Description = "SearchValues Contains")]
    public bool SearchValuesContains()
    {
        var searchValues = System.Buffers.SearchValues.Create(",;\t|");
        return _testString.AsSpan().ContainsAny(searchValues);
    }

    [Benchmark(Description = "Manual Contains Check")]
    public bool ManualContains()
    {
        var span = _testString.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == ',' || span[i] == ';' || span[i] == '\t' || span[i] == '|')
                return true;
        }
        return false;
    }
#endif

    [Benchmark(Description = "ArrayPool Rent/Return")]
    public void ArrayPoolRentReturn()
    {
        var buffer = ArrayPool<char>.Shared.Rent(1024);
        ArrayPool<char>.Shared.Return(buffer, clearArray: false);
    }

    [Benchmark(Description = "Direct Array Allocation")]
    public void DirectArrayAllocation()
    {
        var buffer = new char[1024];
        _ = buffer[0]; // Prevent optimization
    }
}