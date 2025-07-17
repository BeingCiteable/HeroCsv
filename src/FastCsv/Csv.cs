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
    /// <param name="csvContent">CSV text as memory span</param>
    /// <param name="destination">Pre-allocated span to fill with parsed records</param>
    /// <returns>Number of records parsed</returns>
    public static int Read(ReadOnlySpan<char> csvContent, Span<string[]> destination)
    {
        return ReadIntoArray(csvContent, destination, CsvOptions.Default);
    }

    /// <summary>
    /// High-performance CSV parsing with pre-allocated destination array
    /// </summary>
    /// <param name="csvContent">CSV text as memory span</param>
    /// <param name="destination">Pre-allocated span to fill with parsed records</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Number of records parsed</returns>
    public static int ReadIntoArray(ReadOnlySpan<char> csvContent, Span<string[]> destination, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        var recordCount = 0;
        var position = 0;

        while (position < csvContent.Length && recordCount < destination.Length)
        {
            var lineEnd = FindLineEnd(csvContent, position);
            var lineSpan = csvContent.Slice(position, lineEnd - position);

            if (lineSpan.Length > 0)
            {
                var fields = ParseLine(lineSpan, options);
                if (fields.Length > 0)
                {
                    destination[recordCount++] = fields;
                }
            }

            position = SkipLineEnding(csvContent, lineEnd);
        }

        return recordCount;
    }

    /// <summary>
    /// Counts CSV records without allocating strings for maximum performance
    /// </summary>
    /// <param name="csvContent">CSV text as memory span</param>
    /// <param name="options">CSV parsing options</param>
    /// <returns>Number of records found</returns>
    public static int CountRecords(ReadOnlySpan<char> csvContent, CsvOptions options = default)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        
        var count = 0;
        var position = 0;

        while (position < csvContent.Length)
        {
            var lineEnd = FindLineEnd(csvContent, position);
            var lineSpan = csvContent.Slice(position, lineEnd - position);

            if (lineSpan.Length > 0)
            {
                count++;
            }

            position = SkipLineEnding(csvContent, lineEnd);
        }

        return count;
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
    /// Parses CSV content with custom options
    /// </summary>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each CSV row as an array of field values</returns>
    public static IEnumerable<string[]> Read(string csvContent, CsvOptions options)
    {
        return ReadInternal(csvContent.AsSpan(), options);
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
        return ReadInternal(content.AsSpan(), options);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names, returns data as name-value pairs
    /// </summary>
    /// <param name="csvContent">Raw CSV text where first line contains column headers</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string csvContent, CsvOptions options = default)
    {
        return ReadWithHeaders(csvContent, options, DuplicateHeaderHandling.ThrowException);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names with specified duplicate header handling
    /// </summary>
    /// <param name="csvContent">Raw CSV text where first line contains column headers</param>
    /// <param name="duplicateHandling">Strategy for handling duplicate column headers</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string csvContent, DuplicateHeaderHandling duplicateHandling)
    {
        return ReadWithHeaders(csvContent, CsvOptions.Default, duplicateHandling);
    }

    /// <summary>
    /// Parses CSV data where first row contains column names with specified duplicate header handling
    /// </summary>
    /// <param name="csvContent">Raw CSV text where first line contains column headers</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <param name="duplicateHandling">Strategy for handling duplicate column headers</param>
    /// <returns>Each data row as a dictionary mapping column names to field values</returns>
    public static IEnumerable<Dictionary<string, string>> ReadWithHeaders(string csvContent, CsvOptions options, DuplicateHeaderHandling duplicateHandling)
    {
        if (options.Equals(default(CsvOptions))) options = CsvOptions.Default;
        var records = ReadInternal(csvContent.AsSpan(), options);
        using var enumerator = records.GetEnumerator();

        if (!enumerator.MoveNext()) yield break;

        var headers = ProcessHeaders(enumerator.Current, duplicateHandling);
        if (headers == null) yield break; // Skip if duplicate handling says to skip

        while (enumerator.MoveNext())
        {
            var record = enumerator.Current;
            var dict = CreateRecordDictionary(headers, record, duplicateHandling);
            if (dict != null) yield return dict;
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
    /// Reads CSV content and maps each record to the specified type using auto mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string csvContent) where T : class, new()
    {
        return Read<T>(csvContent, CsvOptions.Default);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using auto mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string csvContent, CsvOptions options) where T : class, new()
    {
        var mapper = new CsvMapper<T>(options);
        return ReadWithMapper(csvContent, options, mapper);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using manual mapping
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> Read<T>(string csvContent, CsvMapping<T> mapping) where T : class, new()
    {
        var mapper = new CsvMapper<T>(mapping);
        return ReadWithMapper(csvContent, mapping.Options, mapper);
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
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadMixed<T>(string csvContent, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        return ReadMixed<T>(csvContent, CsvOptions.Default, configureMapping);
    }

    /// <summary>
    /// Reads CSV content and maps each record to the specified type using mixed mapping with custom options
    /// </summary>
    /// <typeparam name="T">Type to map CSV records to</typeparam>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
    /// <param name="configureMapping">Action to configure manual mapping overrides</param>
    /// <returns>Enumerable of mapped objects</returns>
    public static IEnumerable<T> ReadMixed<T>(string csvContent, CsvOptions options, Action<CsvMapping<T>> configureMapping) where T : class, new()
    {
        var mapping = CsvMapping<T>.CreateMixed();
        mapping.Options = options;
        configureMapping(mapping);
        return Read<T>(csvContent, mapping);
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
        return ReadInternal(content.AsSpan(), options);
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
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <param name="detailsOptions">Configuration for what details to collect during parsing</param>
    /// <returns>Detailed parsing result based on configured options</returns>
    public static CsvReadResult ReadWithDetails(string csvContent, CsvReadDetailsOptions detailsOptions)
    {
        return Configure(csvContent).ReadWithDetails(detailsOptions);
    }

    /// <summary>
    /// Reads CSV content with detailed parsing results using default detail options
    /// </summary>
    /// <param name="csvContent">Raw CSV text to parse</param>
    /// <returns>Detailed parsing result with default metrics</returns>
    public static CsvReadResult ReadWithDetails(string csvContent)
    {
        return Configure(csvContent).ReadWithDetails();
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
    private static IEnumerable<T> ReadWithMapper<T>(string csvContent, CsvOptions options, CsvMapper<T> mapper) where T : class, new()
    {
        var records = ReadInternal(csvContent.AsSpan(), options);
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
    internal static IEnumerable<string[]> ReadInternal(ReadOnlySpan<char> csvContent, CsvOptions options)
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

    /// <summary>
    /// Finds the position of the first newline character ('\n' or '\r') in the specified content, starting from the
    /// given index.
    /// </summary>
    /// <param name="content">The string to search for a newline character. Cannot be null.</param>
    /// <param name="start">The zero-based index in the string at which to begin the search. Must be within the bounds of the string.</param>
    /// <returns>The zero-based index of the first newline character found, or the length of the string if no newline character
    /// is present.</returns>
    private static int FindLineEnd(string content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    /// <summary>
    /// Finds the index of the first newline character ('\n' or '\r') in the specified span of text, starting from the
    /// given position.
    /// </summary>
    /// <param name="content">The span of characters to search for a newline character.</param>
    /// <param name="start">The zero-based index at which to begin the search.</param>
    /// <returns>The zero-based index of the first newline character found, or the length of <paramref name="content"/> if no
    /// newline character is found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindLineEnd(ReadOnlySpan<char> content, int start)
    {
        for (int i = start; i < content.Length; i++)
        {
            if (content[i] == '\n' || content[i] == '\r')
                return i;
        }
        return content.Length;
    }

    /// <summary>
    /// Skips over a line ending sequence in the specified content, starting at the given position.
    /// </summary>
    /// <remarks>This method recognizes both Windows-style ("\r\n") and Unix-style ("\n") line endings. If the
    /// starting position is at or beyond the end of the content, the method returns the original position.</remarks>
    /// <param name="content">The span of characters to process.</param>
    /// <param name="position">The starting position within <paramref name="content"/> to check for a line ending sequence.</param>
    /// <returns>The position immediately after the line ending sequence, or the original position if no line ending is found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SkipLineEnding(ReadOnlySpan<char> content, int position)
    {
        if (position >= content.Length) return position;

        if (content[position] == '\r')
        {
            position++;
            if (position < content.Length && content[position] == '\n')
                position++;
        }
        else if (content[position] == '\n')
        {
            position++;
        }

        return position;
    }

    /// <summary>
    /// Parses a single line of CSV data into an array of fields based on the specified options.
    /// </summary>
    /// <remarks>This method supports parsing both quoted and unquoted fields in the CSV line. Quoted fields
    /// allow the inclusion of special characters,  such as the delimiter, within the field value. If no quotes are
    /// detected in the line, a faster parsing path is used.</remarks>
    /// <param name="line">The line of text to parse, represented as a <see cref="ReadOnlySpan{T}"/> of characters.</param>
    /// <param name="options">The <see cref="CsvOptions"/> that define parsing behavior, such as the delimiter, quote character, and whether
    /// to trim whitespace.</param>
    /// <returns>An array of strings, where each element represents a field extracted from the input line.  If the input line is
    /// empty, an empty array is returned.</returns>
    private static string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Optimized path: Check if line contains quotes - if not, use faster parsing
        bool hasQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Quote)
            {
                hasQuotes = true;
                break;
            }
        }

        if (!hasQuotes)
        {
            // Fast path: No quotes, pre-allocate exact size
            var fieldCount = 1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == options.Delimiter) fieldCount++;
            }

            var fields = new string[fieldCount];
            var fieldIndex = 0;
            var fieldStart = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == options.Delimiter)
                {
                    var field = line.Slice(fieldStart, i - fieldStart);
                    fields[fieldIndex++] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
                    fieldStart = i + 1;
                }
            }

            // Add final field
            if (fieldStart <= line.Length)
            {
                var field = line.Slice(fieldStart);
                fields[fieldIndex] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
            }

            return fields;
        }

        // Slow path: Handle quotes properly
        var fieldList = new List<string>(8);
        var quotedFieldStart = 0;
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == options.Quote && !inQuotes)
            {
                inQuotes = true;
                quotedFieldStart = i + 1;
            }
            else if (ch == options.Quote && inQuotes)
            {
                inQuotes = false;
            }
            else if (ch == options.Delimiter && !inQuotes)
            {
                var fieldSpan = line.Slice(quotedFieldStart, i - quotedFieldStart);
                var field = options.TrimWhitespace ? fieldSpan.Trim().ToString() : fieldSpan.ToString();
                fieldList.Add(field);
                quotedFieldStart = i + 1;
            }
        }

        // Add final field
        if (quotedFieldStart <= line.Length)
        {
            var fieldSpan = line.Slice(quotedFieldStart);
            var field = options.TrimWhitespace ? fieldSpan.Trim().ToString() : fieldSpan.ToString();
            fieldList.Add(field);
        }

        return [.. fieldList];
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

    /// <summary>
    /// Provides a cached, empty, read-only dictionary of statistics to avoid unnecessary allocations.
    /// </summary>
    /// <remarks>This dictionary is intended for scenarios where an empty set of statistics is required, and
    /// it ensures no additional memory allocations are performed. The dictionary is immutable and can be safely shared
    /// across multiple consumers.</remarks>
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

