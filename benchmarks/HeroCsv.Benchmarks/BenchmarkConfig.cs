using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace HeroCsv.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig() : this(true)
    {
    }
    
    public BenchmarkConfig(bool exportAll)
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
        
        // Always export GitHub markdown
        AddExporter(MarkdownExporter.GitHub);
        
        if (exportAll)
        {
            // Add all export formats for transparency
            AddExporter(HtmlExporter.Default);
            AddExporter(JsonExporter.Full);
            AddExporter(JsonExporter.Brief);
            AddExporter(CsvExporter.Default);
            AddExporter(PlainExporter.Default);
        }
        
        AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}