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

/// <summary>
/// Benchmarks for CSV files with many columns and diverse data types
/// Tests performance with wide datasets (50, 100, 200+ columns)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
[SimpleJob(RuntimeMoniker.Net80)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class WideDatasetBenchmark
{
    private string _csvData50Cols = "";
    private string _csvData100Cols = "";
    private string _csvData200Cols = "";
    private string _csvMixedTypes = "";
    private byte[] _csvBytes50Cols = [];
    private byte[] _csvBytes100Cols = [];
    private byte[] _csvBytes200Cols = [];

    [Params(1000)] // Fixed row count to focus on column performance
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Generate CSV with 50 columns
        _csvData50Cols = GenerateWideCSV(RowCount, 50);
        _csvBytes50Cols = Encoding.UTF8.GetBytes(_csvData50Cols);

        // Generate CSV with 100 columns
        _csvData100Cols = GenerateWideCSV(RowCount, 100);
        _csvBytes100Cols = Encoding.UTF8.GetBytes(_csvData100Cols);

        // Generate CSV with 200 columns
        _csvData200Cols = GenerateWideCSV(RowCount, 200);
        _csvBytes200Cols = Encoding.UTF8.GetBytes(_csvData200Cols);

        // Generate CSV with mixed complex types
        _csvMixedTypes = GenerateMixedTypesCSV(RowCount);
    }

    private static string GenerateWideCSV(int rowCount, int columnCount)
    {
        var sb = new StringBuilder();
        var random = new Random(42); // Fixed seed

        // Generate headers
        for (int i = 0; i < columnCount; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"Column_{i:D3}");
        }
        sb.AppendLine();

        // Generate data rows
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                if (col > 0) sb.Append(',');

                // Mix different data types based on column index
                var value = (col % 10) switch
                {
                    0 => row.ToString(), // Integer
                    1 => $"Text_{row}_{col}", // String
                    2 => (random.NextDouble() * 1000).ToString("F2"), // Decimal
                    3 => random.Next(100) > 50 ? "true" : "false", // Boolean
                    4 => $"2024-{(row % 12) + 1:D2}-{(row % 28) + 1:D2}", // Date
                    5 => Guid.NewGuid().ToString(), // GUID
                    6 => $"user{row}@example.com", // Email
                    7 => $"+1-555-{random.Next(100, 999)}-{random.Next(1000, 9999)}", // Phone
                    8 => $"192.168.{row % 256}.{col % 256}", // IP Address
                    9 => random.Next(100) > 80 ? $"\"Value with, comma\"" : $"Normal_{row}", // Quoted values
                    _ => $"Data_{row}_{col}"
                };
                sb.Append(value);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GenerateMixedTypesCSV(int rowCount)
    {
        var sb = new StringBuilder();
        var random = new Random(42);

        // Complex headers with different naming conventions
        sb.AppendLine("ID,First Name,Last_Name,EmailAddress,Phone-Number,Date.Of.Birth,Salary$Amount,Is Active?,Department Code,Manager ID,Project|Name,Task#ID,Priority!Level,Completion%,Notes,Address Line 1,Address Line 2,City,State/Province,ZIP+4,Country,TimeZone,LastLogin,AccountCreated,Tags,Skills,Languages,CertificationDate,ExpirationDate,Score");

        // Generate complex data
        for (int i = 1; i <= rowCount; i++)
        {
            var hasSpecialChars = i % 10 == 0;
            var hasEmptyFields = i % 7 == 0;
            var hasQuotes = i % 5 == 0;

            sb.Append($"{i},");
            sb.Append(hasQuotes ? $"\"John{i}\"," : $"John{i},");
            sb.Append($"Doe{i},");
            sb.Append($"john.doe{i}@example.com,");
            sb.Append($"+1-555-{random.Next(100, 999)}-{random.Next(1000, 9999)},");
            sb.Append($"19{80 + (i % 40)}-{(i % 12) + 1:D2}-{(i % 28) + 1:D2},");
            sb.Append($"{50000 + i * 100}.50,");
            sb.Append(i % 2 == 0 ? "true," : "false,");
            sb.Append($"DEPT{i % 10:D3},");
            sb.Append(hasEmptyFields ? "," : $"{i / 10},");
            sb.Append(hasSpecialChars ? $"\"Project, {i}\"," : $"Project{i},");
            sb.Append($"TASK-{i:D5},");
            sb.Append($"{(i % 5) + 1},");
            sb.Append($"{(i * 10) % 101},");
            sb.Append(hasQuotes ? $"\"Note with \"\"quotes\"\" and, commas\"," : $"Note{i},");
            sb.Append($"{i} Main St,");
            sb.Append(hasEmptyFields ? "," : $"Suite {i},");
            sb.Append($"City{i % 100},");
            sb.Append($"ST{i % 50:D2},");
            sb.Append($"{10000 + i:D5}-{1000 + (i % 9000):D4},");
            sb.Append("USA,");
            sb.Append($"UTC{(i % 24) - 12:+00;-00},");
            sb.Append($"2024-01-{(i % 28) + 1:D2}T{(i % 24):D2}:{(i % 60):D2}:00Z,");
            sb.Append($"20{20 + (i % 5)}-{(i % 12) + 1:D2}-{(i % 28) + 1:D2},");
            sb.Append(hasSpecialChars ? "\"tag1,tag2,tag3\"," : "tag1;tag2;tag3,");
            sb.Append("C#;JavaScript;Python,");
            sb.Append("English;Spanish,");
            sb.Append(hasEmptyFields ? "," : $"20{23 + (i % 2)}-{(i % 12) + 1:D2}-{(i % 28) + 1:D2},");
            sb.Append($"20{25 + (i % 3)}-{(i % 12) + 1:D2}-{(i % 28) + 1:D2},");
            sb.Append($"{85 + (i % 15)}.{i % 100:D2}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // 50 Columns Benchmarks
    [BenchmarkCategory("50-Columns"), Benchmark(Baseline = true)]
    public int HeroCsv_50Cols()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData50Cols);
        return records.Count;
    }

    [BenchmarkCategory("50-Columns"), Benchmark]
    public int HeroCsv_Stream_50Cols()
    {
        using var stream = new MemoryStream(_csvBytes50Cols);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }

    [BenchmarkCategory("50-Columns"), Benchmark]
    public int CsvHelper_50Cols()
    {
        using var reader = new StringReader(_csvData50Cols);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        return records.Count;
    }

    [BenchmarkCategory("50-Columns"), Benchmark]
    public int Sep_50Cols()
    {
        using var reader = Sep.Reader().FromText(_csvData50Cols);
        var count = 0;
        foreach (var row in reader) count++;
        return count;
    }

    // 100 Columns Benchmarks
    [BenchmarkCategory("100-Columns"), Benchmark(Baseline = true)]
    public int HeroCsv_100Cols()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData100Cols);
        return records.Count;
    }

    [BenchmarkCategory("100-Columns"), Benchmark]
    public int HeroCsv_Stream_100Cols()
    {
        using var stream = new MemoryStream(_csvBytes100Cols);
        using var reader = HeroCsv.Csv.CreateReader(stream);
        var count = 0;
        while (reader.TryReadRecord(out _)) count++;
        return count;
    }

    [BenchmarkCategory("100-Columns"), Benchmark]
    public int CsvHelper_100Cols()
    {
        using var reader = new StringReader(_csvData100Cols);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        return records.Count;
    }

    [BenchmarkCategory("100-Columns"), Benchmark]
    public int Sep_100Cols()
    {
        using var reader = Sep.Reader().FromText(_csvData100Cols);
        var count = 0;
        foreach (var row in reader) count++;
        return count;
    }

    [BenchmarkCategory("100-Columns"), Benchmark]
    public int Sylvan_100Cols()
    {
        using var reader = new StringReader(_csvData100Cols);
        using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);
        var count = 0;
        while (csv.Read()) count++;
        return count;
    }

    // 200 Columns Benchmarks
    [BenchmarkCategory("200-Columns"), Benchmark(Baseline = true)]
    public int HeroCsv_200Cols()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvData200Cols);
        return records.Count;
    }

    [BenchmarkCategory("200-Columns"), Benchmark]
    public int HeroCsv_Count_200Cols()
    {
        return HeroCsv.Csv.CountRecords(_csvData200Cols);
    }

    [BenchmarkCategory("200-Columns"), Benchmark]
    public int Sep_200Cols()
    {
        using var reader = Sep.Reader().FromText(_csvData200Cols);
        var count = 0;
        foreach (var row in reader) count++;
        return count;
    }

    // Mixed Complex Types Benchmarks
    [BenchmarkCategory("Mixed-Types"), Benchmark(Baseline = true)]
    public int HeroCsv_MixedTypes()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvMixedTypes);
        return records.Count;
    }

    [BenchmarkCategory("Mixed-Types"), Benchmark]
    public int HeroCsv_FieldAccess_MixedTypes()
    {
        var records = HeroCsv.Csv.ReadAllRecords(_csvMixedTypes);
        var sum = 0;
        foreach (var record in records)
        {
            // Access different field types
            if (int.TryParse(record[0], out var id)) sum += id;
            var name = record[1];
            var email = record[3];
            if (bool.TryParse(record[7], out var isActive) && isActive) sum++;
            if (decimal.TryParse(record[6], out var salary)) sum += (int)(salary / 1000);
        }
        return sum;
    }

    [BenchmarkCategory("Mixed-Types"), Benchmark]
    public int CsvHelper_MixedTypes()
    {
        using var reader = new StringReader(_csvMixedTypes);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        return records.Count;
    }

    [BenchmarkCategory("Mixed-Types"), Benchmark]
    public int Sep_MixedTypes()
    {
        using var reader = Sep.Reader().FromText(_csvMixedTypes);
        var count = 0;
        foreach (var row in reader) count++;
        return count;
    }

    [BenchmarkCategory("Mixed-Types"), Benchmark]
    public int Sylvan_MixedTypes()
    {
        using var reader = new StringReader(_csvMixedTypes);
        using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);
        var count = 0;
        while (csv.Read()) count++;
        return count;
    }
}

