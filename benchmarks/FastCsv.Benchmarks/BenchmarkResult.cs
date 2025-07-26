using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace FastCsv.Benchmarks;

/// <summary>
/// Unified benchmark result data structure for consistent reporting
/// </summary>
public class BenchmarkResult
{
    public string BenchmarkName { get; set; } = "";
    public string TestCase { get; set; } = "";
    public string Library { get; set; } = "";
    public string Method { get; set; } = "";
    public int RowCount { get; set; }
    public int Iterations { get; set; }
    public double MeanTimeMs { get; set; }
    public double StdDevMs { get; set; }
    public long AllocatedBytes { get; set; }
    public string FileSize { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = "";
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Collection of benchmark results with metadata
/// </summary>
public class BenchmarkResultSet
{
    public string BenchmarkSuite { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MachineName { get; set; } = Environment.MachineName;
    public string RuntimeVersion { get; set; } = Environment.Version.ToString();
    public List<BenchmarkResult> Results { get; set; } = new();
}

/// <summary>
/// Unified exporter for benchmark results in multiple formats:
/// - JSON: Machine-readable structured data with full metadata
/// - CSV: Spreadsheet-compatible format for data analysis  
/// - Markdown: Human-readable reports for documentation
/// - HTML: Interactive web reports with styling and charts
/// - Spectre: Rich console output with colored tables and charts
/// </summary>
public static class BenchmarkExporter
{
    /// <summary>
    /// Export results to JSON format
    /// </summary>
    public static void ExportToJson(BenchmarkResultSet resultSet, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(resultSet, options);
        File.WriteAllText(filePath, json);
        AnsiConsole.MarkupLine($"[green]📄 Results exported to JSON:[/] [blue]{filePath}[/]");
    }

    /// <summary>
    /// Export results to CSV format for Excel/analysis
    /// </summary>
    public static void ExportToCsv(BenchmarkResultSet resultSet, string filePath)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("BenchmarkName,TestCase,Library,Method,RowCount,Iterations,MeanTimeMs,StdDevMs,AllocatedBytes,FileSize,Timestamp,Environment");
        
        // Data rows
        foreach (var result in resultSet.Results)
        {
            csv.AppendLine($"{EscapeCsv(result.BenchmarkName)},{EscapeCsv(result.TestCase)},{EscapeCsv(result.Library)},{EscapeCsv(result.Method)},{result.RowCount},{result.Iterations},{result.MeanTimeMs:F3},{result.StdDevMs:F3},{result.AllocatedBytes},{EscapeCsv(result.FileSize)},{result.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(result.Environment)}");
        }
        
        File.WriteAllText(filePath, csv.ToString());
        AnsiConsole.MarkupLine($"[green]📊 Results exported to CSV:[/] [blue]{filePath}[/]");
    }

    /// <summary>
    /// Export results to Markdown format for documentation
    /// </summary>
    public static void ExportToMarkdown(BenchmarkResultSet resultSet, string filePath)
    {
        var md = new StringBuilder();
        
        md.AppendLine($"# {resultSet.BenchmarkSuite} Results");
        md.AppendLine();
        md.AppendLine($"**Generated:** {resultSet.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        md.AppendLine($"**Machine:** {resultSet.MachineName}");
        md.AppendLine($"**Runtime:** .NET {resultSet.RuntimeVersion}");
        md.AppendLine();
        
        // Group by benchmark name
        var groupedResults = resultSet.Results.GroupBy(r => r.BenchmarkName);
        
        foreach (var group in groupedResults)
        {
            md.AppendLine($"## {group.Key}");
            md.AppendLine();
            md.AppendLine("| Library | Method | Rows | Mean (ms) | StdDev (ms) | Allocated | Notes |");
            md.AppendLine("|---------|--------|------|-----------|-------------|-----------|-------|");
            
            foreach (var result in group.OrderBy(r => r.MeanTimeMs))
            {
                var notes = result.FileSize;
                md.AppendLine($"| {result.Library} | {result.Method} | {result.RowCount:N0} | {result.MeanTimeMs:F2} | {result.StdDevMs:F2} | {FormatBytes(result.AllocatedBytes)} | {notes} |");
            }
            md.AppendLine();
        }
        
        File.WriteAllText(filePath, md.ToString());
        AnsiConsole.MarkupLine($"[green]📝 Results exported to Markdown:[/] [blue]{filePath}[/]");
    }

    /// <summary>
    /// Export results to HTML format with interactive features
    /// </summary>
    public static void ExportToHtml(BenchmarkResultSet resultSet, string filePath)
    {
        var html = new StringBuilder();
        
        // HTML header with styling
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"    <title>{EscapeHtml(resultSet.BenchmarkSuite)} Results</title>");
        html.AppendLine("    <style>");
        html.AppendLine(@"
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .metadata { background: #ecf0f1; padding: 20px; border-radius: 5px; margin: 20px 0; }
        .metadata strong { color: #2c3e50; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #3498db; color: white; font-weight: 600; }
        tr:nth-child(even) { background: #f8f9fa; }
        tr:hover { background: #e8f4f8; }
        .fastest { background: #d4edda !important; color: #155724; font-weight: bold; }
        .slowest { background: #f8d7da !important; color: #721c24; }
        .number { text-align: right; font-family: 'Monaco', 'Consolas', monospace; }
        .chart-container { margin: 20px 0; }
        .bar-chart { display: flex; align-items: flex-end; height: 200px; margin: 20px 0; }
        .bar { background: #3498db; margin: 2px; border-radius: 3px 3px 0 0; min-width: 20px; position: relative; }
        .bar-label { position: absolute; bottom: -25px; left: 50%; transform: translateX(-50%); font-size: 10px; text-align: center; }
        .summary { background: #e8f6f3; padding: 20px; border-left: 4px solid #1abc9c; margin: 20px 0; }
        .footer { text-align: center; margin-top: 40px; color: #7f8c8d; border-top: 1px solid #eee; padding-top: 20px; }
        ");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        
        // Header
        html.AppendLine($"        <h1>{EscapeHtml(resultSet.BenchmarkSuite)} Results</h1>");
        
        // Metadata section
        html.AppendLine("        <div class=\"metadata\">");
        html.AppendLine($"            <strong>Generated:</strong> {resultSet.Timestamp:yyyy-MM-dd HH:mm:ss} UTC<br>");
        html.AppendLine($"            <strong>Machine:</strong> {EscapeHtml(resultSet.MachineName)}<br>");
        html.AppendLine($"            <strong>Runtime:</strong> .NET {EscapeHtml(resultSet.RuntimeVersion)}<br>");
        html.AppendLine($"            <strong>Total Tests:</strong> {resultSet.Results.Count}");
        html.AppendLine("        </div>");
        
        // Group by benchmark name and test case
        var groupedResults = resultSet.Results
            .GroupBy(r => r.TestCase)
            .OrderBy(g => g.Key);
        
        foreach (var group in groupedResults)
        {
            html.AppendLine($"        <h2>{EscapeHtml(group.Key)}</h2>");
            
            var results = group.OrderBy(r => r.MeanTimeMs).ToList();
            var fastest = results.First();
            var slowest = results.Last();
            
            // Summary box
            html.AppendLine("        <div class=\"summary\">");
            html.AppendLine($"            <strong>Best Method:</strong> {EscapeHtml(fastest.Method)} ({fastest.MeanTimeMs:F2} ms/op)<br>");
            html.AppendLine($"            <strong>Records:</strong> {fastest.RowCount:N0}<br>");
            html.AppendLine($"            <strong>File Size:</strong> {EscapeHtml(fastest.FileSize)}");
            html.AppendLine("        </div>");
            
            // Performance table
            html.AppendLine("        <table>");
            html.AppendLine("            <thead>");
            html.AppendLine("                <tr>");
            html.AppendLine("                    <th>Library</th>");
            html.AppendLine("                    <th>Method</th>");
            html.AppendLine("                    <th class=\"number\">Mean (ms)</th>");
            html.AppendLine("                    <th class=\"number\">Records</th>");
            html.AppendLine("                    <th class=\"number\">Iterations</th>");
            html.AppendLine("                    <th class=\"number\">Records/sec</th>");
            html.AppendLine("                    <th>Performance</th>");
            html.AppendLine("                </tr>");
            html.AppendLine("            </thead>");
            html.AppendLine("            <tbody>");
            
            foreach (var result in results)
            {
                var cssClass = "";
                if (result == fastest) cssClass = "fastest";
                else if (result == slowest && results.Count > 1) cssClass = "slowest";
                
                var recordsPerSec = result.MeanTimeMs > 0 ? (result.RowCount / (result.MeanTimeMs / 1000.0)) : 0;
                var performanceRatio = fastest.MeanTimeMs > 0 ? (result.MeanTimeMs / fastest.MeanTimeMs) : 1;
                var performanceText = performanceRatio <= 1.1 ? "Excellent" : 
                                    performanceRatio <= 2.0 ? "Good" : 
                                    performanceRatio <= 5.0 ? "Fair" : "Slow";
                
                html.AppendLine($"                <tr class=\"{cssClass}\">");
                html.AppendLine($"                    <td>{EscapeHtml(result.Library)}</td>");
                html.AppendLine($"                    <td>{EscapeHtml(result.Method)}</td>");
                html.AppendLine($"                    <td class=\"number\">{result.MeanTimeMs:F3}</td>");
                html.AppendLine($"                    <td class=\"number\">{result.RowCount:N0}</td>");
                html.AppendLine($"                    <td class=\"number\">{result.Iterations}</td>");
                html.AppendLine($"                    <td class=\"number\">{recordsPerSec:N0}</td>");
                html.AppendLine($"                    <td>{performanceText} ({performanceRatio:F1}x)</td>");
                html.AppendLine("                </tr>");
            }
            
            html.AppendLine("            </tbody>");
            html.AppendLine("        </table>");
            
            // Simple bar chart
            if (results.Count > 1)
            {
                html.AppendLine("        <div class=\"chart-container\">");
                html.AppendLine("            <h3>Performance Comparison</h3>");
                html.AppendLine("            <div class=\"bar-chart\">");
                
                var maxTime = results.Max(r => r.MeanTimeMs);
                foreach (var result in results)
                {
                    var height = maxTime > 0 ? (int)((result.MeanTimeMs / maxTime) * 150) : 1;
                    html.AppendLine($"                <div class=\"bar\" style=\"height: {height}px;\">");
                    html.AppendLine($"                    <div class=\"bar-label\">{EscapeHtml(result.Method.Replace(" ", "<br>"))}</div>");
                    html.AppendLine("                </div>");
                }
                
                html.AppendLine("            </div>");
                html.AppendLine("        </div>");
            }
        }
        
        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Generated by FastCsv Benchmark Suite</p>");
        html.AppendLine($"            <p>Report generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("        </div>");
        
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        File.WriteAllText(filePath, html.ToString());
        AnsiConsole.MarkupLine($"[green]🌐 Results exported to HTML:[/] [blue]{filePath}[/]");
    }

    /// <summary>
    /// Display results in console using Spectre.Console
    /// </summary>
    public static void DisplayInConsole(BenchmarkResultSet resultSet)
    {
        // Header
        AnsiConsole.Clear();
        var rule = new Rule($"[yellow]FastCsv Benchmark Results - {resultSet.BenchmarkSuite}[/]")
            .RuleStyle(Style.Parse("yellow"))
            .LeftJustified();
        AnsiConsole.Write(rule);
        
        // System info panel
        var infoGrid = new Grid();
        infoGrid.AddColumn();
        infoGrid.AddColumn();
        infoGrid.AddRow(
            $"[grey]Machine:[/] {resultSet.MachineName}",
            $"[grey]Runtime:[/] .NET {resultSet.RuntimeVersion}"
        );
        infoGrid.AddRow(
            $"[grey]Timestamp:[/] {resultSet.Timestamp:yyyy-MM-dd HH:mm:ss}",
            $"[grey]Results:[/] {resultSet.Results.Count} benchmarks"
        );
        
        var infoPanel = new Panel(infoGrid)
            .Header("[blue]Environment[/]")
            .Expand()
            .RoundedBorder()
            .BorderColor(Color.Blue);
        AnsiConsole.Write(infoPanel);
        AnsiConsole.WriteLine();
        
        // Group results by test case
        var groupedResults = resultSet.Results.GroupBy(r => r.TestCase);
        
        foreach (var group in groupedResults)
        {
            AnsiConsole.Write(new Rule($"[cyan]{group.Key}[/]").RuleStyle(Style.Parse("cyan dim")));
            
            // Create table for this test case
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[yellow]Library[/]")
                .AddColumn("[yellow]Method[/]")
                .AddColumn(new TableColumn("[yellow]Mean (ms)[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Records[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Records/sec[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Performance[/]").Centered());
            
            var results = group.OrderBy(r => r.MeanTimeMs).ToList();
            var fastest = results.First();
            
            foreach (var result in results)
            {
                var recordsPerSec = result.MeanTimeMs > 0 ? (result.RowCount / (result.MeanTimeMs / 1000.0)) : 0;
                var performanceRatio = fastest.MeanTimeMs > 0 ? (result.MeanTimeMs / fastest.MeanTimeMs) : 1;
                
                // Color coding based on performance
                var libraryColor = result == fastest ? "green" : 
                                 performanceRatio <= 1.5 ? "yellow" : 
                                 performanceRatio <= 3.0 ? "orange1" : "red";
                
                var performanceBar = "";
                var barLength = (int)(20 * (fastest.MeanTimeMs / result.MeanTimeMs));
                barLength = Math.Max(1, Math.Min(20, barLength));
                performanceBar = new string('█', barLength);
                
                var performanceText = performanceRatio <= 1.1 ? "[green]Excellent[/]" : 
                                    performanceRatio <= 2.0 ? "[yellow]Good[/]" : 
                                    performanceRatio <= 5.0 ? "[orange1]Fair[/]" : "[red]Slow[/]";
                
                table.AddRow(
                    $"[{libraryColor}]{result.Library}[/]",
                    $"[{libraryColor}]{result.Method}[/]",
                    $"[{libraryColor}]{result.MeanTimeMs:F3}[/]",
                    $"[{libraryColor}]{result.RowCount:N0}[/]",
                    $"[{libraryColor}]{recordsPerSec:N0}[/]",
                    $"{performanceText} [{libraryColor}]{performanceBar}[/] ({performanceRatio:F1}x)"
                );
            }
            
            AnsiConsole.Write(table);
            
            // Performance chart
            if (results.Count > 1)
            {
                AnsiConsole.WriteLine();
                var chart = new BarChart()
                    .Width(60)
                    .Label("[yellow]Performance Comparison (ms)[/]")
                    .CenterLabel();
                
                foreach (var result in results.Take(10)) // Limit to top 10 for readability
                {
                    var color = result == fastest ? Color.Green : Color.Blue;
                    chart.AddItem(result.Library, result.MeanTimeMs, color);
                }
                
                AnsiConsole.Write(chart);
            }
            
            AnsiConsole.WriteLine();
        }
        
        // Summary statistics
        if (resultSet.Results.Count > 0)
        {
            var summaryTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[yellow]Metric[/]")
                .AddColumn(new TableColumn("[yellow]Value[/]").RightAligned());
            
            var totalTime = resultSet.Results.Sum(r => r.MeanTimeMs * r.Iterations);
            var totalRecords = resultSet.Results.Sum(r => r.RowCount * r.Iterations);
            var avgTimePerRecord = totalRecords > 0 ? totalTime / totalRecords : 0;
            
            summaryTable.AddRow("Total Benchmarks", $"{resultSet.Results.Count:N0}");
            summaryTable.AddRow("Total Time (ms)", $"{totalTime:N0}");
            summaryTable.AddRow("Total Records Processed", $"{totalRecords:N0}");
            summaryTable.AddRow("Avg Time per Record (µs)", $"{avgTimePerRecord * 1000:F2}");
            
            var summaryPanel = new Panel(summaryTable)
                .Header("[green]Summary Statistics[/]")
                .Expand()
                .RoundedBorder()
                .BorderColor(Color.Green);
            
            AnsiConsole.Write(summaryPanel);
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[grey]End of Report[/]").RuleStyle(Style.Parse("grey dim")));
    }

    /// <summary>
    /// Export all formats to a directory
    /// </summary>
    public static void ExportAll(BenchmarkResultSet resultSet, string? outputDirectory = null, bool showInConsole = true)
    {
        outputDirectory ??= GetBenchmarkOutputDirectory();
        Directory.CreateDirectory(outputDirectory);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var baseName = $"{resultSet.BenchmarkSuite.Replace(" ", "")}-{timestamp}";
        
        // Display in console first if requested
        if (showInConsole)
        {
            DisplayInConsole(resultSet);
            AnsiConsole.WriteLine();
        }
        
        // Export to files
        ExportToJson(resultSet, Path.Combine(outputDirectory, $"{baseName}.json"));
        ExportToCsv(resultSet, Path.Combine(outputDirectory, $"{baseName}.csv"));
        ExportToMarkdown(resultSet, Path.Combine(outputDirectory, $"{baseName}.md"));
        ExportToHtml(resultSet, Path.Combine(outputDirectory, $"{baseName}.html"));
        
        AnsiConsole.MarkupLine($"[green]✅ All formats exported to:[/] [blue]{Path.GetFullPath(outputDirectory)}[/]");
    }

    /// <summary>
    /// Gets the standard benchmark output directory for the solution
    /// </summary>
    public static string GetBenchmarkOutputDirectory()
    {
        // Find the solution root by looking for the .sln file
        var currentDir = Directory.GetCurrentDirectory();
        var solutionRoot = FindSolutionRoot(currentDir);
        
        if (solutionRoot == null)
        {
            // Fallback to current directory if solution root not found
            return Path.Combine(currentDir, "benchmark-results");
        }
        
        return Path.Combine(solutionRoot, "BenchmarkResults");
    }

    /// <summary>
    /// Creates a benchmark output subdirectory for specific benchmark types
    /// </summary>
    public static string GetBenchmarkOutputDirectory(string benchmarkType)
    {
        var baseDir = GetBenchmarkOutputDirectory();
        var typeDir = Path.Combine(baseDir, benchmarkType);
        Directory.CreateDirectory(typeDir);
        return typeDir;
    }

    private static string? FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
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

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }

    private static string EscapeHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
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