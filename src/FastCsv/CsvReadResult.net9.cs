#if NET9_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FastCsv;

/// <summary>
/// NET9+ advanced optimizations for CsvReadResult
/// </summary>
public sealed partial class CsvReadResult
{
    /// <summary>
    /// Creates a successful result with advanced profiling metrics
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="activity">Activity for distributed tracing</param>
    /// <returns>Successful CSV read result with profiling data</returns>
    public static CsvReadResult SuccessWithProfiling(
        IReadOnlyList<string[]> records,
        TimeSpan processingTime,
        Activity? activity = null)
    {
        var statistics = new Dictionary<string, object>
        {
            ["RecordCount"] = records.Count,
            ["AverageFieldsPerRecord"] = records.Count > 0 ? records.Average(r => r.Length) : 0,
            ["ProcessingTimeMs"] = processingTime.TotalMilliseconds,
            ["RecordsPerSecond"] = records.Count / Math.Max(processingTime.TotalSeconds, 0.001),
            ["Vector512Supported"] = System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated,
            ["TimestampUtc"] = DateTimeOffset.UtcNow
        };

        if (activity != null)
        {
            statistics["TraceId"] = activity.TraceId.ToString();
            statistics["SpanId"] = activity.SpanId.ToString();
            statistics["Duration"] = activity.Duration.TotalMilliseconds;
        }

        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime,
            Statistics = statistics.ToFrozenDictionary()
        };
    }

    /// <summary>
    /// Gets detailed performance metrics for analysis
    /// </summary>
    /// <returns>Detailed performance metrics</returns>
    public PerformanceMetrics GetPerformanceMetrics()
    {
        var stats = Statistics;
        return new PerformanceMetrics
        {
            RecordCount = TotalRecords,
            ProcessingTimeMs = ProcessingTime.TotalMilliseconds,
            RecordsPerSecond = stats.TryGetValue("RecordsPerSecond", out var rps) ? Convert.ToDouble(rps) : 0,
            AverageFieldsPerRecord = stats.TryGetValue("AverageFieldsPerRecord", out var avg) ? Convert.ToDouble(avg) : 0,
            Vector512Supported = stats.TryGetValue("Vector512Supported", out var v512) && Convert.ToBoolean(v512),
            TimestampUtc = stats.TryGetValue("TimestampUtc", out var ts) ? (DateTimeOffset)ts : DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Performance metrics for detailed analysis
    /// </summary>
    public readonly record struct PerformanceMetrics
    {
        public int RecordCount { get; init; }
        public double ProcessingTimeMs { get; init; }
        public double RecordsPerSecond { get; init; }
        public double AverageFieldsPerRecord { get; init; }
        public bool Vector512Supported { get; init; }
        public DateTimeOffset TimestampUtc { get; init; }

        public override string ToString() =>
            $"Records: {RecordCount}, Time: {ProcessingTimeMs:F2}ms, Rate: {RecordsPerSecond:F0}/sec, Avg Fields: {AverageFieldsPerRecord:F1}, Vector512: {Vector512Supported}";
    }
}
#endif