/// <summary>
/// Benchmarks focused on field access performance with many columns
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class FieldAccessBenchmark
{
    private string _csvData = "";
    private IReadOnlyList<string[]> _records = new List<string[]>();

    [Params(50, 100, 200)]
    public int ColumnCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();

        // Generate headers
        for (int i = 0; i < ColumnCount; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"Col{i}");
        }
        sb.AppendLine();

        // Generate 1000 rows
        var random = new Random(42);
        for (int row = 0; row < 1000; row++)
        {
            for (int col = 0; col < ColumnCount; col++)
            {
                if (col > 0) sb.Append(',');
                sb.Append(random.Next(1000));
            }
            sb.AppendLine();
        }

        _csvData = sb.ToString();
        _records = HeroCsv.Csv.ReadAllRecords(_csvData);
    }

    [Benchmark(Baseline = true)]
    public int DirectArrayAccess()
    {
        var sum = 0;
        foreach (var record in _records)
        {
            // Access first, middle, and last columns
            if (int.TryParse(record[0], out var first)) sum += first;
            if (int.TryParse(record[ColumnCount / 2], out var middle)) sum += middle;
            if (int.TryParse(record[ColumnCount - 1], out var last)) sum += last;
        }
        return sum;
    }

    [Benchmark]
    public int AllFieldsAccess()
    {
        var sum = 0;
        foreach (var record in _records)
        {
            for (int i = 0; i < record.Length; i++)
            {
                if (int.TryParse(record[i], out var value))
                    sum += value;
            }
        }
        return sum;
    }

    [Benchmark]
    public int ExtensionMethodAccess()
    {
        var sum = 0;
        using var reader = HeroCsv.Csv.CreateReader(_csvData);

        while (reader.TryReadRecord(out var record))
        {
            if (record.TryGetInt32(0, out var val1)) sum += val1;
            if (record.TryGetInt32(ColumnCount / 2, out var val2)) sum += val2;
            if (record.TryGetInt32(ColumnCount - 1, out var val3)) sum += val3;
        }
        return sum;
    }

    [Benchmark]
    public int FieldIteratorAccess()
    {
        var sum = 0;
        var options = new HeroCsv.Models.CsvOptions();

        foreach (var field in HeroCsv.Parsing.CsvFieldIterator.IterateFields(_csvData, options))
        {
            if (field.FieldIndex == 0 ||
                field.FieldIndex == ColumnCount / 2 ||
                field.FieldIndex == ColumnCount - 1)
            {
                if (int.TryParse(field.Value, out var value))
                    sum += value;
            }
        }
        return sum;
    }
}