using System.Text;

namespace FastCsv;

/// <summary>
/// Provides convenient methods for reading CSV data from strings and files
/// </summary>
public static partial class Csv
{

    /// <summary>
    /// Counts CSV records without allocating strings for maximum performance
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Number of records found</returns>
    public static int CountRecords(ReadOnlySpan<char> content, CsvOptions options = default)
    {
        using var reader = CreateReader(content.ToString(), options);
        return reader.CountRecords();
    }

    /// <summary>
    /// Parses CSV content with default options
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string content)
    {
        return Read(content, CsvOptions.Default);
    }

    /// <summary>
    /// Parses CSV content with custom delimiter
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="delimiter">Field separator character</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string content, char delimiter)
    {
        var options = new CsvOptions(delimiter);
        return Read(content, options);
    }

    /// <summary>
    /// Parses CSV content with custom options
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string content, CsvOptions options)
    {
        var reader = CreateReader(content, options);
        return reader.GetRecords();
    }

    /// <summary>
    /// Loads and parses CSV data from a file with custom formatting settings
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadFile(string filePath, CsvOptions options = default)
    {
        var content = File.ReadAllText(filePath);
        return Read(content, options);
    }

    /// <summary>
    /// Parses CSV content and returns all records as a read-only list for better performance
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Read-only list of all parsed records</returns>
    public static IReadOnlyList<string[]> ReadAllRecords(ReadOnlySpan<char> content, CsvOptions options = default)
    {
        using var reader = CreateReader(content.ToString(), options);
        return reader.ReadAllRecords();
    }

    /// <summary>
    /// Creates a configuration builder for customizing CSV parsing behavior
    /// </summary>
    /// <returns>Builder for setting validation, error handling, and performance options</returns>
    public static ICsvReaderBuilder Configure() => new CsvReaderBuilder();

    /// <summary>
    /// Creates a configuration builder pre-loaded with CSV content
    /// </summary>
    /// <param name="content">Raw CSV text to be configured for parsing</param>
    /// <returns>Builder for setting validation, error handling, and performance options</returns>
    public static ICsvReaderBuilder Configure(string content)
    {
        return new CsvReaderBuilder().WithContent(content);
    }

    /// <summary>
    /// Creates a CSV reader with the specified content and options
    /// </summary>
    /// <param name="content">CSV content to read</param>
    /// <param name="options">Parsing options</param>
    /// <returns>Configured CSV reader</returns>
    public static ICsvReader CreateReader(string content, CsvOptions options)
    {
        return new FastCsvReader(content, options);
    }

    /// <summary>
    /// Creates a CSV reader with the specified content and default options
    /// </summary>
    /// <param name="content">CSV content to read</param>
    /// <returns>Configured CSV reader</returns>
    public static ICsvReader CreateReader(string content)
    {
        return CreateReader(content, CsvOptions.Default);
    }

    /// <summary>
    /// Creates a CSV reader from a stream
    /// </summary>
    /// <param name="stream">Stream containing CSV data</param>
    /// <param name="options">Parsing options</param>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="leaveOpen">Whether to leave the stream open when disposing the reader</param>
    /// <returns>Configured CSV reader</returns>
    public static ICsvReader CreateReader(Stream stream, CsvOptions options = default, Encoding? encoding = null, bool leaveOpen = false)
    {
        return new FastCsvReader(stream, options, encoding, leaveOpen);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using auto mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="content">Raw CSV text to parse</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string content) where T : class, new()
    {
        return Read<T>(content, CsvOptions.Default);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using auto mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string content, CsvOptions options) where T : class, new()
    {
        var mapper = new CsvMapper<T>(options);
        return ReadWithMapper(content, options, mapper);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using manual mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string content, CsvMapping<T> mapping) where T : class, new()
    {
        var mapper = new CsvMapper<T>(mapping);
        return ReadWithMapper(content, mapping.Options, mapper);
    }

    /// <summary>
    /// Reads CSV file and maps each record to the specified type using auto mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadFile<T>(string filePath) where T : class, new()
    {
        return ReadFile<T>(filePath, CsvOptions.Default);
    }

    /// <summary>
    /// Reads CSV file and maps each record to the specified type using auto mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadFile<T>(string filePath, CsvOptions options) where T : class, new()
    {
        var content = File.ReadAllText(filePath);
        return Read<T>(content, options);
    }

    /// <summary>
    /// Reads CSV file and maps each record to the specified type using manual mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadFile<T>(string filePath, CsvMapping<T> mapping) where T : class, new()
    {
        var content = File.ReadAllText(filePath);
        return Read(content, mapping);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using mixed mapping (auto + manual overrides)
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadMixed<T>(string content, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        return ReadMixed(content, CsvOptions.Default, configureMapping);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using mixed mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadMixed<T>(string content, CsvOptions options, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        var mapping = CsvMapping<T>.CreateMixed();
        mapping.Options = options;
        configureMapping(mapping);
        return Read<T>(content, mapping);
    }

    /// <summary>
    /// Reads CSV file and maps each record to the specified type using mixed mapping (auto + manual overrides)
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadFileMixed<T>(string filePath, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        var content = File.ReadAllText(filePath);
        return ReadMixed<T>(content, configureMapping);
    }

    /// <summary>
    /// Reads CSV data from a stream and returns each row as a string array
    /// </summary>
    /// <param name="stream">Stream containing CSV data</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadStream(Stream stream)
    {
        return ReadStream(stream, CsvOptions.Default);
    }

    /// <summary>
    /// Reads CSV data from a stream with custom options
    /// </summary>
    /// <param name="stream">Stream containing CSV data</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadStream(Stream stream, CsvOptions options)
    {
        using var streamReader = new StreamReader(stream);
        var content = streamReader.ReadToEnd();
        var reader = CreateReader(content, options);
        return reader.GetRecords();
    }

    /// <summary>
    /// Reads CSV data from a stream and maps each record to the specified type using auto mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="stream">Stream containing CSV data</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadStream<T>(Stream stream) where T : class, new()
    {
        return ReadStream<T>(stream, CsvOptions.Default);
    }

    /// <summary>
    /// Reads CSV data from a stream and maps each record to the specified type using auto mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="stream">Stream containing CSV data</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadStream<T>(Stream stream, CsvOptions options) where T : class, new()
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return Read<T>(content, options);
    }

    /// <summary>
    /// Reads CSV data from a stream and maps each record to the specified type using manual mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="stream">Stream containing CSV data</param>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadStream<T>(Stream stream, CsvMapping<T> mapping) where T : class, new()
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return Read<T>(content, mapping);
    }

    /// <summary>
    /// Reads CSV data from a stream and maps each record to the specified type using mixed mapping (auto + manual overrides)
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="stream">Stream containing CSV data</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadStreamMixed<T>(Stream stream, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return ReadMixed<T>(content, configureMapping);
    }


    /// <summary>
    /// Internal method for reading with a configured mapper
    /// </summary>
    private static IEnumerable<T> ReadWithMapper<T>(string content, CsvOptions options, CsvMapper<T> mapper) where T : class, new()
    {
        var reader = CreateReader(content, options);
        var records = reader.GetRecords();
        using var enumerator = records.GetEnumerator();

        // Map each record
        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            yield return mapper.MapRecord(record);
        }
    }


}