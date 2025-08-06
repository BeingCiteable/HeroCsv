using System.CommandLine;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HeroCsv.Benchmarks;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("HeroCsv Benchmark Suite - High-performance CSV parsing benchmarks");

        // Add global options
        var outputOption = new Option<DirectoryInfo?>("--output", "-o")
        {
            Description = "Output directory for benchmark results (default: auto-detected solution root)"
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose output"
        };

        rootCommand.Add(outputOption);
        rootCommand.Add(verboseOption);

        // Real data benchmark command
        var realDataCommand = new Command("realdata", "Run real CSV file performance benchmarks");

        var quickOption = new Option<bool>("--quick", "-q")
        {
            Description = "Run quick subset of tests (faster execution)"
        };

        var noExportOption = new Option<bool>("--no-export")
        {
            Description = "Skip exporting results to files"
        };

        realDataCommand.Add(quickOption);
        realDataCommand.Add(noExportOption);

        realDataCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var quick = parseResult.GetValue(quickOption);
            var noExport = parseResult.GetValue(noExportOption);
            RunRealDataBenchmark(output, verbose, quick, noExport);
            return 0;
        });

        // Library comparison commands
        var libraryCommand = new Command("library", "Compare HeroCsv with other CSV libraries");

        var simpleCompareCommand = new Command("simple", "Simplified comparison with major libraries");
        var rowsOption1 = new Option<int>("--rows", "-r")
        {
            Description = "Number of rows to generate for synthetic data",
            DefaultValueFactory = _ => 1000
        };
        var iterationsOption1 = new Option<int>("--iterations", "-i")
        {
            Description = "Number of benchmark iterations",
            DefaultValueFactory = _ => 10
        };

        simpleCompareCommand.Add(rowsOption1);
        simpleCompareCommand.Add(iterationsOption1);
        simpleCompareCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var rows = parseResult.GetValue(rowsOption1);
            var iterations = parseResult.GetValue(iterationsOption1);
            RunSimpleComparison(output, verbose, rows, iterations);
            return 0;
        });

        var directCompareCommand = new Command("direct", "Direct HeroCsv vs Sep comparison");
        var rowsOption2 = new Option<int>("--rows", "-r")
        {
            Description = "Number of rows to generate for synthetic data",
            DefaultValueFactory = _ => 1000
        };
        var iterationsOption2 = new Option<int>("--iterations", "-i")
        {
            Description = "Number of benchmark iterations",
            DefaultValueFactory = _ => 10
        };

        directCompareCommand.Add(rowsOption2);
        directCompareCommand.Add(iterationsOption2);
        directCompareCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var rows = parseResult.GetValue(rowsOption2);
            var iterations = parseResult.GetValue(iterationsOption2);
            RunDirectComparison(output, verbose, rows, iterations);
            return 0;
        });

        libraryCommand.Add(simpleCompareCommand);
        libraryCommand.Add(directCompareCommand);

        // Performance analysis commands
        var perfCommand = new Command("perf", "Performance analysis and profiling");

        var quickPerfCommand = new Command("quick", "Quick performance comparison");
        var rowsOption3 = new Option<int>("--rows", "-r")
        {
            Description = "Number of rows to generate for synthetic data",
            DefaultValueFactory = _ => 1000
        };
        var iterationsOption3 = new Option<int>("--iterations", "-i")
        {
            Description = "Number of benchmark iterations",
            DefaultValueFactory = _ => 10
        };

        quickPerfCommand.Add(rowsOption3);
        quickPerfCommand.Add(iterationsOption3);
        quickPerfCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var rows = parseResult.GetValue(rowsOption3);
            var iterations = parseResult.GetValue(iterationsOption3);
            RunQuickPerformance(output, verbose, rows, iterations);
            return 0;
        });

        var internalPerfCommand = new Command("internal", "Internal HeroCsv performance benchmarks");
        internalPerfCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            RunInternalBenchmarks(output, verbose);
            return 0;
        });

        perfCommand.Add(quickPerfCommand);
        perfCommand.Add(internalPerfCommand);

        // Quick benchmarks for CI/CD
        var quickCommand = new Command("quick", "Quick benchmarks for CI/CD and rapid testing");
        quickCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            RunQuickBenchmarks(output, verbose);
            return 0;
        });
        
        // Feature benchmarks for comprehensive testing
        var featuresCommand = new Command("features", "Benchmark all HeroCsv features");
        featuresCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            RunFeatureBenchmarks(output, verbose);
            return 0;
        });
        
        // Competitor comparison for transparency
        var competitorsCommand = new Command("competitors", "Compare performance with other CSV libraries");
        competitorsCommand.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            RunCompetitorBenchmarks(output, verbose);
            return 0;
        });
        
        // Add all commands to root (simplified structure)
        rootCommand.Add(quickCommand);
        rootCommand.Add(featuresCommand);
        rootCommand.Add(competitorsCommand);
        rootCommand.Add(realDataCommand);
        rootCommand.Add(libraryCommand);
        rootCommand.Add(perfCommand);

        // List command to show available test files
        var listCommand = new Command("list", "List available test files and benchmarks");
        listCommand.SetAction(parseResult =>
        {
            var verbose = parseResult.GetValue(verboseOption);
            ListAvailableResources(verbose);
            return 0;
        });
        rootCommand.Add(listCommand);

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static void RunRealDataBenchmark(DirectoryInfo? output, bool verbose, bool quick, bool noExport)
    {
        if (verbose) AnsiConsole.MarkupLine("[yellow]🚀 Running Real Data Performance Benchmarks[/]");
        if (verbose && output != null) AnsiConsole.MarkupLine($"[grey]📁 Output directory:[/] [blue]{output.FullName}[/]");
        if (quick) AnsiConsole.MarkupLine("[cyan]⚡ Quick mode: Running subset of tests[/]");

        RealDataBenchmark.RunRealDataComparison();
    }

    private static void RunSimpleComparison(DirectoryInfo? output, bool verbose, int rows, int iterations)
    {
        if (verbose) AnsiConsole.MarkupLine($"[yellow]📊 Running Library Comparison[/] [grey](rows: {rows}, iterations: {iterations})[/]");

        BenchmarkRunner.Run<SimplifiedComparison>(CreateUnifiedConfig("SimplifiedComparison", output));
    }

    private static void RunDirectComparison(DirectoryInfo? output, bool verbose, int rows, int iterations)
    {
        if (verbose) AnsiConsole.MarkupLine($"[yellow]⚔️  Running Direct HeroCsv vs Sep Comparison[/] [grey](rows: {rows}, iterations: {iterations})[/]");

        BenchmarkRunner.Run<DirectComparison>(CreateUnifiedConfig("DirectComparison", output));
    }

    private static void RunQuickPerformance(DirectoryInfo? output, bool verbose, int rows, int iterations)
    {
        if (verbose) AnsiConsole.MarkupLine($"[yellow]🏃 Running Quick Performance Analysis[/] [grey](rows: {rows}, iterations: {iterations})[/]");

        // This method is now replaced by QuickBenchmarks
        BenchmarkRunner.Run<QuickBenchmarks>(CreateUnifiedConfig("QuickBenchmarks", output));
    }

    private static void RunInternalBenchmarks(DirectoryInfo? output, bool verbose)
    {
        if (verbose) AnsiConsole.MarkupLine("[yellow]🧪 Running Internal HeroCsv Benchmarks[/]");

        BenchmarkRunner.Run<CsvParsingBenchmarks>(CreateUnifiedConfig("CsvParsingBenchmarks", output));
    }
    
    private static void RunQuickBenchmarks(DirectoryInfo? output, bool verbose)
    {
        if (verbose) AnsiConsole.MarkupLine("[yellow]⚡ Running Quick Benchmarks for CI/CD[/]");
        
        BenchmarkRunner.Run<QuickBenchmarks>(CreateUnifiedConfig("QuickBenchmarks", output));
    }
    
    private static void RunFeatureBenchmarks(DirectoryInfo? output, bool verbose)
    {
        if (verbose) AnsiConsole.MarkupLine("[yellow]📊 Running Feature Benchmarks[/]");
        
        BenchmarkRunner.Run<FeatureBenchmarks>(CreateUnifiedConfig("FeatureBenchmarks", output));
    }
    
    private static void RunCompetitorBenchmarks(DirectoryInfo? output, bool verbose)
    {
        if (verbose) AnsiConsole.MarkupLine("[yellow]🏆 Running Competitor Comparison Benchmarks[/]");
        
        BenchmarkRunner.Run<CompetitorBenchmarks>(CreateUnifiedConfig("CompetitorBenchmarks", output));
    }

    private static void ListAvailableResources(bool verbose)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]📋 Available Benchmark Resources[/]").RuleStyle(Style.Parse("yellow")));
        AnsiConsole.WriteLine();

        // Test Data Files
        var filesPanel = new Panel(GetTestFilesContent())
            .Header("[blue]🗂️  Test Data Files[/]")
            .Expand()
            .RoundedBorder()
            .BorderColor(Color.Blue);
        AnsiConsole.Write(filesPanel);
        AnsiConsole.WriteLine();

        // Benchmark Types
        var typesTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Command[/]")
            .AddColumn("[yellow]Description[/]");

        typesTable.AddRow("[green]quick[/]", "Quick benchmarks for CI/CD and rapid testing");
        typesTable.AddRow("[green]features[/]", "Benchmark all HeroCsv features (core, mapping, I/O, advanced)");
        typesTable.AddRow("[green]competitors[/]", "Transparent comparison with CsvHelper, Sylvan, Sep");
        typesTable.AddRow("[green]realdata[/]", "Real CSV files performance testing");
        typesTable.AddRow("[green]library[/]", "Library-specific comparisons");
        typesTable.AddRow("[green]perf[/]", "Internal performance analysis");

        var typesPanel = new Panel(typesTable)
            .Header("[blue]🏆 Benchmark Types[/]")
            .Expand()
            .RoundedBorder()
            .BorderColor(Color.Blue);
        AnsiConsole.Write(typesPanel);
        AnsiConsole.WriteLine();

        // Export Formats
        var formatsGrid = new Grid();
        formatsGrid.AddColumn();
        formatsGrid.AddColumn();
        formatsGrid.AddRow(
            "[green]• JSON[/] - Machine-readable structured data",
            "[green]• CSV[/] - Spreadsheet-compatible format"
        );
        formatsGrid.AddRow(
            "[green]• Markdown[/] - Human-readable reports",
            "[green]• HTML[/] - Interactive web reports"
        );

        var formatsPanel = new Panel(formatsGrid)
            .Header("[blue]📊 Export Formats[/]")
            .Expand()
            .RoundedBorder()
            .BorderColor(Color.Blue);
        AnsiConsole.Write(formatsPanel);
    }

    private static IRenderable GetTestFilesContent()
    {
        try
        {
            var testDataDir = Path.Combine(FindSolutionRoot() ?? ".", "tests", "TestData");
            if (Directory.Exists(testDataDir))
            {
                var files = Directory.GetFiles(testDataDir, "*.csv").OrderBy(f => f);

                var table = new Table()
                    .Border(TableBorder.None)
                    .AddColumn("[cyan]File[/]")
                    .AddColumn(new TableColumn("[cyan]Size[/]").RightAligned());

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var fileInfo = new FileInfo(file);
                    var size = FormatFileSize(fileInfo.Length);

                    var sizeColor = fileInfo.Length > 1_000_000 ? "red" :
                                   fileInfo.Length > 100_000 ? "yellow" :
                                   fileInfo.Length > 10_000 ? "green" : "grey";

                    table.AddRow($"{fileName}", $"[{sizeColor}]{size}[/]");
                }

                return table;
            }
            else
            {
                return new Markup("[red]❌ Test data directory not found[/]");
            }
        }
        catch (Exception ex)
        {
            return new Markup($"[red]❌ Error accessing test files: {ex.Message}[/]");
        }
    }

    private static IConfig CreateUnifiedConfig(string benchmarkType, DirectoryInfo? customOutput)
    {
        string outputDir;

        if (customOutput != null)
        {
            outputDir = Path.Combine(customOutput.FullName, "BenchmarkDotNet", benchmarkType);
        }
        else
        {
            outputDir = BenchmarkExporter.GetBenchmarkOutputDirectory($"BenchmarkDotNet/{benchmarkType}");
        }

        return DefaultConfig.Instance.WithArtifactsPath(outputDir);
    }

    private static string? FindSolutionRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0)
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return null;
    }

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