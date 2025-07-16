using System;

namespace FastCsv.Records;

/// <summary>
/// Handler responsible for record boundary detection and extraction
/// Core responsibility: Find and extract complete records from CSV data
/// </summary>
public partial interface IRecordHandler
{
    /// <summary>
    /// Find the end position of a record starting at the given position
    /// </summary>
    int FindRecordEnd(ReadOnlySpan<char> data, int startPosition, CsvOptions options);
    
    /// <summary>
    /// Extract a complete record from the data
    /// </summary>
    ReadOnlySpan<char> ExtractRecord(ReadOnlySpan<char> data, int startPosition, CsvOptions options);
    
    /// <summary>
    /// Skip to the next record position
    /// </summary>
    int SkipToNextRecord(ReadOnlySpan<char> data, int currentPosition, CsvOptions options);
    
    /// <summary>
    /// Check if we're at the end of the data
    /// </summary>
    bool IsAtEnd(ReadOnlySpan<char> data, int position);
}