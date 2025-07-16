#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;

namespace FastCsv;

/// <summary>
/// Advanced parsing features for Csv
/// </summary>
public static partial class Csv
{
    private static readonly SearchValues<char> CommonDelimiters = SearchValues.Create(",;\t|");
    private static readonly SearchValues<char> QuoteChars = SearchValues.Create("\"'");
    private static readonly SearchValues<char> LineEndings = SearchValues.Create("\r\n");
    
    /// <summary>
    /// Auto-detect CSV format and read
    /// </summary>
    /// <param name="csvContent">CSV content as string</param>
    /// <returns>Enumerable of string arrays representing records</returns>
    public static IEnumerable<string[]> ReadAutoDetect(string csvContent)
    {
        var options = AutoDetectFormat(csvContent.AsSpan());
        return ReadInternal(csvContent.AsSpan(), options);
    }

    /// <summary>
    /// Auto-detect CSV format from file and read
    /// </summary>
    /// <param name="filePath">Path to CSV file</param>
    /// <returns>Enumerable of string arrays representing records</returns>
    public static IEnumerable<string[]> ReadFileAutoDetect(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var options = AutoDetectFormat(content.AsSpan());
        return ReadInternal(content.AsSpan(), options);
    }
    
    /// <summary>
    /// Auto-detect CSV format and read with zero-allocation span processing
    /// </summary>
    /// <param name="csvContent">CSV content as memory span</param>
    /// <returns>Enumerable of string arrays representing records</returns>
    public static IEnumerable<string[]> ReadAutoDetect(ReadOnlySpan<char> csvContent)
    {
        var options = AutoDetectFormat(csvContent);
        return ReadInternal(csvContent, options);
    }

    private static CsvOptions AutoDetectFormat(ReadOnlySpan<char> content)
    {
        var sampleSize = Math.Min(content.Length, 2000);
        var sample = content.Slice(0, sampleSize);
        
        var commaCount = 0;
        var semicolonCount = 0;
        var tabCount = 0;
        var hasQuotes = false;
        
        var position = 0;
        while (position < sample.Length)
        {
            var nextDelimiter = sample.Slice(position).IndexOfAny(CommonDelimiters);
            if (nextDelimiter == -1) break;
            
            var actualPos = position + nextDelimiter;
            switch (sample[actualPos])
            {
                case ',': commaCount++; break;
                case ';': semicolonCount++; break;
                case '\t': tabCount++; break;
                case '|': break; // Could add pipe support
            }
            position = actualPos + 1;
        }
        
        // Check for quotes
        hasQuotes = sample.IndexOfAny(QuoteChars) != -1;
        
        var delimiter = ',';
        if (semicolonCount > commaCount && semicolonCount > tabCount)
            delimiter = ';';
        else if (tabCount > commaCount && tabCount > semicolonCount)
            delimiter = '\t';
        
        return new CsvOptions(delimiter, hasQuotes ? '"' : '"', true);
    }
    
    /// <summary>
    /// Optimized line ending detection using SearchValues
    /// </summary>
    /// <param name="content">Content to search</param>
    /// <param name="start">Starting position</param>
    /// <returns>Position of line ending</returns>
    private static int FindLineEndOptimized(ReadOnlySpan<char> content, int start)
    {
        var searchSpan = content.Slice(start);
        var lineEndIndex = searchSpan.IndexOfAny(LineEndings);
        
        if (lineEndIndex == -1)
            return content.Length;
            
        var actualPos = start + lineEndIndex;
        
        // Handle \r\n sequence
        if (content[actualPos] == '\r' && actualPos + 1 < content.Length && content[actualPos + 1] == '\n')
            return actualPos;
            
        return actualPos;
    }
    
    /// <summary>
    /// High-performance field parsing with SearchValues optimization
    /// </summary>
    /// <param name="line">Line to parse</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Parsed field array</returns>
    private static string[] ParseLineOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return Array.Empty<string>();
        
        var delimiterSearch = SearchValues.Create(stackalloc char[] { options.Delimiter });
        var quoteSearch = SearchValues.Create(stackalloc char[] { options.Quote });
        
        var fields = new List<string>();
        var fieldStart = 0;
        var inQuotes = false;
        
        var position = 0;
        while (position < line.Length)
        {
            int nextSpecial;
            if (inQuotes)
            {
                nextSpecial = line.Slice(position).IndexOfAny(quoteSearch);
                if (nextSpecial == -1) break;
                nextSpecial += position;
                
                if (line[nextSpecial] == options.Quote)
                {
                    inQuotes = false;
                    position = nextSpecial + 1;
                }
            }
            else
            {
                var delimiterPos = line.Slice(position).IndexOfAny(delimiterSearch);
                var quotePos = line.Slice(position).IndexOfAny(quoteSearch);
                
                if (delimiterPos == -1 && quotePos == -1)
                {
                    // No more special characters
                    break;
                }
                
                if (quotePos != -1 && (delimiterPos == -1 || quotePos < delimiterPos))
                {
                    // Quote comes first
                    inQuotes = true;
                    if (fieldStart == position + quotePos)
                        fieldStart = position + quotePos + 1;
                    position += quotePos + 1;
                }
                else if (delimiterPos != -1)
                {
                    // Delimiter comes first
                    var fieldSpan = line.Slice(fieldStart, position + delimiterPos - fieldStart);
                    fields.Add(fieldSpan.ToString());
                    fieldStart = position + delimiterPos + 1;
                    position = fieldStart;
                }
                else
                {
                    position++;
                }
            }
        }
        
        // Add final field
        if (fieldStart <= line.Length)
        {
            var fieldSpan = line.Slice(fieldStart);
            fields.Add(fieldSpan.ToString());
        }
        
        return fields.ToArray();
    }
}
#endif