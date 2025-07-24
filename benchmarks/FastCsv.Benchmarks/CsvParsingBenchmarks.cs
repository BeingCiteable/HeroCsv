using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using System.Text;

namespace FastCsv.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CsvParsingBenchmarks
{
    private string _smallCsv = "";
    private string _mediumCsv = "";
    private string _largeCsv = "";

    private ReadOnlyMemory<char> _smallMemory;
    private ReadOnlyMemory<char> _mediumMemory;
    private ReadOnlyMemory<char> _largeMemory;

    [GlobalSetup]
    public void Setup()
    {
        // Small CSV (10 rows)
        var sb = new StringBuilder();
        sb.AppendLine("ID,Name,Age,City,Country");
        for (int i = 0; i < 10; i++)
        {
            sb.AppendLine($"{i},Person{i},{20 + i},City{i},Country{i}");
        }
        _smallCsv = sb.ToString();
        _smallMemory = _smallCsv.AsMemory();

        // Medium CSV (1,000 rows)
        sb.Clear();
        sb.AppendLine("ID,Name,Age,City,Country,Description");
        for (int i = 0; i < 1_000; i++)
        {
            sb.AppendLine($"{i},Person{i},{20 + i % 50},City{i % 100},Country{i % 20},\"This is a description for person {i}\"");
        }
        _mediumCsv = sb.ToString();
        _mediumMemory = _mediumCsv.AsMemory();

        // Large CSV (100,000 rows)
        sb.Clear();
        sb.AppendLine("ID,Name,Age,City,Country,Description,Value1,Value2,Value3");
        for (int i = 0; i < 100_000; i++)
        {
            sb.AppendLine($"{i},Person{i},{20 + i % 50},City{i % 100},Country{i % 20},\"Description {i}\",{i * 100},{i * 200},{i * 300}");
        }
        _largeCsv = sb.ToString();
        _largeMemory = _largeCsv.AsMemory();
    }

    // Small dataset benchmarks
    [Benchmark(Baseline = true)]
    public int SmallCsv_String()
    {
        var records = Csv.ReadAllRecords(_smallCsv);
        return records.Count;
    }

    [Benchmark]
    public int SmallCsv_Memory()
    {
        var records = Csv.ReadAllRecords(_smallMemory);
        return records.Count;
    }

    [Benchmark]
    public int SmallCsv_Span()
    {
        // This will convert to string internally
        var records = Csv.ReadAllRecords(_smallCsv.AsSpan());
        return records.Count;
    }

    // Medium dataset benchmarks
    [Benchmark]
    public int MediumCsv_String()
    {
        var records = Csv.ReadAllRecords(_mediumCsv);
        return records.Count;
    }

    [Benchmark]
    public int MediumCsv_Memory()
    {
        var records = Csv.ReadAllRecords(_mediumMemory);
        return records.Count;
    }

    // Large dataset benchmarks
    [Benchmark]
    public int LargeCsv_String()
    {
        var records = Csv.ReadAllRecords(_largeCsv);
        return records.Count;
    }

    [Benchmark]
    public int LargeCsv_Memory()
    {
        var records = Csv.ReadAllRecords(_largeMemory);
        return records.Count;
    }

    // Count-only benchmarks (minimal allocations)
    [Benchmark]
    public int CountOnly_String()
    {
        return Csv.CountRecords(_mediumCsv);
    }

    [Benchmark]
    public int CountOnly_Memory()
    {
        return Csv.CountRecords(_mediumMemory);
    }

    // Reader with reset benchmarks
    [Benchmark]
    public int ReaderWithReset_String()
    {
        using var reader = Csv.CreateReader(_smallCsv);
        var count1 = reader.CountRecords();
        reader.Reset();
        var count2 = reader.CountRecords();
        return count1 + count2;
    }

    [Benchmark]
    public int ReaderWithReset_Memory()
    {
        using var reader = Csv.CreateReader(_smallMemory);
        var count1 = reader.CountRecords();
        reader.Reset();
        var count2 = reader.CountRecords();
        return count1 + count2;
    }
}