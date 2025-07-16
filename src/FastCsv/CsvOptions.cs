namespace FastCsv;

/// <summary>
/// Configuration for CSV parsing and writing operations
/// </summary>
/// <remarks>
/// Creates a new CsvOptions instance with the specified settings
/// </remarks>
public readonly struct CsvOptions(
    char delimiter = ',',
    char quote = '"',
    bool hasHeader = true,
    bool trimWhitespace = false,
    string? newLine = null)
{
    /// <summary>
    /// The delimiter character (e.g., comma, semicolon, tab)
    /// </summary>
    public readonly char Delimiter = delimiter;

    /// <summary>
    /// The quote character for escaping fields
    /// </summary>
    public readonly char Quote = quote;

    /// <summary>
    /// Whether the CSV has a header row
    /// </summary>
    public readonly bool HasHeader = hasHeader;

    /// <summary>
    /// Whether to trim whitespace from fields
    /// </summary>
    public readonly bool TrimWhitespace = trimWhitespace;

    /// <summary>
    /// The newline string to use when writing
    /// </summary>
    public readonly string NewLine = newLine ?? Environment.NewLine;

    /// <summary>
    /// Default CSV options (comma-separated, quoted, with header)
    /// </summary>
    public static CsvOptions Default => new();
}
