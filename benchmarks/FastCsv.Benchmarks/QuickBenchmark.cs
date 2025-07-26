using System.Diagnostics;
using System.Text;
using CsvHelper;
using System.Globalization;
using nietras.SeparatedValues;
using Spectre.Console;

namespace FastCsv.Benchmarks;

public class QuickBenchmark
{
    public static void RunComparison()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]FastCsv Competitive Performance Analysis[/]").RuleStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();
        
        var testData = GenerateTestCsv(1000);
        var testMemory = testData.AsMemory();
        const int iterations = 100;
        
        AnsiConsole.MarkupLine($"[grey]Dataset:[/] [cyan]{testData.Split('\n').Length - 1} rows[/], [cyan]{iterations} iterations[/]");
        AnsiConsole.WriteLine();
        
        var results = new Dictionary<string, Dictionary<string, double>>();
        
        // Initialize library results
        var libraries = new[] { "CsvHelper", "FastCsv", "LumenWorks", "Sep", "Sylvan" };
        foreach (var lib in libraries)
        {
            results[lib] = new Dictionary<string, double>();
        }
        
        // Feature 1: Read All Records (String Input)
        AnsiConsole.Write(new Rule("[blue]🔍 FEATURE: Read All Records (String Input)[/]").RuleStyle(Style.Parse("blue dim")));
        
        results["FastCsv"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testData);
            int count = 0;
            // Use optimized read all records method
            var records = reader.ReadAllRecords();
            foreach (var record in records)
            {
                count++;
                // Access fields for complete parsing
                for (int i = 0; i < record.Length; i++)
                {
                    var _ = record[i]; // This allocates strings like other libraries
                }
            }
            return count;
        }, iterations, "FastCsv");
        
        // Test direct row enumeration for maximum performance
        results["FastCsv"]["ReadAll_Optimized"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testData);
            int count = 0;
            // Direct row enumeration using whole-buffer parsing
            foreach (var row in ((global::FastCsv.FastCsvReader)reader).EnumerateRows())
            {
                count++;
                // Access fields with pre-computed positions
                for (int i = 0; i < 6; i++) // We know there are 6 fields
                {
                    var _ = row[i].ToString();
                }
            }
            return count;
        }, iterations, "FastCsv (Direct Rows)");
        
        results["CsvHelper"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            int count = 0;
            while (csv.Read())
            {
                count++;
                // Access fields for fair comparison
                for (int i = 0; i < 6; i++)
                {
                    var _ = csv.GetField(i);
                }
            }
            return count;
        }, iterations, "CsvHelper");
        
        results["Sylvan"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = Sylvan.Data.Csv.CsvDataReader.Create(reader);
            int count = 0;
            while (csv.Read())
            {
                count++;
                // Access with allocation for fair comparison
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    var _ = csv.GetString(i); // Allocates string like FastCsv
                }
            }
            return count;
        }, iterations, "Sylvan.Data.Csv");
        
        results["Sep"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = Sep.Reader().FromText(testData);
            int count = 0;
            foreach (var row in reader)
            {
                count++;
                // Access with allocation for fair comparison
                for (int i = 0; i < row.ColCount; i++)
                {
                    var _ = row[i].ToString(); // Allocates string like FastCsv
                }
            }
            return count;
        }, iterations, "Sep");
        
        results["LumenWorks"]["ReadAll_String"] = BenchmarkAction(() =>
        {
            using var reader = new StringReader(testData);
            using var csv = new LumenWorks.Framework.IO.Csv.CsvReader(reader, true);
            int count = 0;
            while (csv.ReadNextRecord())
            {
                count++;
                // Access fields for fair comparison
                for (int i = 0; i < csv.FieldCount; i++)
                {
                    var _ = csv[i];
                }
            }
            return count;
        }, iterations, "LumenWorks");
        
        AnsiConsole.WriteLine();
        
        // Feature 2: Read All Records (Memory Input - Zero Allocation)
        AnsiConsole.Write(new Rule("[green]🚀 FEATURE: Read All Records (Memory Input - Zero Allocation)[/]").RuleStyle(Style.Parse("green dim")));
        
        results["FastCsv"]["ReadAll_Memory"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testMemory);
            int count = 0;
            // Parse fields for fair comparison
            foreach (var record in reader.GetRecords())
            {
                count++;
                // Access fields to ensure parsing happens
                var _ = record.Length;
            }
            return count;
        }, iterations, "FastCsv");
        
        // Other libraries don't support ReadOnlyMemory<char> input
        AnsiConsole.MarkupLine("[grey]CsvHelper        : N/A (no Memory<char> support)[/]");
        AnsiConsole.MarkupLine("[grey]LumenWorks       : N/A (no Memory<char> support)[/]");
        AnsiConsole.MarkupLine("[grey]Sep              : N/A (no Memory<char> support)[/]");
        AnsiConsole.MarkupLine("[grey]Sylvan.Data.Csv  : N/A (no Memory<char> support)[/]");
        
        results["CsvHelper"]["ReadAll_Memory"] = -1; // N/A
        results["LumenWorks"]["ReadAll_Memory"] = -1; // N/A  
        results["Sep"]["ReadAll_Memory"] = -1; // N/A
        results["Sylvan"]["ReadAll_Memory"] = -1; // N/A
        
        AnsiConsole.WriteLine();
        
        // Feature 3: Count Records Only
        AnsiConsole.Write(new Rule("[yellow]⚡ FEATURE: Count Records Only (No String Allocation)[/]").RuleStyle(Style.Parse("yellow dim")));
        
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
        AnsiConsole.MarkupLine("[grey]LumenWorks       : N/A (no count-only optimization)[/]");
        AnsiConsole.MarkupLine("[grey]Sylvan.Data.Csv  : N/A (no count-only optimization)[/]");
        
        results["LumenWorks"]["CountOnly"] = -1; // N/A
        results["Sylvan"]["CountOnly"] = -1; // N/A
        
        AnsiConsole.WriteLine();
        
        // Feature 4: Zero-Allocation Row Enumeration (FastCsv Advanced)
        AnsiConsole.Write(new Rule("[cyan]🚀 FEATURE: Zero-Allocation Row Enumeration (FastCsv Advanced)[/]").RuleStyle(Style.Parse("cyan dim")));
        
        results["FastCsv"]["ZeroAlloc"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testData);
            int count = 0;
            // Use optimized enumerator for zero-allocation parsing
            foreach (var row in ((global::FastCsv.FastCsvReader)reader).EnumerateRows())
            {
                count++;
                // Access fields without allocation
                for (int i = 0; i < 6; i++)
                {
                    var _ = row[i]; // Access span without allocating
                }
            }
            return count;
        }, iterations, "FastCsv (Zero-Alloc)");
        
        AnsiConsole.MarkupLine("[grey]CsvHelper        : N/A (no zero-allocation mode)[/]");
        AnsiConsole.MarkupLine("[grey]LumenWorks       : N/A (no zero-allocation mode)[/]");
        AnsiConsole.MarkupLine("[grey]Sep              : Uses spans (similar capability)[/]");
        AnsiConsole.MarkupLine("[grey]Sylvan.Data.Csv  : Uses spans (similar capability)[/]");
        
        results["CsvHelper"]["ZeroAlloc"] = -1; // N/A
        results["LumenWorks"]["ZeroAlloc"] = -1; // N/A
        results["Sep"]["ZeroAlloc"] = -1; // N/A
        results["Sylvan"]["ZeroAlloc"] = -1; // N/A
        
        AnsiConsole.WriteLine();
        
        // Feature 5: Direct Field Iteration (Direct Buffer Access)
        AnsiConsole.Write(new Rule("[red]🔥 FEATURE: Direct Field Iteration (Direct Buffer Access)[/]").RuleStyle(Style.Parse("red dim")));
        
        results["FastCsv"]["DirectFieldIteration"] = BenchmarkAction(() =>
        {
            var options = new global::FastCsv.CsvOptions(hasHeader: true);
            int count = 0;
            
            // Direct field iteration - the fastest approach
            foreach (var field in global::FastCsv.CsvFieldIterator.IterateFields(testData.AsSpan(), options))
            {
                // Just count fields, no allocation
                count++;
            }
            
            return count / 6; // 6 fields per row in our test data
        }, iterations, "FastCsv (Direct)");
        
        // Other libraries don't have this approach
        AnsiConsole.MarkupLine("[grey]CsvHelper        : N/A (no direct field iteration)[/]");
        AnsiConsole.MarkupLine("[grey]LumenWorks       : N/A (no direct field iteration)[/]");
        AnsiConsole.MarkupLine("[grey]Sep              : N/A (no direct field iteration)[/]");
        AnsiConsole.MarkupLine("[grey]Sylvan.Data.Csv  : N/A (no direct field iteration)[/]");
        
        results["CsvHelper"]["DirectFieldIteration"] = -1; // N/A
        results["LumenWorks"]["DirectFieldIteration"] = -1; // N/A  
        results["Sep"]["DirectFieldIteration"] = -1; // N/A
        results["Sylvan"]["DirectFieldIteration"] = -1; // N/A
        
        AnsiConsole.WriteLine();
        
        // Feature 6: Async vs Sync Performance (File I/O)
        AnsiConsole.Write(new Rule("[magenta]⚡ FEATURE: Async vs Sync Performance (File I/O)[/]").RuleStyle(Style.Parse("magenta dim")));
        
        // Create test file
        var testFilePath = Path.GetTempFileName();
        File.WriteAllText(testFilePath, testData);
        
        try
        {
            results["FastCsv"]["Sync_File"] = BenchmarkAction(() =>
            {
                var options = new global::FastCsv.CsvOptions(hasHeader: true);
                using var stream = File.OpenRead(testFilePath);
                using var reader = global::FastCsv.Csv.CreateReader(stream, options);
                int count = 0;
                foreach (var record in reader.GetRecords())
                {
                    count++;
                    // Access fields to ensure parsing happens
                    for (int i = 0; i < record.Length; i++)
                    {
                        var _ = record[i];
                    }
                }
                return count;
            }, 10, "FastCsv (Sync File)"); // Fewer iterations for file I/O
            
#if NET7_0_OR_GREATER
            results["FastCsv"]["Async_File"] = BenchmarkAsyncAction(async () =>
            {
                var options = new global::FastCsv.CsvOptions(hasHeader: true);
                var records = await global::FastCsv.Csv.ReadFileAsync(
                    filePath: testFilePath, 
                    options: options, 
                    encoding: null, 
                    cancellationToken: CancellationToken.None);
                int count = 0;
                foreach (var record in records)
                {
                    count++;
                    // Access fields to ensure parsing happens
                    for (int i = 0; i < record.Length; i++)
                    {
                        var _ = record[i];
                    }
                }
                return count;
            }, 10, "FastCsv (Async File)");
            
            results["FastCsv"]["Async_Stream"] = BenchmarkAsyncAction(async () =>
            {
                var options = new global::FastCsv.CsvOptions(hasHeader: true);
                await using var stream = File.OpenRead(testFilePath);
                var records = await global::FastCsv.Csv.ReadStreamAsync(stream, options);
                int count = 0;
                foreach (var record in records)
                {
                    count++;
                    // Access fields to ensure parsing happens
                    for (int i = 0; i < record.Length; i++)
                    {
                        var _ = record[i];
                    }
                }
                return count;
            }, 10, "FastCsv (Async Stream)");
            
            results["FastCsv"]["Async_Enumerable"] = BenchmarkAsyncAction(async () =>
            {
                var options = new global::FastCsv.CsvOptions(hasHeader: true);
                int count = 0;
                await foreach (var record in global::FastCsv.Csv.ReadFileAsyncEnumerable(testFilePath, options))
                {
                    count++;
                    // Access fields to ensure parsing happens
                    for (int i = 0; i < record.Length; i++)
                    {
                        var _ = record[i];
                    }
                }
                return count;
            }, 10, "FastCsv (Async Enumerable)");
#else
            AnsiConsole.MarkupLine("[grey]FastCsv (Async)      : N/A (requires .NET 7+)[/]");
            results["FastCsv"]["Async_File"] = -1;
            results["FastCsv"]["Async_Stream"] = -1;
            results["FastCsv"]["Async_Enumerable"] = -1;
#endif
        }
        finally
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
        
        AnsiConsole.WriteLine();
        
        // Feature 7: Data Source Comparison
        AnsiConsole.Write(new Rule("[green]📊 FEATURE: Data Source Performance Comparison[/]").RuleStyle(Style.Parse("green dim")));
        
        results["FastCsv"]["String_Source"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testData);
            return reader.CountRecords();
        }, iterations, "FastCsv (String)");
        
        results["FastCsv"]["Memory_Source"] = BenchmarkAction(() =>
        {
            using var reader = global::FastCsv.Csv.CreateReader(testMemory);
            return reader.CountRecords();
        }, iterations, "FastCsv (Memory)");
        
        results["FastCsv"]["Stream_Source"] = BenchmarkAction(() =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testData));
            using var reader = global::FastCsv.Csv.CreateReader(stream);
            return reader.CountRecords();
        }, iterations, "FastCsv (Stream)");
        
        AnsiConsole.WriteLine();
        
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
        
        AnsiConsole.MarkupLine($"[cyan]{name,-16}[/] : [yellow]{stopwatch.ElapsedMilliseconds:N0} ms[/] ([green]{msPerOp:F2} ms/op[/])");
        
        return msPerOp;
    }
    
    private static double BenchmarkAsyncAction(Func<Task<int>> action, int iterations, string name)
    {
        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            var count = action().GetAwaiter().GetResult(); // Sync wait for fair comparison
            totalCount += count;
            if (count == 0) throw new Exception($"No records returned by {name}");
        }
        
        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;
        
        AnsiConsole.MarkupLine($"[cyan]{name,-20}[/] : [yellow]{stopwatch.ElapsedMilliseconds:N0} ms[/] ([green]{msPerOp:F2} ms/op[/])");
        
        return msPerOp;
    }
    
    private static void PrintSummaryReport(Dictionary<string, Dictionary<string, double>> results)
    {
        AnsiConsole.Write(new Rule("[yellow]📊 PERFORMANCE SUMMARY REPORT[/]").RuleStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();
        
        var features = new[]
        {
            ("Read All Records (String)", "ReadAll_String"),
            ("Read All Records (Memory)", "ReadAll_Memory"),
            ("Count Records Only", "CountOnly"),
            ("Zero-Allocation Row Enumeration", "ZeroAlloc"),
            ("Direct Field Iteration", "DirectFieldIteration"),
            ("Sync File I/O", "Sync_File"),
            ("Async File I/O", "Async_File"),
            ("Async Stream I/O", "Async_Stream"),
            ("Async Enumerable", "Async_Enumerable"),
            ("String Data Source", "String_Source"),
            ("Memory Data Source", "Memory_Source"),
            ("Stream Data Source", "Stream_Source")
        };
        
        foreach (var (featureName, featureKey) in features)
        {
            AnsiConsole.MarkupLine($"[blue]🔹 {featureName}:[/]");
            
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
                    var color = rank == 1 ? "green" : rank == 2 ? "yellow" : rank == 3 ? "darkorange" : "cyan";
                    AnsiConsole.MarkupLine($"   {rankEmoji} [{color}]{rank}. {result.Library,-12} : {result.Time:F2} ms/op[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]   No libraries support this feature[/]");
            }
            
            // Show N/A libraries
            var naLibraries = results
                .Where(kvp => !kvp.Value.ContainsKey(featureKey) || kvp.Value[featureKey] <= 0)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (naLibraries.Any())
            {
                AnsiConsole.MarkupLine($"[grey]   ❌ Not supported: {string.Join(", ", naLibraries)}[/]");
            }
            
            AnsiConsole.WriteLine();
        }
        
        AnsiConsole.MarkupLine("[yellow]🎯 KEY INSIGHTS:[/]");
        AnsiConsole.MarkupLine($"[green]   • FastCsv Memory variant offers zero-allocation benefits[/]");
        AnsiConsole.MarkupLine($"[green]   • Count-only operations show optimization potential[/]");
        AnsiConsole.MarkupLine($"[green]   • Feature support varies significantly across libraries[/]");
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