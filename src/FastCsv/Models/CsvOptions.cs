using FastCsv.Utilities;

namespace FastCsv.Models;

/// <summary>
/// Configuration for CSV parsing and writing operations
/// </summary>
public readonly struct CsvOptions(
    char delimiter = ',',
    char quote = '"',
    bool hasHeader = true,
    bool trimWhitespace = false,
    bool skipEmptyFields = false,
    string? newLine = null,
    StringPool? stringPool = null)
{
    /// <summary>
    /// Field delimiter character
    /// </summary>
    public readonly char Delimiter = delimiter;

    /// <summary>
    /// Quote character for field escaping
    /// </summary>
    public readonly char Quote = quote;

    /// <summary>
    /// Indicates if first row contains headers
    /// </summary>
    public readonly bool HasHeader = hasHeader;

    /// <summary>
    /// Trim leading and trailing whitespace from fields
    /// </summary>
    public readonly bool TrimWhitespace = trimWhitespace;

    /// <summary>
    /// Skip empty fields during object mapping
    /// </summary>
    public readonly bool SkipEmptyFields = skipEmptyFields;

    /// <summary>
    /// Line terminator for CSV writing
    /// </summary>
    public readonly string NewLine = newLine ?? Environment.NewLine;

    /// <summary>
    /// String pool for memory optimization with repeated values
    /// </summary>
    public readonly StringPool? StringPool = stringPool;

    /// <summary>
    /// Default CSV configuration
    /// </summary>
    public static CsvOptions Default => new(',', '"', true);
}