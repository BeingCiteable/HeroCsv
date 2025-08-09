using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Utilities;

namespace HeroCsv.Tests.Integration;

public class OptimizationIntegrationTests
{
    [Fact]
    public void AllOptimizations_WorkTogether_LargeDataset()
    {
        // Arrange - Create a large CSV with patterns that benefit from all optimizations
        var rows = new List<string>();
        rows.Add("ID,Status,Active,Value,Category,Date,Description");

        for (int i = 0; i < 10000; i++)
        {
            // Mix of common values (good for StringPool) and unique values
            var status = i % 3 == 0 ? "Active" : i % 3 == 1 ? "Inactive" : "Pending";
            var active = i % 2 == 0 ? "true" : "false";
            var category = $"CAT_{i % 100}"; // 100 repeating categories
            var description = i % 10 == 0 ?
                $"\"Long description with, comma and \"\"quotes\"\" for ID {i}\"" :
                $"Desc_{i}";

            rows.Add($"{i},{status},{active},{i * 1.5},{category},2024-01-{(i % 28) + 1:D2},{description}");
        }

        var csvContent = string.Join("\n", rows);
        var stringPool = new StringPool();
        var options = new CsvOptions(',', '"', true, stringPool: stringPool);

        // Act
        var startMem = GC.GetTotalMemory(true);
        var records = Csv.ReadContent(csvContent, options).ToList();
        var endMem = GC.GetTotalMemory(false);

        // Assert
        Assert.Equal(10000, records.Count);
        Assert.All(records, r => Assert.Equal(7, r.Length));

        // Verify StringPool is working (common values should be interned)
        var firstActive = records.First(r => r[2] == "true")[2];
        var lastActive = records.Last(r => r[2] == "true")[2];
        Assert.Same(firstActive, lastActive); // Should be same reference due to StringPool

        // Memory usage should be reasonable
        var memUsedMB = (endMem - startMem) / (1024.0 * 1024.0);
        Assert.True(memUsedMB < 50, $"Memory usage too high: {memUsedMB:F2} MB");
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task AsyncOperations_WithOptimizations_WorkCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var lines = new List<string>
            {
                "Name,Status,Count",
                "Item1,Active,100",
                "Item2,Inactive,200",
                "Item3,Active,300"
            };
            await File.WriteAllLinesAsync(tempFile, lines, TestContext.Current.CancellationToken);

            var stringPool = new StringPool();
            var options = new CsvOptions(',', '"', true, stringPool: stringPool);

            // Act
            var records = new List<string[]>();
            var fileRecords = await Csv.ReadFileAsync(tempFile, options, cancellationToken: TestContext.Current.CancellationToken);
            records.AddRange(fileRecords);

            // Assert
            Assert.Equal(3, records.Count);
            // Verify StringPool worked for repeated "Active" status
            Assert.Same(records[0][1], records[2][1]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
#endif

#if NET8_0_OR_GREATER
    [Fact]
    public void AutoDetection_WithSearchValues_DetectsCorrectly()
    {
        // Arrange - Various CSV formats
        var testCases = new[]
        {
            (csv: "a,b,c\n1,2,3", delimiter: ','),
            (csv: "a;b;c\n1;2;3", delimiter: ';'),
            (csv: "a\tb\tc\n1\t2\t3", delimiter: '\t'),
            (csv: "a|b|c\n1|2|3", delimiter: '|')
        };

        foreach (var testCase in testCases)
        {
            // Act - ReadAutoDetect uses auto-detection internally
            var records = Csv.ReadAutoDetect(testCase.csv).ToList();

            // Assert - Should parse correctly with auto-detected delimiter
            // AutoDetectFormat always sets hasHeader = true, so first line is treated as header
            Assert.Single(records); // One data row after header
            Assert.Equal(3, records[0].Length);
            Assert.Equal("1", records[0][0]);
            Assert.Equal("2", records[0][1]);
            Assert.Equal("3", records[0][2]);
        }
    }

    [Fact]
    public void FrozenCollections_ImprovePerformance_CommonValues()
    {
        // Arrange
        var csvWithManyBooleans = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            csvWithManyBooleans.Add($"{i},true,false,YES,NO,1,0,T,F");
        }
        var content = string.Join("\n", csvWithManyBooleans);

        var pool = new StringPool();
        var options = new CsvOptions(',', stringPool: pool);

        // Act - Parse multiple times to test StringPool efficiency
        var firstParse = Csv.ReadContent(content, options).ToList();
        var secondParse = Csv.ReadContent(content, options).ToList();

        // Assert - StringPool should intern common values
        Assert.Equal(firstParse.Count, secondParse.Count);

        // Check that common values are actually interned (same reference)
        for (int i = 0; i < Math.Min(10, firstParse.Count); i++)
        {
            // "true", "false", etc should be the same reference across parses
            Assert.Same(firstParse[i][1], secondParse[i][1]); // "true"
            Assert.Same(firstParse[i][2], secondParse[i][2]); // "false"
        }
    }
#endif

#if NET9_0_OR_GREATER
    [Fact(Skip = "Vector512 tests require AVX-512 hardware support")]
    public void Vector512_Integration_LargeFields()
    {
        if (!System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated) return;

        // Arrange - CSV with very wide rows (good for Vector512)
        var fieldCount = 200;
        var rowCount = 100;
        var rows = new List<string>();

        for (int r = 0; r < rowCount; r++)
        {
            var fields = Enumerable.Range(0, fieldCount).Select(f => $"R{r}F{f}");
            rows.Add(string.Join(",", fields));
        }

        var csvContent = string.Join("\n", rows);
        var options = new CsvOptions(',');

        // Act
        var records = Csv.ReadContent(csvContent, options).ToList();

        // Assert
        Assert.Equal(rowCount, records.Count);
        Assert.All(records, r => Assert.Equal(fieldCount, r.Length));

        // Verify field values
        for (int r = 0; r < Math.Min(10, rowCount); r++)
        {
            for (int f = 0; f < Math.Min(10, fieldCount); f++)
            {
                Assert.Equal($"R{r}F{f}", records[r][f]);
            }
        }
    }
#endif

    [Fact]
    public void BufferPool_ReuseAcrossMultipleParsing()
    {
        // Arrange
        var csvFiles = new List<string>();
        for (int f = 0; f < 10; f++)
        {
            var lines = new List<string>();
            for (int i = 1; i <= 100; i++)
            {
                lines.Add($"file{f}_field1_{i},file{f}_field2_{i},file{f}_field3_{i}");
            }
            csvFiles.Add(string.Join("\n", lines));
        }

        var pool = new StringPool();
        var options = new CsvOptions(',', '"', false, stringPool: pool); // hasHeader = false

        // Act - Parse multiple files, buffer pool should reuse buffers
        var allRecords = new List<List<string[]>>();
        var startMem = GC.GetTotalMemory(true);

        foreach (var csv in csvFiles)
        {
            var records = Csv.ReadContent(csv, options).ToList();
            allRecords.Add(records);
        }

        var endMem = GC.GetTotalMemory(false);

        // Assert
        Assert.Equal(10, allRecords.Count);
        Assert.All(allRecords, records => Assert.Equal(100, records.Count));

        // Memory usage should be efficient due to buffer reuse
        var totalMemUsedMB = (endMem - startMem) / (1024.0 * 1024.0);
        Assert.True(totalMemUsedMB < 10, $"Total memory usage too high: {totalMemUsedMB:F2} MB");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Optimizations_ScaleWell_VariousDataSizes(int rowCount)
    {
        // Arrange
        var rows = new List<string>();
        rows.Add("ID,Type,Status,Value,Timestamp");

        for (int i = 0; i < rowCount; i++)
        {
            var type = (i % 5) switch
            {
                0 => "TypeA",
                1 => "TypeB",
                2 => "TypeC",
                3 => "TypeD",
                _ => "TypeE"
            };
            var status = i % 2 == 0 ? "true" : "false";
            rows.Add($"{i},{type},{status},{i * 10.5},2024-01-01T{(i % 24):D2}:00:00");
        }

        var csvContent = string.Join("\n", rows);
        var pool = new StringPool();
        var options = new CsvOptions(',', '"', true, stringPool: pool);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var records = Csv.ReadContent(csvContent, options).ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(rowCount, records.Count);

        // Performance should scale reasonably
        var msPerRow = stopwatch.ElapsedMilliseconds / (double)rowCount;
        Assert.True(msPerRow < 1.0, $"Performance degraded: {msPerRow:F3} ms per row");
    }
}