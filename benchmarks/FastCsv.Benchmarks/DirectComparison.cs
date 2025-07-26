using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text;
using nietras.SeparatedValues;
using FastCsv; // For Csv static class
using FastCsv.Core; // For FastCsvReader

namespace FastCsv.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 10)]
[MemoryDiagnoser]
public class DirectComparison
{
    private string _csvData = null!;
    private byte[] _csvBytes = null!;

    [Params(100, 1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Age,City,Country,Email,Phone");

        for (int i = 0; i < RowCount; i++)
        {
            sb.AppendLine($"Person{i},25,City{i},Country{i},email{i}@example.com,555-{i:D4}");
        }

        _csvData = sb.ToString();
        _csvBytes = Encoding.UTF8.GetBytes(_csvData);
    }

    [Benchmark(Baseline = true)]
    public int Sep_ReadAll()
    {
        using var reader = Sep.Reader().FromText(_csvData);
        var count = 0;

        foreach (var row in reader)
        {
            count++;
            // Access all fields to ensure parsing
            for (int i = 0; i < row.ColCount; i++)
            {
                _ = row[i].ToString();
            }
        }

        return count;
    }

    [Benchmark]
    public int FastCsv_ReadAll()
    {
        using var reader = Csv.CreateReader(_csvData);
        var records = reader.ReadAllRecords();
        var count = 0;

        foreach (var record in records)
        {
            count++;
            // Fields already parsed as strings
            for (int i = 0; i < record.Length; i++)
            {
                _ = record[i];
            }
        }

        return count;
    }

    [Benchmark]
    public int FastCsv_DirectRows()
    {
        using var reader = Csv.CreateReader(_csvData);
        var fastReader = (FastCsvReader)reader;
        var count = 0;

        foreach (var row in fastReader.EnumerateRows())
        {
            count++;
            // Access fields on-demand like Sep
            for (int i = 0; i < 6; i++)
            {
                _ = row.GetString(i);
            }
        }

        return count;
    }

    [Benchmark]
    public int Sep_CountOnly()
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
    public int FastCsv_CountOnly()
    {
        using var reader = Csv.CreateReader(_csvData);
        return reader.CountRecords();
    }
}