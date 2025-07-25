using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace FastCsv.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("FastCsv Benchmark Suite");
            Console.WriteLine("=======================");
            Console.WriteLine();
            Console.WriteLine("Available benchmark suites:");
            Console.WriteLine("  quick         - Quick performance comparison (no complex setup)");
            Console.WriteLine("  simple        - Simplified comparison with major libraries (recommended)");
            Console.WriteLine("  direct        - Direct comparison between FastCsv and Sep");
            Console.WriteLine("  original      - Original FastCsv internal benchmarks");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <suite-name>");
            Console.WriteLine("Example: dotnet run -- quick");
            return;
        }

        var suite = args[0].ToLowerInvariant();
        
        switch (suite)
        {
            case "quick":
                Console.WriteLine("Running Quick Performance Comparison...");
                QuickBenchmark.RunComparison();
                break;
                
            case "simple":
                Console.WriteLine("Running Simplified CSV Library Comparison...");
                BenchmarkRunner.Run<SimplifiedComparison>();
                break;
                                
            case "direct":
                Console.WriteLine("Running Direct FastCsv vs Sep Comparison...");
                BenchmarkRunner.Run<DirectComparison>();
                break;
                
            case "original":
                Console.WriteLine("Running Original FastCsv Benchmarks...");
                BenchmarkRunner.Run<CsvParsingBenchmarks>();
                break;
                
            default:
                Console.WriteLine($"Unknown benchmark suite: {suite}");
                Console.WriteLine("Use 'dotnet run' without arguments to see available options.");
                break;
        }
    }
}