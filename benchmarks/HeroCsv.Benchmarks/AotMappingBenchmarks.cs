using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using CsvHelper;
using HeroCsv;
using HeroCsv.Core;
using HeroCsv.Mapping;
using System.Globalization;
using System.Text;

namespace HeroCsv.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class AotMappingBenchmarks
{
    private string csvData = "";
    private byte[] csvBytes = [];
    private const int RowCount = 1000;

    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,FirstName,LastName,Department,Salary,HireDate,IsActive");

        for (int i = 1; i <= RowCount; i++)
        {
            sb.AppendLine($"{i},John{i},Doe{i},Engineering,{75000 + i * 100},2020-01-{(i % 28) + 1:D2},true");
        }

        csvData = sb.ToString();
        csvBytes = Encoding.UTF8.GetBytes(csvData);
    }

    // ========== HeroCsv Benchmarks ==========

    [Benchmark(Description = "HeroCsv - Reflection Mapping")]
    public List<Employee> HeroCsv_ReflectionMapping()
    {
        return [.. Csv.Read<Employee>(csvData)];
    }

    [Benchmark(Description = "HeroCsv - Factory Mapping (AOT)")]
    public List<Employee> HeroCsv_FactoryMapping()
    {
        return [.. Csv.ReadWithHeaders(csvData, (headers, record) =>
        {
            var idIdx = Array.IndexOf(headers, "Id");
            var fnIdx = Array.IndexOf(headers, "FirstName");
            var lnIdx = Array.IndexOf(headers, "LastName");
            var deptIdx = Array.IndexOf(headers, "Department");
            var salIdx = Array.IndexOf(headers, "Salary");
            var hireIdx = Array.IndexOf(headers, "HireDate");
            var activeIdx = Array.IndexOf(headers, "IsActive");

            return new Employee
            {
                Id = record.GetInt32(idIdx),
                FirstName = record.GetString(fnIdx),
                LastName = record.GetString(lnIdx),
                Department = record.GetString(deptIdx),
                Salary = record.GetDecimal(salIdx),
                HireDate = record.GetDateTime(hireIdx),
                IsActive = record.GetBoolean(activeIdx)
            };
        })];
    }

    [Benchmark(Description = "HeroCsv - Optimized Factory (AOT)")]
    public List<Employee> HeroCsv_OptimizedFactory()
    {
        // Pre-calculate indices outside the loop
        return [.. Csv.Read(csvData, record => new Employee
        {
            Id = record.GetInt32(0),
            FirstName = record.GetString(1),
            LastName = record.GetString(2),
            Department = record.GetString(3),
            Salary = record.GetDecimal(4),
            HireDate = record.GetDateTime(5),
            IsActive = record.GetBoolean(6)
        })];
    }

    [Benchmark(Description = "HeroCsv - Manual Mapping")]
    public List<Employee> HeroCsv_ManualMapping()
    {
        var builder = new CsvMappingBuilder<Employee>();
        builder.Map(e => e.Id, 0);
        builder.Map(e => e.FirstName, 1);
        builder.Map(e => e.LastName, 2);
        builder.Map(e => e.Department, 3);
        builder.Map(e => e.Salary, 4);
        builder.Map(e => e.HireDate, 5);
        builder.Map(e => e.IsActive, 6);
        var mapping = builder.Build();

        return [.. Csv.Read(csvData, mapping)];
    }

    // Source-generated would be similar to OptimizedFactory but with compile-time generation
    // We can simulate it with the same approach
    [Benchmark(Description = "HeroCsv - Simulated SourceGen")]
    public List<Employee> HeroCsv_SimulatedSourceGen()
    {
        // This simulates what source generator would produce
        return [.. Csv.Read(csvData, CreateEmployeeFromRecord)];
    }

    // This method simulates what the source generator would create
    private static Employee CreateEmployeeFromRecord(ICsvRecord record)
    {
        return new Employee
        {
            Id = record.GetInt32(0),
            FirstName = record.GetString(1),
            LastName = record.GetString(2),
            Department = record.GetString(3),
            Salary = record.GetDecimal(4),
            HireDate = record.GetDateTime(5),
            IsActive = record.GetBoolean(6)
        };
    }

    // ========== Competitor Benchmarks ==========

    [Benchmark(Description = "CsvHelper - Auto Mapping")]
    public List<Employee> CsvHelper_AutoMapping()
    {
        using var reader = new StringReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return [.. csv.GetRecords<Employee>()];
    }

    // Sep benchmark removed - add Sep package if needed
    // [Benchmark(Description = "Sep - Auto Mapping")]
    // public List<Employee> Sep_AutoMapping()

    // TinyCsvParser benchmark removed - add TinyCsvParser package if needed
    // [Benchmark(Description = "TinyCsvParser")]
    // public List<Employee> TinyCsvParser_Mapping()

    // Sylvan benchmark removed - add Sylvan.Data.Csv package if needed
    // [Benchmark(Description = "Sylvan - Manual Mapping")]
    // public List<Employee> Sylvan_ManualMapping()
}

/// <summary>
/// Micro-benchmark to specifically measure mapping overhead
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class MappingOverheadBenchmarks
{
    private string simpleCsv = "";
    private const int Iterations = 10000;

    public class SimpleRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Value { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        simpleCsv = "1,Test,123.45";
    }

    [Benchmark(Baseline = true, Description = "Direct Property Set")]
    public SimpleRecord DirectPropertySet()
    {
        var parts = simpleCsv.Split(',');
        return new SimpleRecord
        {
            Id = int.Parse(parts[0]),
            Name = parts[1],
            Value = decimal.Parse(parts[2])
        };
    }

    [Benchmark(Description = "Reflection SetValue")]
    public SimpleRecord ReflectionSetValue()
    {
        var parts = simpleCsv.Split(',');
        var obj = new SimpleRecord();
        var type = typeof(SimpleRecord);

        type.GetProperty("Id")!.SetValue(obj, int.Parse(parts[0]));
        type.GetProperty("Name")!.SetValue(obj, parts[1]);
        type.GetProperty("Value")!.SetValue(obj, decimal.Parse(parts[2]));

        return obj;
    }

    [Benchmark(Description = "HeroCsv Factory")]
    public SimpleRecord HeroCsvFactory()
    {
        var record = Csv.Read(simpleCsv, rec => new SimpleRecord
        {
            Id = rec.GetInt32(0),
            Name = rec.GetString(1),
            Value = rec.GetDecimal(2)
        }).First();

        return record;
    }

    [Benchmark(Description = "HeroCsv Reflection")]
    public SimpleRecord HeroCsvReflection()
    {
        return Csv.Read<SimpleRecord>(simpleCsv).First();
    }
}