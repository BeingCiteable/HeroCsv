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
    public static ICsvReaderBuilder Configure() => new CsvReaderBuilder();

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

        return [.. fields];
    }
}

/// <summary>
/// Advanced result from CSV reading operations with performance optimizations
/// </summary>
public sealed partial class CsvReadResult
{
    private IReadOnlyList<string[]>? _records;
    private IReadOnlyList<string>? _validationErrors;
    private IReadOnlyDictionary<string, object>? _statistics;

    /// <summary>
    /// Parsed CSV records as arrays of field values
    /// </summary>
    public IReadOnlyList<string[]> Records
    {
        get => _records ?? [];
        set => _records = value;
    }

    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Indicates whether CSV validation passed without errors
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation errors encountered during processing
    /// </summary>
    public IReadOnlyList<string> ValidationErrors
    {
        get => _validationErrors ?? [];
        set => _validationErrors = value;
    }

    /// <summary>
    /// Time taken to process the CSV data
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Performance statistics and metrics from the parsing operation
    /// </summary>
    public IReadOnlyDictionary<string, object> Statistics
    {
        get => _statistics ?? EmptyStatistics;
        set => _statistics = value;
    }

    /// <summary>
    /// Indicates if any validation errors were encountered
    /// </summary>
    public bool HasValidationErrors => _validationErrors?.Count > 0;

    /// <summary>
    /// Count of validation errors (0 if none)
    /// </summary>
    public int ValidationErrorCount => _validationErrors?.Count ?? 0;

    /// <summary>
    /// Indicates if performance statistics are available
    /// </summary>
    public bool HasStatistics => _statistics?.Count > 0;

    // Cached empty statistics dictionary to avoid allocations
    private static readonly IReadOnlyDictionary<string, object> EmptyStatistics =
        new Dictionary<string, object>();

    /// <summary>
    /// Creates a successful result with records and processing time
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <returns>Successful CSV read result</returns>
    public static CsvReadResult Success(IReadOnlyList<string[]> records, TimeSpan processingTime)
    {
        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">List of validation errors</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <returns>Failed CSV read result</returns>
    public static CsvReadResult Failure(IReadOnlyList<string> validationErrors, TimeSpan processingTime)
    {
        return new CsvReadResult
        {
            IsValid = false,
            ValidationErrors = validationErrors,
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Creates a successful result with statistics
    /// </summary>
    /// <param name="records">Parsed CSV records</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="statistics">Performance statistics</param>
    /// <returns>Successful CSV read result with statistics</returns>
    public static CsvReadResult SuccessWithStatistics(
        IReadOnlyList<string[]> records,
        TimeSpan processingTime,
        IReadOnlyDictionary<string, object> statistics)
    {
        return new CsvReadResult
        {
            Records = records,
            TotalRecords = records.Count,
            IsValid = true,
            ProcessingTime = processingTime,
            Statistics = statistics
        };
    }
}

