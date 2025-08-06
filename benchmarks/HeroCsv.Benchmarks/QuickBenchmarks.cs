using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Columns;
using HeroCsv;
using HeroCsv.Models;
using System.Text;

namespace HeroCsv.Benchmarks;

/// <summary>
/// Quick benchmarks for CI/CD and rapid performance testing
/// </summary>
[MemoryDiagnoser]
[Config(typeof(QuickConfig))]
public class QuickBenchmarks
{
    private string _csvData100Rows = null!;
    private string _csvData1000Rows = null!;
    
    public class QuickConfig : ManualConfig
    {
        public QuickConfig()
        {
            // Short run for quick feedback
            AddJob(Job.ShortRun
                .WithWarmupCount(3)
                .WithIterationCount(5)
                .WithId("Quick"));
                
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(RankColumn.Arabic);
            
            AddExporter(JsonExporter.Default);
            AddExporter(MarkdownExporter.Default);
        }
    }
    
    public class SimpleRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
    
    [GlobalSetup]
    public void Setup()
    {
        _csvData100Rows = GenerateSimpleCsvData(100);
        _csvData1000Rows = GenerateSimpleCsvData(1000);
    }
    
    private string GenerateSimpleCsvData(int rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");
        
        for (int i = 1; i <= rows; i++)
        {
            sb.AppendLine($"{i},Item{i},{i * 10}");
        }
        
        return sb.ToString();
    }
    
    // ===== Core Operations (100 rows) =====
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Small")]
    public int ReadStringArray_100()
    {
        var count = 0;
        foreach (var record in Csv.ReadContent(_csvData100Rows))
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("Small")]
    public int CountRecords_100()
    {
        return Csv.CountRecords(_csvData100Rows);
    }
    
    [Benchmark]
    [BenchmarkCategory("Small")]
    public List<SimpleRecord> ReadTyped_100()
    {
        return Csv.Read<SimpleRecord>(_csvData100Rows).ToList();
    }
    
    // ===== Core Operations (1000 rows) =====
    
    [Benchmark]
    [BenchmarkCategory("Medium")]
    public int ReadStringArray_1000()
    {
        var count = 0;
        foreach (var record in Csv.ReadContent(_csvData1000Rows))
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("Medium")]
    public int CountRecords_1000()
    {
        return Csv.CountRecords(_csvData1000Rows);
    }
    
    [Benchmark]
    [BenchmarkCategory("Medium")]
    public List<SimpleRecord> ReadTyped_1000()
    {
        return Csv.Read<SimpleRecord>(_csvData1000Rows).ToList();
    }
    
    // ===== Advanced Features =====
    
    [Benchmark]
    [BenchmarkCategory("Advanced")]
    public CsvReadResult ReadWithValidation()
    {
        return Csv.Configure()
            .WithContent(_csvData100Rows)
            .WithValidation(true)
            .Read();
    }
    
#if NET8_0_OR_GREATER
    [Benchmark]
    [BenchmarkCategory("Advanced")]
    public int ReadWithAutoDetect()
    {
        var count = 0;
        foreach (var record in Csv.ReadAutoDetect(_csvData100Rows))
        {
            count++;
        }
        return count;
    }
#endif
}