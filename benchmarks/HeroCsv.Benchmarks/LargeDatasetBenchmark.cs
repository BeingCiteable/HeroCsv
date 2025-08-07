using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using nietras.SeparatedValues;
using CsvHelper;
using Sylvan.Data.Csv;

namespace HeroCsv.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net60)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LargeDatasetBenchmark
{
    private string _csvData10k = "";
    private string _csvData100k = "";
    private string _csvData1M = "";
    private byte[] _csvBytes10k = Array.Empty<byte>();
    private byte[] _csvBytes100k = Array.Empty<byte>();
    private byte[] _csvBytes1M = Array.Empty<byte>();
    
    [GlobalSetup]
    public void Setup()
    {
        // Generate 10k rows
        _csvData10k = GenerateCsvData(10_000);
        _csvBytes10k = Encoding.UTF8.GetBytes(_csvData10k);
        
        // Generate 100k rows
        _csvData100k = GenerateCsvData(100_000);
        _csvBytes100k = Encoding.UTF8.GetBytes(_csvData100k);
        
        // Generate 1M rows (for extreme testing)
        _csvData1M = GenerateCsvData(1_000_000);
        _csvBytes1M = Encoding.UTF8.GetBytes(_csvData1M);
    }
    
    private static string GenerateCsvData(int rowCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Age,Department,Salary,HireDate,IsActive,Notes");
        
        var random = new Random(42); // Fixed seed for reproducibility
        var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations" };
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        
        for (int i = 1; i <= rowCount; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var age = random.Next(22, 65);
            var department = departments[random.Next(departments.Length)];
            var salary = random.Next(40000, 150000);
            var hireYear = random.Next(2010, 2024);
            var hireMonth = random.Next(1, 13);
            var hireDay = random.Next(1, 29);
            var isActive = random.Next(100) > 10 ? "true" : "false";
            var notes = i % 100 == 0 ? $"\"Special employee, milestone {i}\"" : "Regular employee";
            
            sb.AppendLine($"{i},{firstName} {lastName},{firstName.ToLower()}.{lastName.ToLower()}@company.com,{age},{department},{salary},{hireYear}-{hireMonth:D2}-{hireDay:D2},{isActive},{notes}");
        }
        
        return sb.ToString();
    }
    
    // 10K Benchmarks
    [BenchmarkCategory("10K"), Benchmark(Baseline = true)]
    public int HeroCsv_10k()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData10k);
        return records.Count;
    }
    
    [BenchmarkCategory("10K"), Benchmark]
    public int HeroCsv_Stream_10k()
    {
        using var stream = new MemoryStream(_csvBytes10k);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }
    
    [BenchmarkCategory("10K"), Benchmark]
    public int HeroCsv_Count_10k()
    {
        return HeroCsv.Csv.CountRecords(_csvData10k);
    }
    
    // 100K Benchmarks
    [BenchmarkCategory("100K"), Benchmark(Baseline = true)]
    public int HeroCsv_100k()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData100k);
        return records.Count;
    }
    
    [BenchmarkCategory("100K"), Benchmark]
    public int HeroCsv_Stream_100k()
    {
        using var stream = new MemoryStream(_csvBytes100k);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }
    
    [BenchmarkCategory("100K"), Benchmark]
    public int HeroCsv_Count_100k()
    {
        return HeroCsv.Csv.CountRecords(_csvData100k);
    }
    
    [BenchmarkCategory("100K"), Benchmark]
    public int HeroCsv_FieldIterator_100k()
    {
        var fieldCount = 0;
        var options = new global::HeroCsv.Models.CsvOptions();
        foreach (var field in HeroCsv.Parsing.CsvFieldIterator.IterateFields(_csvData100k, options))
        {
            fieldCount++;
        }
        return fieldCount;
    }
    
    // 1M Benchmarks (extreme)
    [BenchmarkCategory("1M"), Benchmark(Baseline = true)]
    public int HeroCsv_1M()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData1M);
        return records.Count;
    }
    
    [BenchmarkCategory("1M"), Benchmark]
    public int HeroCsv_Stream_1M()
    {
        using var stream = new MemoryStream(_csvBytes1M);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }
    
    [BenchmarkCategory("1M"), Benchmark]
    public int HeroCsv_Count_1M()
    {
        return HeroCsv.Csv.CountRecords(_csvData1M);
    }
    
    // Memory efficiency tests
    [BenchmarkCategory("Memory"), Benchmark]
    public long HeroCsv_Memory_100k()
    {
        var before = GC.GetTotalMemory(true);
        var records = HeroCsv.Csv.ReadAllRecords(_csvData100k);
        var after = GC.GetTotalMemory(false);
        return (after - before) / records.Count; // Bytes per record
    }
    
    [BenchmarkCategory("Memory"), Benchmark]
    public long HeroCsv_StreamMemory_100k()
    {
        var before = GC.GetTotalMemory(true);
        using var stream = new MemoryStream(_csvBytes100k);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        var after = GC.GetTotalMemory(false);
        return (after - before) / count; // Bytes per record
    }
}

// Competitor comparison for 100k rows
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class LargeDatasetCompetitorBenchmark
{
    private string _csvData = "";
    private byte[] _csvBytes = Array.Empty<byte>();
    
    [Params(100_000)]
    public int RowCount { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Age,Department,Salary,HireDate,IsActive");
        
        var random = new Random(42);
        for (int i = 1; i <= RowCount; i++)
        {
            sb.AppendLine($"{i},Person{i},person{i}@example.com,{20 + i % 50},Dept{i % 10},{40000 + i * 10},2020-01-{(i % 28) + 1:D2},true");
        }
        
        _csvData = sb.ToString();
        _csvBytes = Encoding.UTF8.GetBytes(_csvData);
    }
    
    [Benchmark(Baseline = true)]
    public int HeroCsv_ReadAll()
    {
        var records = global::HeroCsv.Csv.ReadAllRecords(_csvData);
        return records.Count;
    }
    
    [Benchmark]
    public int HeroCsv_Stream()
    {
        using var stream = new MemoryStream(_csvBytes);
        using var reader = global::HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }
    
    [Benchmark]
    public int HeroCsv_CountOnly()
    {
        return global::HeroCsv.Csv.CountRecords(_csvData);
    }
    
    [Benchmark]
    public int CsvHelper()
    {
        using var reader = new StringReader(_csvData);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        return records.Count;
    }
    
    [Benchmark]
    public int Sep_Library()
    {
        using var reader = Sep.Reader().FromText(_csvData);
        var count = 0;
        foreach (var row in reader)
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int Sylvan_Library()
    {
        using var reader = new StringReader(_csvData);
        using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);
        var count = 0;
        while (csv.Read())
        {
            count++;
        }
        return count;
    }
    
    [Benchmark]
    public int ServiceStack_Library()
    {
        var records = ServiceStack.Text.CsvSerializer.DeserializeFromString<List<Dictionary<string, string>>>(_csvData);
        return records.Count;
    }
}