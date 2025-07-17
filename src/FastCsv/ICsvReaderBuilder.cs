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
    /// Configures whether to skip empty fields during mapping
    /// </summary>
    /// <param name="skipEmpty">True to skip empty fields</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithSkipEmptyFields(bool skipEmpty = true);

    /// <summary>
    /// Configures whether to trim whitespace from field values
    /// </summary>
    /// <param name="trimWhitespace">True to trim whitespace</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithTrimWhitespace(bool trimWhitespace = true);

    /// <summary>
    /// Configures complete CSV options at once
    /// </summary>
    /// <param name="options">Complete CSV options</param>
    /// <returns>Configuration builder for additional options</returns>
    ICsvReaderBuilder WithOptions(CsvOptions options);

    /// <summary>
    /// Parses the configured CSV data and returns rows as string arrays
    /// </summary>
    /// <returns>Each CSV row as an array of field values</returns>
    IEnumerable<string[]> Read();



    /// <summary>
    /// Parses CSV data and maps each record to the specified type using auto mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <returns>Enumerable of mapped objects</returns>
    IEnumerable<T> Read<T>() where T : class, new();

    /// <summary>
    /// Parses CSV data and maps each record to the specified type using manual mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <returns>Enumerable of mapped objects</returns>
    IEnumerable<T> Read<T>(CsvMapping<T> mapping) where T : class, new();
}