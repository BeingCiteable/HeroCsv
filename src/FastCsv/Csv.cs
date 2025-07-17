using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Provides convenient methods for reading CSV data from strings and files
/// </summary>
public static partial class Csv
{
    /// <summary>
    /// Parses CSV content into pre-allocated destination array for maximum performance
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="destination">Pre-allocated span to fill with parsed records</param>
    /// <returns>Number of records parsed</returns>
    public static int Read(ReadOnlySpan<char> content, Span<string[]> destination)
    {
        return ReadIntoArray(content, destination, CsvOptions.Default);
    }

    /// <summary>
    /// High-performance CSV parsing with pre-allocated destination array
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="destination">Pre-allocated span to fill with parsed records</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Number of records parsed</returns>
    public static int ReadIntoArray(ReadOnlySpan<char> content, Span<string[]> destination, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        var recordCount = 0;
        var position = 0;

        while (position < content.Length && recordCount < destination.Length)
        {
            var lineEnd = CsvSpanEnumerator.FindLineEnd(content, position);
            var lineSpan = content.Slice(position, lineEnd - position);

            if (lineSpan.Length > 0)
            {
                var fields = CsvSpanEnumerator.ParseLine(lineSpan, options);
                if (fields.Length > 0)
                {
                    destination[recordCount++] = fields;
                }
            }

            position = CsvSpanEnumerator.SkipLineEnding(content, lineEnd);
        }

        return recordCount;
    }

    /// <summary>
    /// Counts CSV records without allocating strings for maximum performance
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Number of records found</returns>
    public static int CountRecords(ReadOnlySpan<char> content, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        var count = 0;
        var position = 0;

        while (position < content.Length)
        {
            var lineEnd = CsvSpanEnumerator.FindLineEnd(content, position);
            var lineSpan = content.Slice(position, lineEnd - position);

            if (lineSpan.Length > 0)
            {
                count++;
            }

            position = CsvSpanEnumerator.SkipLineEnding(content, lineEnd);
        }

        return count;
    }

