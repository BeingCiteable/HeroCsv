using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using HeroCsv;
using HeroCsv.Models;
using Xunit;

namespace HeroCsv.Tests.Unit.Core;

public class ConcurrencyTests
{
    private const string CsvData = """
        ID,Name,Value
        1,Item1,100
        2,Item2,200
        3,Item3,300
        4,Item4,400
        5,Item5,500
        """;

    private const string LargeCsvData = """
        ID,Name,Email,Age,City,Country,Status
        1,John Doe,john@example.com,25,NYC,USA,Active
        2,Jane Smith,jane@example.com,30,LA,USA,Active
        3,Bob Johnson,bob@example.com,35,Chicago,USA,Inactive
        4,Alice Brown,alice@example.com,28,Houston,USA,Active
        5,Charlie Wilson,charlie@example.com,42,Phoenix,USA,Active
        6,Diana Davis,diana@example.com,33,Seattle,USA,Active
        7,Eve Miller,eve@example.com,27,Boston,USA,Inactive
        8,Frank Garcia,frank@example.com,31,Denver,USA,Active
        9,Grace Lee,grace@example.com,29,Miami,USA,Active
        10,Henry Wang,henry@example.com,26,Portland,USA,Active
        """;

    public class TestModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class LargeTestModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public string Status { get; set; } = "";
    }

    [Fact]
    public async Task ReadContent_ConcurrentAccess_ShouldBeThreadSafe()
    {
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var results = new ConcurrentBag<string[][]>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        var records = Csv.ReadContent(CsvData).ToArray();
                        results.Add(records);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(threadCount * iterationsPerThread, results.Count);

        foreach (var result in results)
        {
            Assert.Equal(5, result.Length);
            Assert.Equal("Item1", result[0][1]);
            Assert.Equal("500", result[4][2]);
        }
    }

    [Fact]
    public async Task CreateReader_ConcurrentAccess_ShouldCreateIndependentReaders()
    {
        const int readerCount = 50;
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, readerCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    using var reader = Csv.CreateReader(CsvData);
                    var count = 0;
                    while (reader.TryReadRecord(out var record))
                    {
                        count++;
                    }
                    results.Add(count);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(readerCount, results.Count);

        foreach (var count in results)
        {
            Assert.Equal(6, count);
        }
    }

    [Fact]
    public async Task Read_GenericConcurrentAccess_ShouldBeThreadSafe()
    {
        const int threadCount = 20;
        var results = new ConcurrentBag<TestModel[]>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    var models = Csv.Read<TestModel>(CsvData).ToArray();
                    results.Add(models);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(threadCount, results.Count);

        foreach (var models in results)
        {
            Assert.Equal(5, models.Length);
            Assert.Equal(1, models[0].ID);
            Assert.Equal("Item1", models[0].Name);
            Assert.Equal(100, models[0].Value);
        }
    }

    [Fact]
    public async Task CountRecords_ConcurrentAccess_ShouldBeConsistent()
    {
        const int threadCount = 30;
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    var count = Csv.CountRecords(LargeCsvData);
                    results.Add(count);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(threadCount, results.Count);

        foreach (var count in results)
        {
            Assert.Equal(10, count);
        }
    }

    [Fact]
    public async Task ReadAllRecords_ConcurrentAccess_WithLargeDataset_ShouldBeStable()
    {
        const int threadCount = 15;
        const int iterations = 20;
        var allResults = new ConcurrentBag<string[][]>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var records = Csv.ReadAllRecords(LargeCsvData);
                        allResults.Add(records.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(threadCount * iterations, allResults.Count);

        foreach (var records in allResults)
        {
            Assert.Equal(10, records.Length);
            Assert.Equal("John Doe", records[0][1]);
            Assert.Equal("henry@example.com", records[9][2]);
        }
    }

    [Fact]
    public async Task MixedOperations_ConcurrentAccess_ShouldNotInterfere()
    {
        const int operationsCount = 25;
        var countResults = new ConcurrentBag<int>();
        var readResults = new ConcurrentBag<LargeTestModel[]>();
        var recordResults = new ConcurrentBag<string[][]>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, operationsCount)
            .Select(i => Task.Run(() =>
            {
                try
                {
                    switch (i % 3)
                    {
                        case 0:
                            var count = Csv.CountRecords(LargeCsvData);
                            countResults.Add(count);
                            break;

                        case 1:
                            var models = Csv.Read<LargeTestModel>(LargeCsvData).ToArray();
                            readResults.Add(models);
                            break;

                        case 2:
                            var records = Csv.ReadAllRecords(LargeCsvData).ToArray();
                            recordResults.Add(records);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);

        Assert.All(countResults, count => Assert.Equal(10, count));
        Assert.All(readResults, models => Assert.Equal(10, models.Length));
        Assert.All(recordResults, records => Assert.Equal(10, records.Length));
    }

    [Fact]
    public async Task ReaderDisposal_ConcurrentAccess_ShouldNotCauseCrash()
    {
        const int readerCount = 100;
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, readerCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    using var reader = Csv.CreateReader(CsvData);

                    reader.TryReadRecord(out var record1);
                    reader.TryReadRecord(out var record2);

                    var remainingCount = 0;
                    while (reader.TryReadRecord(out var record))
                    {
                        remainingCount++;
                    }

                    Assert.Equal(4, remainingCount);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }
}