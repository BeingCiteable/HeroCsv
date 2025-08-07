using System.Text;
using HeroCsv.Core;
using HeroCsv.Models;
using HeroCsv.Parsing;
using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Unit.Performance;

public class StringPoolTest
{
    [Fact]
    public void StringPool_DeduplicatesIdenticalValues()
    {
        // Arrange
        var pool = new StringPool();
        var csv = "Status,Priority,Status\nActive,High,Active\nInactive,Low,Inactive\nActive,High,Active";
        var options = new CsvOptions(stringPool: pool);

        // Act
        using var reader = Csv.CreateReader(csv, options);
        var records = reader.ReadAllRecords();

        // Assert
        Assert.Equal(3, records.Count);

        // Verify string deduplication - same "Active" instance
        Assert.Same(records[0][0], records[0][2]); // First row: Status columns should be same instance
        Assert.Same(records[0][0], records[2][0]); // "Active" in row 1 and row 3 should be same instance

        // Verify pool contains unique values
        Assert.True(pool.Count <= 5); // Should have at most: Active, Inactive, High, Low, Status
    }

    [Fact]
    public void DirectRows_WithStringPool_DeduplicatesValues()
    {
        // Arrange
        var pool = new StringPool();
        var csv = "Color,Size,Color\nRed,Large,Red\nBlue,Small,Blue\nRed,Large,Red";
        var options = new CsvOptions(stringPool: pool);

        // Act
        using var reader = Csv.CreateReader(csv, options);
        var fastReader = (HeroCsvReader)reader;

        var rows = new List<string[]>();
        foreach (var row in fastReader.EnumerateRows())
        {
            var fields = new string[3];
            for (int i = 0; i < 3; i++)
            {
                fields[i] = row.GetString(i);
            }
            rows.Add(fields);
        }

        // Assert
        Assert.Equal(3, rows.Count);

        // Verify deduplication across rows and columns
        Assert.Same(rows[0][0], rows[0][2]); // "Red" in same row
        Assert.Same(rows[0][0], rows[2][0]); // "Red" across rows
        Assert.Same(rows[0][1], rows[2][1]); // "Large" across rows
    }
}
