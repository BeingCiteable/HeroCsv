using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Spectre.Console;
using FastCsv; // For Csv static class
using FastCsv.Models; // For CsvOptions

namespace FastCsv.Benchmarks;

public class RealDataBenchmark
{
    private static readonly string TestDataDirectory;

    static RealDataBenchmark()
    {
        // Get path to TestData directory
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        TestDataDirectory = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "tests", "TestData");
        TestDataDirectory = Path.GetFullPath(TestDataDirectory);
    }

    public static void RunRealDataComparison()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]FastCsv Real Data Performance Analysis[/]").RuleStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();

        var resultSet = new BenchmarkResultSet
        {
            BenchmarkSuite = "FastCsv Real Data Performance",
            RuntimeVersion = Environment.Version.ToString()
        };

        var testFiles = new[]
        {
            ("simple.csv", "Simple 3-record file"),
            ("employees.csv", "Employee data (10 records)"),
            ("products.csv", "Product catalog with quotes"),
            ("mixed_data_types.csv", "Mixed data types"),
            ("medium_dataset.csv", "Medium dataset (1K records)"),
            ("large_dataset_10k.csv", "Large dataset (10K records)")
        };

        foreach (var (fileName, description) in testFiles)
        {
            var filePath = Path.Combine(TestDataDirectory, fileName);

            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️  Skipping {fileName} - file not found[/]");
                continue;
            }

            AnsiConsole.MarkupLine($"[cyan]📄 {description}[/]");
            AnsiConsole.MarkupLine($"[grey]File:[/] {fileName}");

            var fileInfo = new FileInfo(filePath);
            AnsiConsole.MarkupLine($"[grey]Size:[/] {FormatFileSize(fileInfo.Length)}");

            BenchmarkFile(filePath, fileName, description, resultSet);
            AnsiConsole.WriteLine();
        }

        // Special performance test with huge dataset if available
        var hugeFile = Path.Combine(TestDataDirectory, "huge_dataset.csv");
        if (File.Exists(hugeFile))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[red]🚀 EXTREME PERFORMANCE TEST[/]").RuleStyle(Style.Parse("red")));
            BenchmarkLargeFile(hugeFile, resultSet);
        }

        // Export results in all formats to consistent directory
        var outputDir = BenchmarkExporter.GetBenchmarkOutputDirectory("RealData");
        BenchmarkExporter.ExportAll(resultSet, outputDir);
    }

    private static void BenchmarkFile(string filePath, string fileName, string description, BenchmarkResultSet resultSet)
    {
        var options = new CsvOptions(hasHeader: true);
        int iterations = fileName.Contains("large") ? 5 : 20; // Fewer iterations for large files

        var fileInfo = new FileInfo(filePath);
        var fileSize = FormatFileSize(fileInfo.Length);
        var recordCount = 0;

        try
        {
            // Sync Read All Records
            var syncResult = BenchmarkActionWithResult(() =>
            {
                var content = File.ReadAllText(filePath);
                var records = global::FastCsv.Csv.ReadAllRecords(content, options);
                recordCount = records.Count;
                return recordCount;
            }, iterations, "Sync ReadAllRecords", description, "FastCsv", recordCount, fileSize);
            resultSet.Results.Add(syncResult);

            // Count Records Only
            var countResult = BenchmarkActionWithResult(() =>
            {
                var content = File.ReadAllText(filePath);
                return global::FastCsv.Csv.CountRecords(content, options);
            }, iterations, "Count Only", description, "FastCsv", recordCount, fileSize);
            resultSet.Results.Add(countResult);

            // Stream Reading
            var streamResult = BenchmarkActionWithResult(() =>
            {
                using var stream = File.OpenRead(filePath);
                using var reader = global::FastCsv.Csv.CreateReader(stream, options);
                return reader.CountRecords();
            }, iterations, "Stream Reading", description, "FastCsv", recordCount, fileSize);
            resultSet.Results.Add(streamResult);

#if NET7_0_OR_GREATER
            // Async File Reading
            var asyncResult = BenchmarkAsyncActionWithResult(async () =>
            {
                var records = await global::FastCsv.Csv.ReadFileAsync(filePath, options, null, CancellationToken.None);
                return records.Count;
            }, Math.Min(iterations, 10), "Async ReadFileAsync", description, "FastCsv", recordCount, fileSize);
            resultSet.Results.Add(asyncResult);

            // Async Stream Reading
            var asyncStreamResult = BenchmarkAsyncActionWithResult(async () =>
            {
                await using var stream = File.OpenRead(filePath);
                var records = await global::FastCsv.Csv.ReadStreamAsync(stream, options, null, false, CancellationToken.None);
                return records.Count;
            }, Math.Min(iterations, 10), "Async Stream", description, "FastCsv", recordCount, fileSize);
            resultSet.Results.Add(asyncStreamResult);
#endif

            // Performance comparison (console output)
            AnsiConsole.MarkupLine("[yellow]Performance Summary:[/]");
            AnsiConsole.MarkupLine($"  [green]🏃 Fastest:[/] {countResult.Method} ([cyan]{countResult.MeanTimeMs:F2} ms/op[/])");
            AnsiConsole.MarkupLine($"  [green]📊 Best for processing:[/] {(syncResult.MeanTimeMs < streamResult.MeanTimeMs ? syncResult.Method : streamResult.Method)} ([cyan]{Math.Min(syncResult.MeanTimeMs, streamResult.MeanTimeMs):F2} ms/op[/])");

#if NET7_0_OR_GREATER
            var asyncBenefit = (syncResult.MeanTimeMs > asyncResult.MeanTimeMs ? "YES" : "MINIMAL");
            var asyncColor = asyncBenefit == "YES" ? "green" : "yellow";
            AnsiConsole.MarkupLine($"  [green]⚡ Async benefit:[/] [{asyncColor}]{asyncBenefit}[/] ([cyan]{(syncResult.MeanTimeMs - asyncResult.MeanTimeMs) / syncResult.MeanTimeMs * 100:F0}% faster[/])");
#endif
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error benchmarking {fileName}: {ex.Message}[/]");
        }
    }

    private static void BenchmarkLargeFile(string filePath, BenchmarkResultSet resultSet)
    {
        var fileInfo = new FileInfo(filePath);
        AnsiConsole.MarkupLine($"[grey]File:[/] huge_dataset.csv ([cyan]{FormatFileSize(fileInfo.Length)}[/])");

        var options = new CsvOptions(hasHeader: true);

        // Ultra-fast count only
        var countTime = BenchmarkAction(() =>
        {
            var content = File.ReadAllText(filePath);
            return global::FastCsv.Csv.CountRecords(content, options);
        }, 3, "Count Records");

        // Memory-efficient stream reading
        var streamTime = BenchmarkAction(() =>
        {
            using var stream = File.OpenRead(filePath);
            using var reader = global::FastCsv.Csv.CreateReader(stream, options);
            var count = 0;
            while (reader.TryReadRecord(out _)) count++;
            return count;
        }, 3, "Stream Processing");

#if NET7_0_OR_GREATER
        // Async streaming (memory efficient)
        var asyncStreamTime = BenchmarkAsyncAction(async () =>
        {
            var count = 0;
            await foreach (var record in global::FastCsv.Csv.ReadFileAsyncEnumerable(filePath, options, null, CancellationToken.None))
            {
                count++;
            }
            return count;
        }, 2, "Async Streaming");

        AnsiConsole.MarkupLine($"[green]🎯 Best for huge files:[/] Async Streaming ([cyan]{asyncStreamTime:F2} ms/op[/])");
#endif

        AnsiConsole.MarkupLine($"[yellow]💡 Memory usage:[/] Stream/Async methods use constant memory");
        AnsiConsole.MarkupLine($"[yellow]📈 Throughput:[/] ~[cyan]{fileInfo.Length / 1024.0 / 1024.0 / Math.Min(countTime, streamTime) * 1000:F1} MB/sec[/]");
    }

    private static double BenchmarkAction(Func<int> action, int iterations, string name)
    {
        // Warmup
        action();

        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var count = action();
            totalCount += count;
        }

        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;

        AnsiConsole.MarkupLine($"  [grey]{name,-20}[/] : [green]{stopwatch.ElapsedMilliseconds:N0} ms[/] ([cyan]{msPerOp:F2} ms/op[/], [grey]{totalCount / iterations:N0} records[/])");

        return msPerOp;
    }

