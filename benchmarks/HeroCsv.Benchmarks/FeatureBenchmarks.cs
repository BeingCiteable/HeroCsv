using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Columns;
using HeroCsv;
using HeroCsv.Mapping;
using HeroCsv.Models;
using HeroCsv.Parsing;
using System.Text;

namespace HeroCsv.Benchmarks;

/// <summary>
/// Benchmarks for all HeroCsv features to prevent performance regressions
/// </summary>
[MemoryDiagnoser]
[Config(typeof(FeatureConfig))]
public class FeatureBenchmarks
{
    private string _csvData100Rows = null!;
    private string _csvData1000Rows = null!;
    private string _tempFilePath = null!;
    private MemoryStream _memoryStream = null!;
    
    public class FeatureConfig : ManualConfig
    {
        public FeatureConfig()
        {
            // Default job for feature benchmarks
            AddJob(Job.Default
                .WithId("Features"));
                
            AddColumn(StatisticColumn.Min);
            AddColumn(StatisticColumn.Max);
            AddColumn(StatisticColumn.Median);
            AddColumn(RankColumn.Arabic);
            
            AddExporter(JsonExporter.Default);
            AddExporter(CsvExporter.Default);
            AddExporter(HtmlExporter.Default);
            AddExporter(MarkdownExporter.Default);
        }
    }
    
    /// <summary>
    /// Test data class
    /// </summary>
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
    }
    
    [GlobalSetup]
    public void Setup()
    {
        _csvData100Rows = GenerateCsvData(100);
        _csvData1000Rows = GenerateCsvData(1000);
        
        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, _csvData1000Rows);
        
        var bytes = Encoding.UTF8.GetBytes(_csvData1000Rows);
        _memoryStream = new MemoryStream(bytes);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
            
        _memoryStream?.Dispose();
    }
    
    private string GenerateCsvData(int rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Age,City,Salary,HireDate,IsActive");
        
        var names = new[] { "John", "Jane", "Mike", "Sarah", "Tom", "Lisa" };
        var cities = new[] { "New York", "London", "Tokyo", "Paris", "Berlin", "Sydney" };
        var random = new Random(42);
        
        for (int i = 1; i <= rows; i++)
        {
            var name = names[random.Next(names.Length)];
            var age = 20 + random.Next(50);
            var city = cities[random.Next(cities.Length)];
            var salary = 30000 + random.Next(70000);
            var hireDate = new DateTime(2020, 1, 1).AddDays(random.Next(1000)).ToString("yyyy-MM-dd");
            var isActive = random.Next(10) > 1;
            
            sb.AppendLine($"{i},{name},{age},{city},{salary},{hireDate},{isActive}");
        }
        
        return sb.ToString();
    }
    
    // ===== Core Features =====
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Core", "StringArray")]
    public int ReadStringArray()
    {
        var count = 0;
        foreach (var record in Csv.ReadContent(_csvData1000Rows))
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("Core", "Counting")]
    public int CountRecords()
    {
        return Csv.CountRecords(_csvData1000Rows);
    }
    
    [Benchmark]
    [BenchmarkCategory("Core", "ZeroAlloc")]
    public int FieldIterator()
    {
        var fieldCount = 0;
        foreach (var field in CsvFieldIterator.IterateFields(_csvData1000Rows.AsSpan(), new CsvOptions()))
        {
            fieldCount++;
        }
        return fieldCount;
    }
    
    // ===== Object Mapping =====
    
    [Benchmark]
    [BenchmarkCategory("Mapping", "Auto")]
    public List<Person> AutoMapping()
    {
        return Csv.Read<Person>(_csvData1000Rows).ToList();
    }
    
    [Benchmark]
    [BenchmarkCategory("Mapping", "Manual")]
    public List<Person> ManualMapping()
    {
        var builder = new CsvMappingBuilder<Person>();
        builder.Map(p => p.Id, 0);
        builder.Map(p => p.Name, 1);
        builder.Map(p => p.Age, 2);
        builder.Map(p => p.City, 3);
        builder.Map(p => p.Salary, 4);
        builder.Map(p => p.HireDate, 5).WithFormat("yyyy-MM-dd");
        builder.Map(p => p.IsActive, 6);
        var mapping = builder.Build();
        
        return Csv.Read<Person>(_csvData1000Rows, mapping).ToList();
    }
    
    // ===== I/O Operations =====
    
    [Benchmark]
    [BenchmarkCategory("IO", "File")]
    public int ReadFile()
    {
        var count = 0;
        foreach (var record in Csv.ReadFile(_tempFilePath))
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    [BenchmarkCategory("IO", "Stream")]
    public int ReadStream()
    {
        _memoryStream.Position = 0;
        var count = 0;
        foreach (var record in Csv.ReadStream(_memoryStream, leaveOpen: true))
        {
            count++;
        }
        return count;
    }
    
#if NET7_0_OR_GREATER
    [Benchmark]
    [BenchmarkCategory("IO", "AsyncFile")]
    public async Task<int> ReadFileAsync()
    {
        var records = await Csv.ReadFileAsync(_tempFilePath, CsvOptions.Default, cancellationToken: default);
        return records.Count;
    }
    
    [Benchmark]
    [BenchmarkCategory("IO", "AsyncStream")]
    public async Task<int> ReadStreamAsync()
    {
        _memoryStream.Position = 0;
        var records = await Csv.ReadStreamAsync(_memoryStream, leaveOpen: true);
        return records.Count;
    }
#endif
    
    // ===== Advanced Features =====
    
    [Benchmark]
    [BenchmarkCategory("Advanced", "Validation")]
    public CsvReadResult ReadWithValidation()
    {
        return Csv.Configure()
            .WithContent(_csvData1000Rows)
            .WithValidation(true)
            .Read();
    }
    
#if NET8_0_OR_GREATER
    [Benchmark]
    [BenchmarkCategory("Advanced", "AutoDetect")]
    public int ReadWithAutoDetect()
    {
        var count = 0;
        foreach (var record in Csv.ReadAutoDetect(_csvData1000Rows))
        {
            count++;
        }
        return count;
    }
#endif
}