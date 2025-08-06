using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Columns;
using System.Text;
using System.Globalization;

// CSV Libraries
using HeroCsv;
using CsvHelper;
using nietras.SeparatedValues;
using Sylvan.Data.Csv;

namespace HeroCsv.Benchmarks;

/// <summary>
/// Transparent performance comparison with major CSV libraries
/// </summary>
[MemoryDiagnoser]
[Config(typeof(CompetitorConfig))]
public class CompetitorBenchmarks
{
    private string _csvData100Rows = null!;
    private string _csvData1000Rows = null!;
    private string _csvData10000Rows = null!;
    
    public class CompetitorConfig : ManualConfig
    {
        public CompetitorConfig()
        {
            AddJob(Job.Default
                .WithId("Competition"));
                
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Median);
            AddColumn(RankColumn.Arabic);
            
            AddExporter(JsonExporter.Default);
            AddExporter(CsvExporter.Default);
            AddExporter(HtmlExporter.Default);
            AddExporter(MarkdownExporter.Default);
        }
    }
    
    public class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
        public decimal Salary { get; set; }
    }
    
    [Params(100, 1000, 10000)]
    public int RowCount { get; set; }
    
    private string _testData = null!;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _csvData100Rows = GenerateCsvData(100);
        _csvData1000Rows = GenerateCsvData(1000);
        _csvData10000Rows = GenerateCsvData(10000);
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        _testData = RowCount switch
        {
            100 => _csvData100Rows,
            1000 => _csvData1000Rows,
            10000 => _csvData10000Rows,
            _ => _csvData1000Rows
        };
    }
    
    private string GenerateCsvData(int rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Age,City,Salary");
        
        var names = new[] { "John", "Jane", "Mike", "Sarah" };
        var cities = new[] { "New York", "London", "Tokyo", "Paris" };
        var random = new Random(42);
        
        for (int i = 1; i <= rows; i++)
        {
            var name = names[random.Next(names.Length)];
            var age = 20 + random.Next(50);
            var city = cities[random.Next(cities.Length)];
            var salary = 30000 + random.Next(70000);
            
            sb.AppendLine($"{i},{name},{age},{city},{salary}");
        }
        
        return sb.ToString();
    }
    
    // ===== String Array Parsing =====
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("StringArray")]
    public int HeroCsv_StringArray()
    {
        var count = 0;
        foreach (var record in Csv.ReadContent(_testData))
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("StringArray")]
    public int CsvHelper_StringArray()
    {
        using var reader = new StringReader(_testData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var count = 0;
        while (csv.Read())
        {
            var record = csv.Parser.Record;
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("StringArray")]
    public int Sep_StringArray()
    {
        using var reader = Sep.Reader().FromText(_testData);
        var count = 0;
        foreach (var row in reader)
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("StringArray")]
    public int Sylvan_StringArray()
    {
        using var reader = Sylvan.Data.Csv.CsvDataReader.Create(new StringReader(_testData));
        var count = 0;
        while (reader.Read())
        {
            count++;
        }
        return count;
    }
    
    // ===== Object Mapping =====
    
    [Benchmark]
    [BenchmarkCategory("ObjectMapping")]
    public List<TestRecord> HeroCsv_ObjectMapping()
    {
        return Csv.Read<TestRecord>(_testData).ToList();
    }
    
    [Benchmark]
    [BenchmarkCategory("ObjectMapping")]
    public List<TestRecord> CsvHelper_ObjectMapping()
    {
        using var reader = new StringReader(_testData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<TestRecord>().ToList();
    }
    
    [Benchmark]
    [BenchmarkCategory("ObjectMapping")]
    public List<TestRecord> Sylvan_ObjectMapping()
    {
        using var reader = Sylvan.Data.Csv.CsvDataReader.Create(new StringReader(_testData));
        var results = new List<TestRecord>();
        while (reader.Read())
        {
            results.Add(new TestRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2),
                City = reader.GetString(3),
                Salary = reader.GetDecimal(4)
            });
        }
        return results;
    }
    
    // ===== Count Only (Minimal Processing) =====
    
    [Benchmark]
    [BenchmarkCategory("CountOnly")]
    public int HeroCsv_CountOnly()
    {
        return Csv.CountRecords(_testData);
    }
    
    [Benchmark]
    [BenchmarkCategory("CountOnly")]
    public int Sep_CountOnly()
    {
        using var reader = Sep.Reader().FromText(_testData);
        var count = 0;
        foreach (var row in reader)
        {
            count++;
        }
        return count;
    }
}