#if NET7_0_OR_GREATER
    private static double BenchmarkAsyncAction(Func<Task<int>> action, int iterations, string name)
    {
        // Warmup
        action().GetAwaiter().GetResult();

        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var count = action().GetAwaiter().GetResult();
            totalCount += count;
        }

        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;

        AnsiConsole.MarkupLine($"  [grey]{name,-20}[/] : [green]{stopwatch.ElapsedMilliseconds:N0} ms[/] ([cyan]{msPerOp:F2} ms/op[/], [grey]{totalCount / iterations:N0} records[/])");

        return msPerOp;
    }
#endif

    private static BenchmarkResult BenchmarkActionWithResult(Func<int> action, int iterations, string method, string testCase, string library, int rowCount, string fileSize)
    {
        // Warmup
        action();

        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var count = action();
            totalCount += count;
        }

        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;

        Console.WriteLine($"  {method,-20} : {stopwatch.ElapsedMilliseconds:N0} ms ({msPerOp:F2} ms/op, {totalCount / iterations:N0} records)");

        return new BenchmarkResult
        {
            BenchmarkName = "Real Data Performance",
            TestCase = testCase,
            Library = library,
            Method = method,
            RowCount = totalCount / iterations,
            Iterations = iterations,
            MeanTimeMs = msPerOp,
            StdDevMs = 0, // We're not calculating standard deviation here
            AllocatedBytes = 0, // Not tracking allocations in this benchmark
            FileSize = fileSize,
            Environment = $".NET {Environment.Version}"
        };
    }

#if NET7_0_OR_GREATER
    private static BenchmarkResult BenchmarkAsyncActionWithResult(Func<Task<int>> action, int iterations, string method, string testCase, string library, int rowCount, string fileSize)
    {
        // Warmup
        action().GetAwaiter().GetResult();

        var stopwatch = Stopwatch.StartNew();
        int totalCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var count = action().GetAwaiter().GetResult();
            totalCount += count;
        }

        stopwatch.Stop();
        double msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;

        Console.WriteLine($"  {method,-20} : {stopwatch.ElapsedMilliseconds:N0} ms ({msPerOp:F2} ms/op, {totalCount / iterations:N0} records)");

        return new BenchmarkResult
        {
            BenchmarkName = "Real Data Performance",
            TestCase = testCase,
            Library = library,
            Method = method,
            RowCount = totalCount / iterations,
            Iterations = iterations,
            MeanTimeMs = msPerOp,
            StdDevMs = 0,
            AllocatedBytes = 0,
            FileSize = fileSize,
            Environment = $".NET {Environment.Version}"
        };
    }
#endif

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}