    /// <summary>
    /// Parses CSV content and returns each row as a string array
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string content)
    {
        return new CsvMemoryEnumerable(content.AsMemory(), CsvOptions.Default);
    }

    /// <summary>
    /// Parses CSV content with custom options
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string content, CsvOptions options)
    {
        return new CsvMemoryEnumerable(content.AsMemory(), options);
    }

    /// <summary>
    /// Loads and parses CSV data from a file with custom formatting settings
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> ReadFile(string filePath, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        var content = File.ReadAllText(filePath);
        return new CsvMemoryEnumerable(content.AsMemory(), options);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names, returns data as name-value pairs
    /// </summary>
    /// <param name="content">Raw CSV text where first line contains column headers</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string content, CsvOptions options = default)
    {
        return ReadWithHeaders(content, options, DuplicateHeaderHandling.ThrowException);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names with specified duplicate header handling
    /// </summary>
    /// <param name="content">Raw CSV text where first line contains column headers</param>
    /// <param name="duplicateHandling">Strategy for handling duplicate column headers</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string content, DuplicateHeaderHandling duplicateHandling)
    {
        return ReadWithHeaders(content, CsvOptions.Default, duplicateHandling);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names with specified duplicate header handling
    /// </summary>
    /// <param name="content">Raw CSV text where first line contains column headers</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <param name="duplicateHandling">Strategy for handling duplicate column headers</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string content, CsvOptions options, DuplicateHeaderHandling duplicateHandling)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        var records = new CsvMemoryEnumerable(content.AsMemory(), options);
        using var enumerator = records.GetEnumerator();
        
        if (!enumerator.MoveNext()) yield break;
        
        var headers = enumerator.Current;
        var processedHeaders = ProcessHeaders(headers, duplicateHandling);
        if (processedHeaders == null) yield break;

        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            var dict = CreateRecordDictionary(processedHeaders, record, duplicateHandling);
            if (dict != null) yield return dict;
        }
    }

    /// <summary>
    /// Parses CSV content and returns all records as a list for better performance
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>List of all parsed records</returns>
    public static List<string[]> ReadAllRecords(ReadOnlySpan<char> content, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        // For span input, we need to convert to string for enumerable
        // This is the only allocation, but it's necessary for the IEnumerable pattern
        var contentString = content.ToString();
        var records = new CsvMemoryEnumerable(contentString.AsMemory(), options);
        return records.ToList();
    }

    /// <summary>
    /// Parses CSV with headers and returns all records as dictionaries
    /// </summary>
    /// <param name="content">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <param name="duplicateHandling">Strategy for handling duplicate headers</param>
    /// <returns>List of all records as dictionaries</returns>
    public static List<Dictionary<string, string>> ReadAllWithHeaders(ReadOnlySpan<char> content, CsvOptions options = default, DuplicateHeaderHandling duplicateHandling = DuplicateHeaderHandling.ThrowException)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        // For span input, we need to convert to string for enumerable
        var contentString = content.ToString();
        var records = new CsvMemoryEnumerable(contentString.AsMemory(), options);
        using var enumerator = records.GetEnumerator();
        
        if (!enumerator.MoveNext()) return new List<Dictionary<string, string>>();
        
        var headers = enumerator.Current;
        var processedHeaders = ProcessHeaders(headers, duplicateHandling);
        if (processedHeaders == null) return new List<Dictionary<string, string>>();

        var result = new List<Dictionary<string, string>>();
        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            var dict = CreateRecordDictionary(processedHeaders, record, duplicateHandling);
            if (dict != null) result.Add(dict);
        }

        return result;
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
        return new FastCsvReader(content, CsvOptions.Default);
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
        return Read<T>(content, mapping);
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
        return ReadMixed<T>(content, CsvOptions.Default, configureMapping);
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
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return ReadInternal(content.AsMemory(), options);
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
    /// Reads CSV content with detailed parsing results including configurable metrics and statistics
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <param name="detailsOptions">Configuration for what details to collect during parsing</param>
    /// <returns>Detailed parsing result based on configured options</returns>
    public static CsvReadResult ReadWithDetails(string content, CsvReadDetailsOptions detailsOptions)
    {
        return Configure(content).ReadWithDetails(detailsOptions);
    }

    /// <summary>
    /// Reads CSV content with detailed parsing results using default detail options
    /// </summary>
    /// <param name="content">Raw CSV text to parse</param>
    /// <returns>Detailed parsing result with default metrics</returns>
    public static CsvReadResult ReadWithDetails(string content)
    {
        return Configure(content).ReadWithDetails();
    }

    /// <summary>
    /// Reads CSV file with detailed parsing results including configurable metrics and statistics
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <param name="detailsOptions">Configuration for what details to collect during parsing</param>
    /// <returns>Detailed parsing result based on configured options</returns>
    public static CsvReadResult ReadFileWithDetails(string filePath, CsvReadDetailsOptions detailsOptions)
    {
        return Configure().WithFile(filePath).ReadWithDetails(detailsOptions);
    }

    /// <summary>
    /// Reads CSV file with detailed parsing results using default detail options
    /// </summary>
    /// <param name="filePath">Full or relative path to the CSV file</param>
    /// <returns>Detailed parsing result with default metrics</returns>
    public static CsvReadResult ReadFileWithDetails(string filePath)
    {
        return Configure().WithFile(filePath).ReadWithDetails();
    }

    /// <summary>
    /// Internal method for reading with a configured mapper
    /// </summary>
    private static IEnumerable<T> ReadWithMapper<T>(string content, CsvOptions options, CsvMapper<T> mapper) where T : class, new()
    {
        var records = new CsvMemoryEnumerable(content.AsMemory(), options);
        using var enumerator = records.GetEnumerator();

        // Handle headers if present
        if (options.HasHeader && enumerator.MoveNext())
        {
            var headers = enumerator.Current;
            mapper.SetHeaders(headers);
        }

        // Map each record
        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            yield return mapper.MapRecord(record);
        }
    }

    /// <summary>
    /// High-performance CSV parsing with minimal allocations
    /// </summary>
    internal static IEnumerable<string[]> ReadInternal(ReadOnlyMemory<char> content, CsvOptions options)
    {
        return new CsvMemoryEnumerable(content, options);
    }

    /// <summary>
    /// Process headers according to duplicate handling strategy
    /// </summary>
    private static string[]? ProcessHeaders(string[] headers, DuplicateHeaderHandling duplicateHandling)
    {
        if (duplicateHandling == DuplicateHeaderHandling.ThrowException)
        {
            var seen = new HashSet<string>();
            foreach (var header in headers)
            {
                if (!seen.Add(header))
                {
                    throw new InvalidOperationException($"Duplicate header found: '{header}'. Use DuplicateHeaderHandling parameter to specify how to handle duplicates.");
                }
            }
            return headers;
        }

        if (duplicateHandling == DuplicateHeaderHandling.MakeUnique)
        {
            var result = new string[headers.Length];
            var counts = new Dictionary<string, int>();
            
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                if (counts.TryGetValue(header, out var count))
                {
                    counts[header] = count + 1;
                    result[i] = $"{header}_{count + 1}";
                }
                else
                {
                    counts[header] = 1;
                    result[i] = header;
                }
            }
            return result;
        }

        // For KeepFirst, KeepLast, and SkipRecord, we'll handle in CreateRecordDictionary
        return headers;
    }

    /// <summary>
    /// Create a dictionary from headers and record values with duplicate handling
    /// </summary>
    private static Dictionary<string, string>? CreateRecordDictionary(string[] headers, string[] record, DuplicateHeaderHandling duplicateHandling)
    {
        var dict = new Dictionary<string, string>(Math.Min(headers.Length, record.Length));

        switch (duplicateHandling)
        {
            case DuplicateHeaderHandling.KeepFirst:
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    if (!dict.ContainsKey(headers[i]))
                    {
                        dict[headers[i]] = record[i]; // Only adds if key doesn't exist
                    }
                }
                break;

            case DuplicateHeaderHandling.KeepLast:
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i]; // Overwrites existing values
                }
                break;

            case DuplicateHeaderHandling.SkipRecord:
                var seen = new HashSet<string>();
                for (int i = 0; i < headers.Length; i++)
                {
                    if (!seen.Add(headers[i]))
                    {
                        return null; // Skip this record due to duplicate headers
                    }
                }
                // No duplicates, create the dictionary normally
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i];
                }
                break;

            default: // ThrowException and MakeUnique already handled
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i];
                }
                break;
        }

        return dict;
    }
}