using System;
using System.Diagnostics;
using System.Text;
using nietras.SeparatedValues;

// Simple comparison script for FastCsv vs Sep performance
var sb = new StringBuilder();
sb.AppendLine("Name,Age,City,Country,Email,Phone");

// Generate test data with varying row counts
var rowCounts = new[] { 100, 1000, 10000 };

foreach (var rowCount in rowCounts)
{
    sb.Clear();
    sb.AppendLine("Name,Age,City,Country,Email,Phone");
    
    for (int i = 0; i < rowCount; i++)
    {
        sb.AppendLine($"Person{i},25,City{i},Country{i},email{i}@example.com,555-{i:D4}");
    }
    
    var csvData = sb.ToString();
    const int iterations = 100;
    
    Console.WriteLine($"\n🔍 Performance Comparison - {rowCount} rows, {iterations} iterations:");
    Console.WriteLine(new string('=', 60));
    
    // Sep - Reading all records with string allocation
    var sepTime = MeasurePerformance(() =>
    {
        using var reader = Sep.Reader().FromText(csvData);
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
    }, iterations, "Sep (ReadAll)");
    
    // FastCsv - Reading all records with string allocation
    var fastCsvTime = MeasurePerformance(() =>
    {
        using var reader = FastCsv.Csv.CreateReader(csvData);
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
    }, iterations, "FastCsv (ReadAll)");
    
    // FastCsv - Direct row enumeration (optimized)
    var fastCsvDirectTime = MeasurePerformance(() =>
    {
        using var reader = FastCsv.Csv.CreateReader(csvData);
        var fastReader = (FastCsv.FastCsvReader)reader;
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
    }, iterations, "FastCsv (DirectRows)");
    
    // Sep - Count only
    var sepCountTime = MeasurePerformance(() =>
    {
        using var reader = Sep.Reader().FromText(csvData);
        var count = 0;
        
        foreach (var row in reader)
        {
            count++;
        }
        
        return count;
    }, iterations, "Sep (CountOnly)");
    
    // FastCsv - Count only
    var fastCsvCountTime = MeasurePerformance(() =>
    {
        using var reader = FastCsv.Csv.CreateReader(csvData);
        return reader.CountRecords();
    }, iterations, "FastCsv (CountOnly)");
    
    // Performance Gap Analysis
    Console.WriteLine($"\n📊 Performance Gap Analysis:");
    Console.WriteLine($"   ReadAll: FastCsv is {fastCsvTime/sepTime:F1}x {(fastCsvTime > sepTime ? "slower" : "faster")} than Sep");
    Console.WriteLine($"   DirectRows: FastCsv is {fastCsvDirectTime/sepTime:F1}x {(fastCsvDirectTime > sepTime ? "slower" : "faster")} than Sep");
    Console.WriteLine($"   CountOnly: FastCsv is {fastCsvCountTime/sepCountTime:F1}x {(fastCsvCountTime > sepCountTime ? "slower" : "faster")} than Sep");
}

double MeasurePerformance(Func<int> action, int iterations, string name)
{
    // Warmup
    for (int i = 0; i < 5; i++)
    {
        action();
    }
    
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        action();
    }
    sw.Stop();
    
    var msPerOp = sw.Elapsed.TotalMilliseconds / iterations;
    Console.WriteLine($"{name,-20}: {msPerOp:F2} ms/op");
    return msPerOp;
}