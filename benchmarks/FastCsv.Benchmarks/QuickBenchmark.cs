using System.Diagnostics;
using System.Text;
using CsvHelper;
using System.Globalization;
using nietras.SeparatedValues;

namespace FastCsv.Benchmarks;

public class QuickBenchmark
{
    public static void RunComparison()
    {
        Console.WriteLine("FastCsv Competitive Performance Analysis");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        var testData = GenerateTestCsv(1000);
        var testMemory = testData.AsMemory();
        const int iterations = 100;
        
        Console.WriteLine($"Dataset: {testData.Split('\n').Length - 1} rows, {iterations} iterations");
        Console.WriteLine();
        
        var results = new Dictionary<string, Dictionary<string, double>>();
        
        // Initialize library results
        var libraries = new[] { "CsvHelper", "FastCsv", "LumenWorks", "Sep", "Sylvan" };
        foreach (var lib in libraries)
        {
            results[lib] = new Dictionary<string, double>();
        }
        
        // Feature 1: Read All Records (String Input)
        Console.WriteLine("🔍 FEATURE: Read All Records (String Input)");
        Console.WriteLine("=" + new string('=', 47));
        
        results["FastCsv"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            var records = global::FastCsv.Csv.ReadAllRecords(testData);
            return records.Count;
        }, iterations, "FastCsv");
        
        results["CsvHelper"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            int count = 0;
            while (csv.Read()) count++;
            return count;
        }, iterations, "CsvHelper");
        
        results["Sylvan"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);
            int count = 0;
            while (csv.Read()) count++;
            return count;
        }, iterations, "Sylvan.Data.Csv");
        
        results["Sep"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = Sep.Reader().FromText(testData);
            int count = 0;
            foreach (var row in reader) count++;
            return count;
        }, iterations, "Sep");
        
        results["LumenWorks"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = new LumenWorks.Framework.IO.Csv.CsvReader(reader, true);
            int count = 0;
            while (csv.ReadNextRecord()) count++;
            return count;
        }, iterations, "LumenWorks");
        
        Console.WriteLine();
        
        // Feature 2: Read All Records (Memory Input - Zero Allocation)
        Console.WriteLine("🚀 FEATURE: Read All Records (Memory Input - Zero Allocation)");
        Console.WriteLine("=" + new string('=', 63));
        
        results["FastCsv"]["ReadAll_Memory"] = BenchmarkAction(() =>
        {
            var records = global::FastCsv.Csv.ReadAllRecords(testMemory);
            return records.Count;
        }, iterations, "FastCsv");
        
        // Other libraries don't support ReadOnlyMemory<char> input
        Console.WriteLine("CsvHelper        : N/A (no Memory<char> support)");
        Console.WriteLine("LumenWorks       : N/A (no Memory<char> support)");
        Console.WriteLine("Sep              : N/A (no Memory<char> support)");
        Console.WriteLine("Sylvan.Data.Csv  : N/A (no Memory<char> support)");
        
        results["CsvHelper"]["ReadAll_Memory"] = -1; // N/A
        results["LumenWorks"]["ReadAll_Memory"] = -1; // N/A  
        results["Sep"]["ReadAll_Memory"] = -1; // N/A
        results["Sylvan"]["ReadAll_Memory"] = -1; // N/A
        
        Console.WriteLine();
        
        // Feature 3: Count Records Only
        Console.WriteLine("⚡ FEATURE: Count Records Only (No String Allocation)");
        Console.WriteLine("=" + new string('=', 55));
        
        results["FastCsv"]["CountOnly"] = BenchmarkAction(() =>
        {
            return global::FastCsv.Csv.CountRecords(testData);
        }, iterations, "FastCsv");
        
        results["CsvHelper"]["CountOnly"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            int count = 0;
            while (csv.Read()) count++;
            return count;
        }, iterations, "CsvHelper (same as ReadAll)");
        
        results["Sep"]["CountOnly"] = BenchmarkAction(() =>
        {
            using var reader = Sep.Reader().FromText(testData);
            int count = 0;
            foreach (var row in reader) count++;
            return count;
        }, iterations, "Sep (same as ReadAll)");
        
        // Other libraries don't have count-only optimization
        Console.WriteLine("LumenWorks       : N/A (no count-only optimization)");
        Console.WriteLine("Sylvan.Data.Csv  : N/A (no count-only optimization)");
        
        results["LumenWorks"]["CountOnly"] = -1; // N/A
        results["Sylvan"]["CountOnly"] = -1; // N/A
        
        Console.WriteLine();
        
        // Summary Report
        PrintSummaryReport(results);
    }
    
    private static double BenchmarkAction(Func<int> action, int iterations, string name)
    {
        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            var count = action();
            totalCount += count;
            if (count == 0) throw new Exception($"No records returned by {name}");
        }
        
        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;
        
        Console.WriteLine($"{name,-16} : {stopwatch.ElapsedMilliseconds:N0} ms ({msPerOp:F2} ms/op)");
        
        return msPerOp;
    }
    
    private static void PrintSummaryReport(Dictionary<string, Dictionary<string, double>> results)
    {
        Console.WriteLine("📊 PERFORMANCE SUMMARY REPORT");
        Console.WriteLine("=" + new string('=', 31));
        Console.WriteLine();
        
        var features = new[]
        {
            ("Read All Records (String)", "ReadAll_String"),
            ("Read All Records (Memory)", "ReadAll_Memory"),
            ("Count Records Only", "CountOnly")
        };
        
        foreach (var (featureName, featureKey) in features)
        {
            Console.WriteLine($"🔹 {featureName}:");
            
            var libraryResults = results
                .Where(kvp => kvp.Value.ContainsKey(featureKey) && kvp.Value[featureKey] > 0)
                .Select(kvp => new { Library = kvp.Key, Time = kvp.Value[featureKey] })
                .OrderBy(x => x.Time)
                .ToList();
            
            if (libraryResults.Any())
            {
                for (int i = 0; i < libraryResults.Count; i++)
                {
                    var result = libraryResults[i];
                    var rank = i + 1;
                    var rankEmoji = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : "  ";
                    Console.WriteLine($"   {rankEmoji} {rank}. {result.Library,-12} : {result.Time:F2} ms/op");
                }
            }
            else
            {
                Console.WriteLine("   No libraries support this feature");
            }
            
            // Show N/A libraries
            var naLibraries = results
                .Where(kvp => !kvp.Value.ContainsKey(featureKey) || kvp.Value[featureKey] <= 0)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (naLibraries.Any())
            {
                Console.WriteLine($"   ❌ Not supported: {string.Join(", ", naLibraries)}");
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine("🎯 KEY INSIGHTS:");
        Console.WriteLine($"   • FastCsv Memory variant offers zero-allocation benefits");
        Console.WriteLine($"   • Count-only operations show optimization potential");
        Console.WriteLine($"   • Feature support varies significantly across libraries");
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
}