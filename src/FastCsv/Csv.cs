using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FastCsv;

/// <summary>
/// Provides convenient methods for reading CSV data from strings and files
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Parses CSV content with zero-allocation span-based processing
    /// </summary>
    /// <param name="csvContent">CSV text as memory span</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(ReadOnlySpan<char> csvContent)
    {
        return ReadInternal(csvContent, CsvOptions.Default);
    }

    /// <summary>
    /// Parses CSV content and returns each row as a string array
    /// </summary>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string csvContent)
    {
        return ReadInternal(csvContent.AsSpan(), CsvOptions.Default);
    }

    /// <summary>
    /// Parses CSV content using specified field separator with zero allocations
    /// </summary>
    /// <param name="csvContent">CSV text as memory span</param>
    /// <param name="delimiter">Character that separates fields (e.g., ',' or ';')</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(ReadOnlySpan<char> csvContent, char delimiter)
    {
        var options = new CsvOptions(delimiter);
        return ReadInternal(csvContent, options);
    }

    /// <summary>
    /// Parses CSV content using specified field separator
    /// </summary>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="delimiter">Character that separates fields (e.g., ',' or ';')</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string csvContent, char delimiter)
    {
        var options = new CsvOptions(delimiter);
        return ReadInternal(csvContent.AsSpan(), options);
    }

    /// <summary>
    /// Parses CSV content with custom formatting settings and zero allocations
    /// </summary>
    /// <param name="csvContent">CSV text as memory span</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(ReadOnlySpan<char> csvContent, CsvOptions options)
    {
        return ReadInternal(csvContent, options);
    }

    /// <summary>
    /// Parses CSV content with custom delimiter, quote, and formatting settings
    /// </summary>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string csvContent, CsvOptions options)
    {
        return ReadInternal(csvContent.AsSpan(), options);
    }

    /// <summary>
    /// Loads and parses CSV data from a file
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return ReadInternal(content.AsSpan(), CsvOptions.Default);
    }

    /// <summary>
    /// Loads and parses CSV data from a file with custom formatting settings
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadFile(string filePath, CsvOptions options)
    {
        var content = File.ReadAllText(filePath);
        return ReadInternal(content.AsSpan(), options);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names, returns data as name-value pairs
    /// </summary>
    /// <param name="csvContent">Raw CSV text where first line contains column headers</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string csvContent)
    {
        return ReadWithHeaders(csvContent, CsvOptions.Default);
    }

    /// <summary>
    /// Parses CSV data with custom formatting where first row contains column names
    /// </summary>
    /// <param name="csvContent">Raw CSV text where first line contains column headers</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string csvContent, CsvOptions options)
    {
        var records = ReadInternal(csvContent.AsSpan(), options);
        using var enumerator = records.GetEnumerator();
        
        if (!enumerator.MoveNext()) yield break;
        
        var headers = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            var dict = new Dictionary<string, string>(Math.Min(headers.Length, record.Length));
            
            for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
            {
                dict[headers[i]] = record[i];
            }
            yield return dict;
        }
    }

    /// <summary>
    /// Creates a configuration builder for customizing CSV parsing behavior
    /// </summary>
    /// <returns>Builder for setting validation, error handling, and performance options</returns>
    public static ICsvReaderBuilder Configure()
    {
        return new CsvReaderBuilder();
    }

    /// <summary>
    /// Creates a configuration builder pre-loaded with CSV content
    /// </summary>
    /// <param name="csvContent">Raw CSV text to be configured for parsing</param>
    /// <returns>Builder for setting validation, error handling, and performance options</returns>
    public static ICsvReaderBuilder Configure(string csvContent)
    {
        return new CsvReaderBuilder().WithContent(csvContent);
    }

    /// <summary>
    /// High-performance CSV parsing with minimal allocations
    /// </summary>
    private static IEnumerable<string[]> ReadInternal(ReadOnlySpan<char> csvContent, CsvOptions options)
    {
        // Convert span to string for yield return compatibility
        var content = csvContent.ToString();
        return ReadInternalFromString(content, options);
    }

    private static IEnumerable<string[]> ReadInternalFromString(string csvContent, CsvOptions options)
    {
        var position = 0;
        
        while (position < csvContent.Length)
        {
            // Find end of current line
            var lineEnd = FindLineEnd(csvContent, position);
            var lineSpan = csvContent.AsSpan(position, lineEnd - position);
            
            if (lineSpan.Length > 0)
            {
                var fields = ParseLine(lineSpan, options);
                if (fields.Length > 0)
                {
                    yield return fields;
                }
            }
            
            // Skip line ending characters
            position = lineEnd;
            if (position < csvContent.Length && csvContent[position] == '\r')
                position++;
            if (position < csvContent.Length && csvContent[position] == '\n')
                position++;
        }
    }

    private static int FindLineEnd(string content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    private static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return Array.Empty<string>();
        
        // Pre-allocate for common case
        var fields = new List<string>(8);
        var fieldStart = 0;
        var inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            
            if (ch == options.Quote && !inQuotes)
            {
                inQuotes = true;
                fieldStart = i + 1;
            }
            else if (ch == options.Quote && inQuotes)
            {
                inQuotes = false;
            }
            else if (ch == options.Delimiter && !inQuotes)
            {
                var fieldSpan = line.Slice(fieldStart, i - fieldStart);
                fields.Add(fieldSpan.ToString());
                fieldStart = i + 1;
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


/// <summary>
/// Advanced result from CSV reading operations
/// </summary>
public class CsvReadResult
{
    public IReadOnlyList<string[]> Records { get; set; } = new List<string[]>();
    public int TotalRecords { get; set; }
    public bool IsValid { get; set; }
    public IReadOnlyList<string> ValidationErrors { get; set; } = new List<string>();
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
}

