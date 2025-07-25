namespace FastCsv;

/// <summary>
/// Internal interface for zero-allocation access
/// </summary>
internal interface IInternalCsvReader
{
    /// <summary>
    /// Try to get the next line position in buffer
    /// </summary>
    bool TryGetNextLine(out int lineStart, out int lineLength, out int lineNumber);
    
    /// <summary>
    /// Get the entire buffer
    /// </summary>
    ReadOnlySpan<char> GetBuffer();
    
    /// <summary>
    /// Get the CSV options
    /// </summary>
    CsvOptions Options { get; }
}