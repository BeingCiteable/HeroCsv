using BenchmarkDotNet.Attributes;
using CsvHelper;
using System.Globalization;
using System.Text;
using nietras.SeparatedValues;
using FastCsv; // For Csv static class

namespace FastCsv.Benchmarks;

/// <summary>
/// Simplified comparison with major CSV libraries, avoiding namespace conflicts
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[RankColumn]
public class SimplifiedComparison
{
    private string _testCsv = "";
    private ReadOnlyMemory<char> _testMemory;

    [Params(100, 1000, 5000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _testCsv = GenerateTestCsv(RowCount);
        _testMemory = _testCsv.AsMemory();
    }

    private static string GenerateTestCsv(int rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,Name,Email,Age,City,Country");

        for (int i = 0; i < rows; i++)
        {
            sb.AppendLine($"{i},Person{i},person{i}@example.com,{25 + i % 50},City{i % 20},Country{i % 5}");
        }

        return sb.ToString();
    }

    // ============= FastCsv (Our Implementation) =============

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FastCsv")]
    public int FastCsv_String()
    {
        var records = global::FastCsv.Csv.ReadAllRecords(_testCsv);
        return records.Count;
    }

    [Benchmark]
    [BenchmarkCategory("FastCsv")]
    public int FastCsv_Memory()
    {
        var records = global::FastCsv.Csv.ReadAllRecords(_testMemory);
        return records.Count;
    }

    [Benchmark]
    [BenchmarkCategory("FastCsv")]
    public int FastCsv_CountOnly()
    {
        return global::FastCsv.Csv.CountRecords(_testCsv);
    }

    [Benchmark]
    [BenchmarkCategory("FastCsv")]
    public int FastCsv_Memory_CountOnly()
    {
        return global::FastCsv.Csv.CountRecords(_testMemory);
    }

    // ============= CsvHelper (Most Popular) =============

    [Benchmark]
    [BenchmarkCategory("CsvHelper")]
    public int CsvHelper_ReadAll()
    {
        using var reader = new StringReader(_testCsv);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var count = 0;
        while (csv.Read())
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("CsvHelper")]
    public int CsvHelper_GetRecords()
    {
        using var reader = new StringReader(_testCsv);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>().ToList();
        return records.Count;
    }

    // ============= Sylvan.Data.Csv (High Performance) =============

    [Benchmark]
    [BenchmarkCategory("Sylvan")]
    public int Sylvan_ReadAll()
    {
        using var reader = new StringReader(_testCsv);
        using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);

        var count = 0;
        while (csv.Read())
        {
            count++;
        }
        return count;
    }

    // ============= Sep (Modern High Performance) =============

    [Benchmark]
    [BenchmarkCategory("Sep")]
    public int Sep_ReadAll()
    {
        using var reader = Sep.Reader().FromText(_testCsv);

        var count = 0;
        foreach (var row in reader)
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("Sep")]
    public int Sep_CountOnly()
    {
        using var reader = Sep.Reader().FromText(_testCsv);

        var count = 0;
        foreach (var row in reader)
        {
            count++;
        }
        return count;
    }

    // ============= LumenWorks CsvReader =============

    [Benchmark]
    [BenchmarkCategory("LumenWorks")]
    public int LumenWorks_ReadAll()
    {
        using var reader = new StringReader(_testCsv);
        using var csv = new LumenWorks.Framework.IO.Csv.CsvReader(reader, true);

        var count = 0;
        while (csv.ReadNextRecord())
        {
            count++;
        }
        return count;
    }

    // ============= Raw Performance Baseline =============

    [Benchmark]
    [BenchmarkCategory("Baseline")]
    public int Raw_SplitLines()
    {
        var lines = _testCsv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length - 1; // Subtract header
    }

    [Benchmark]
    [BenchmarkCategory("Baseline")]
    public int Raw_CountNewlines()
    {
        var count = 0;
        foreach (var c in _testCsv)
        {
            if (c == '\n') count++;
        }
        return count - 1; // Subtract header line
    }

    [Benchmark]
    [BenchmarkCategory("Baseline")]
    public int Raw_SpanCountNewlines()
    {
        var span = _testCsv.AsSpan();
        var count = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '\n') count++;
        }
        return count - 1; // Subtract header line
    }
}