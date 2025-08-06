using HeroCsv.Utilities;

namespace HeroCsv.Models;

/// <summary>
/// Configuration for CSV parsing and writing operations
/// </summary>
/// <remarks>
/// Creates a new CsvOptions instance
/// </remarks>
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
    /// Character used to separate fields in CSV
    /// </summary>
    public char Delimiter { get; } = delimiter;

    /// <summary>
    /// Character used to quote fields containing special characters
    /// </summary>
    public char Quote { get; } = quote;

    /// <summary>
    /// Whether the first row contains column headers
    /// </summary>
    public bool HasHeader { get; } = hasHeader;

    /// <summary>
    /// Trim leading and trailing whitespace from fields
    /// </summary>
    public bool TrimWhitespace { get; } = trimWhitespace;

    /// <summary>
    /// Skip empty fields during object mapping
    /// </summary>
    public bool SkipEmptyFields { get; } = skipEmptyFields;

    /// <summary>
    /// Line terminator for CSV writing
    /// </summary>
    public string NewLine { get; } = newLine ?? Environment.NewLine;

    /// <summary>
    /// String pool for memory optimization with repeated values
    /// </summary>
    public StringPool? StringPool { get; } = stringPool;

    /// <summary>
    /// Default CSV configuration
    /// </summary>
    public static CsvOptions Default => new(',', '"', true);
}