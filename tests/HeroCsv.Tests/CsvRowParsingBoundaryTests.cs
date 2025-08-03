using System;
using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Parsing;
using Xunit;

namespace HeroCsv.Tests;

/// <summary>
/// Tests boundary conditions for CsvRow parsing and buffer operations
/// </summary>
public class CsvRowParsingBoundaryTests
{
    [Fact]
    public void CsvRow_ParseWholeBuffer_Comprehensive()
    {
        // Test with header - ParseWholeBuffer skips header when hasHeader: true
        var buffer = "Header1,Header2\nValue1,Value2\nValue3,Value4".AsSpan();
        var options = CsvOptions.Default; // hasHeader: true by default
        
        var rows = CsvParser.ParseWholeBuffer(buffer, options);
        var rowCount = 0;
        
        foreach (var row in rows)
        {
            rowCount++;
            if (rowCount == 1)
            {
                // First row should be Value1,Value2 (header is skipped)
                Assert.Equal("Value1", row[0].ToString());
                Assert.Equal("Value2", row[1].ToString());
            }
            else if (rowCount == 2)
            {
                Assert.Equal("Value3", row[0].ToString());
                Assert.Equal("Value4", row[1].ToString());
            }
            
            // Test field enumerator
            var enumerator = row.GetFieldEnumerator();
            var fieldCount = 0;
            while (enumerator.TryGetNextField(out _))
            {
                fieldCount++;
            }
            Assert.Equal(2, fieldCount);
        }
        
        Assert.Equal(2, rowCount); // 2 data rows (header is skipped)
    }

    [Fact]
    public void CsvRow_EmptyBuffer()
    {
        var buffer = "".AsSpan();
        var options = CsvOptions.Default;
        
        var rows = CsvParser.ParseWholeBuffer(buffer, options);
        var count = 0;
        
        foreach (var _ in rows)
        {
            count++;
        }
        
        Assert.Equal(0, count);
    }
}