namespace FastCsv;

/// <summary>
/// Configures CSV parsing options through a fluent interface
/// </summary>
public partial interface ICsvReaderBuilder
{
    /// <summary>
    /// Specifies the raw CSV text to parse
    /// </summary>
    /// <param name="content">CSV data as a string</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithContent(string content);

    /// <summary>
    /// Specifies the file containing CSV data to parse
    /// </summary>
    /// <param name="filePath">Full or relative path to CSV file</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithFile(string filePath);

    /// <summary>
    /// Sets the character used to separate fields
    /// </summary>
    /// <param name="delimiter">Character separating fields (e.g., ',' or ';' or '\t')</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithDelimiter(char delimiter);

    /// <summary>
    /// Sets the character used to wrap field values containing special characters
    /// </summary>
    /// <param name="quote">Character for escaping fields (typically '"')</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithQuote(char quote);

    /// <summary>
    /// Indicates whether the first row contains column names
    /// </summary>
    /// <param name="hasHeader">True if first row contains column headers</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithHeaders(bool hasHeader = true);

    /// <summary>
    /// Enables validation of CSV structure and data integrity
    /// </summary>
    /// <param name="validate">True to check for malformed records and quote errors</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithValidation(bool validate = true);

    /// <summary>
    /// Enables collection of parsing errors for later inspection
    /// </summary>
    /// <param name="trackErrors">True to gather error details during parsing</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithErrorTracking(bool trackErrors = true);

    /// <summary>
    /// Parses the configured CSV data and returns rows as string arrays
    /// </summary>
    /// <returns>Each CSV row as an array of field values</returns>
    IEnumerable<string[]> Read();

    /// <summary>
    /// Parses CSV data treating first row as column names, returns name-value pairs
    /// </summary>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    IEnumerable<Dictionary<string, string>> ReadWithHeaders();

    /// <summary>
    /// Parses CSV data with comprehensive result including validation and performance metrics
    /// </summary>
    /// <returns>Detailed parsing result with data, errors, timing, and statistics</returns>
    CsvReadResult ReadAdvanced();
}