using System.Text;
using HeroCsv.Builder;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Mapping;
using HeroCsv.Models;
using HeroCsv.Parsing;

namespace HeroCsv;

/// <summary>
/// Provides convenient methods for reading CSV data from strings and files
/// </summary>
public static partial class Csv
{
        /// <summary>
        /// Ensures we have valid options (not default struct)
        /// </summary>
        private static CsvOptions GetValidOptions(CsvOptions options)
        {
            return options.Delimiter == '\0' ? CsvOptions.Default : options;
        }
        /// <summary>
        /// Counts CSV records without allocating strings for maximum performance
        /// </summary>
        /// <param name="content">CSV text as memory span</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>Number of records found</returns>
        public static int CountRecords(ReadOnlySpan<char> content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            return CountRecords(content.ToString(), options);
        }

        /// <summary>
        /// Counts CSV records without allocating strings for maximum performance
        /// </summary>
        /// <param name="content">CSV text as memory</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>Number of records found</returns>
        public static int CountRecords(ReadOnlyMemory<char> content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            if (options.Quote == '"' && !ContainsQuote(content.Span, '"'))
            {
                return CountRecordsFast(content.Span, options);
            }

            var dataSource = new MemoryDataSource(content);
            using var reader = new HeroCsvReader(dataSource, options);
            return reader.CountRecords();
        }

        /// <summary>
        /// Checks if content contains the specified quote character
        /// </summary>
        private static bool ContainsQuote(ReadOnlySpan<char> content, char quote)
        {
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == quote) return true;
            }
            return false;
        }

        /// <summary>
        /// Ultra-fast record counting for CSV without quotes
        /// </summary>
        private static int CountRecordsFast(ReadOnlySpan<char> content, CsvOptions options)
        {
            if (content.IsEmpty) return 0;

            // CsvParser.CountLines counts newline characters
            int newlineCount = CsvParser.CountLines(content);
            
            // Number of records = newline count + 1 (for the last record if it doesn't end with newline)
            int recordCount = newlineCount;
            
            // If content doesn't end with a newline, there's one more record
            if (content.Length > 0 && content[content.Length - 1] != '\n' && content[content.Length - 1] != '\r')
            {
                recordCount++;
            }

            // If hasHeader is true, subtract 1 from total count
            if (options.HasHeader && recordCount > 0)
            {
                recordCount--;
            }

            return Math.Max(0, recordCount);
        }

        /// <summary>
        /// Counts CSV records from string content
        /// </summary>
        /// <param name="content">CSV text as string</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>Number of records found</returns>
        public static int CountRecords(string content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            return CountRecordsFast(content.AsSpan(), options);
        }

        /// <summary>
        /// Parses CSV content with default options
        /// </summary>
        /// <param name="content">Raw CSV text to parse</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadContent(string content)
        {
            return ReadContent(content, CsvOptions.Default);
        }

        /// <summary>
        /// Parses CSV content with custom delimiter
        /// </summary>
        /// <param name="content">Raw CSV text to parse</param>
        /// <param name="delimiter">Field separator character</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadContent(string content, char delimiter)
        {
            var options = new CsvOptions(delimiter);
            return ReadContent(content, options);
        }

        /// <summary>
        /// Parses CSV content with custom options
        /// </summary>
        /// <param name="content">Raw CSV text to parse</param>
        /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadContent(string content, CsvOptions options)
        {
            using var reader = CreateReader(content, options);
            return reader.GetRecords().ToList();
        }

        /// <summary>
        /// Loads and parses CSV data from a file with custom formatting settings
        /// </summary>
        /// <param name="filePath">Full or relative path to the CSV file</param>
        /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadFile(string filePath, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            var content = File.ReadAllText(filePath);
            return ReadContent(content, options);
        }

        /// <summary>
        /// Parses CSV content and returns all records as a read-only list for better performance
        /// </summary>
        /// <param name="content">CSV text as memory span</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>Read-only list of all parsed records</returns>
        public static IReadOnlyList<string[]> ReadAllRecords(ReadOnlySpan<char> content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            // Convert span to string to ensure memory safety across method boundaries
            return ReadAllRecords(content.ToString(), options);
        }

        /// <summary>
        /// Parses CSV content from memory and returns all records as a read-only list
        /// </summary>
        /// <param name="content">CSV text as memory</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>All parsed records</returns>
        public static IReadOnlyList<string[]> ReadAllRecords(ReadOnlyMemory<char> content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            var dataSource = new MemoryDataSource(content);
            using var reader = new HeroCsvReader(dataSource, options);
            return reader.ReadAllRecords();
        }

        /// <summary>
        /// Parses CSV content from string and returns all records as a read-only list
        /// </summary>
        /// <param name="content">CSV text as string</param>
        /// <param name="options">CSV parsing options</param>
        /// <returns>All parsed records</returns>
        public static IReadOnlyList<string[]> ReadAllRecords(string content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            using var reader = CreateReader(content, options);
            return reader.ReadAllRecords();
        }

        /// <summary>
        /// Creates a CSV reader from ReadOnlyMemory for zero-allocation parsing
        /// </summary>
        /// <param name="content">CSV content as memory</param>
        /// <param name="options">Parsing options</param>
        /// <returns>CSV reader instance</returns>
        public static ICsvReader CreateReader(ReadOnlyMemory<char> content, CsvOptions options = default)
        {
            options = GetValidOptions(options);
            var dataSource = new MemoryDataSource(content);
            return new HeroCsvReader(dataSource, options);
        }

        /// <summary>
        /// Creates a configuration builder for customizing CSV parsing behavior
        /// </summary>
        /// <returns>Configuration builder</returns>
        public static ICsvReaderBuilder Configure() => new CsvReaderBuilder();

        /// <summary>
        /// Creates a CSV reader with the specified content and options
        /// </summary>
        /// <param name="content">CSV content to read</param>
        /// <param name="options">Parsing options</param>
        /// <returns>CSV reader instance</returns>
        public static ICsvReader CreateReader(string content, CsvOptions options)
        {
            options = GetValidOptions(options);
            return new HeroCsvReader(content, options);
        }

        /// <summary>
        /// Creates a CSV reader with the specified content and default options
        /// </summary>
        /// <param name="content">CSV content to read</param>
        /// <returns>CSV reader instance</returns>
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
        /// <returns>CSV reader instance</returns>
        public static ICsvReader CreateReader(Stream stream, CsvOptions options = default, Encoding? encoding = null, bool leaveOpen = false)
        {
            options = GetValidOptions(options);
            return new HeroCsvReader(stream, options, encoding, leaveOpen);
        }

        /// <summary>
        /// Creates a CSV reader from a file
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="options">Parsing options</param>
        /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
        /// <returns>CSV reader instance</returns>
        public static ICsvReader CreateReaderFromFile(string filePath, CsvOptions options = default, Encoding? encoding = null)
        {
            options = GetValidOptions(options);
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var dataSource = new StreamDataSource(fileStream, encoding, leaveOpen: false);
            return new HeroCsvReader(dataSource, options);
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
        /// Reads CSV content and maps each record to the specified type using a fluent mapping builder
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="content">Raw CSV text to parse</param>
        /// <param name="buildMapping">Function to configure the mapping using fluent API</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> Read<T>(string content, Func<CsvMappingBuilder<T>, CsvMapping<T>> buildMapping) where T : class, new()
        {
            var builder = new CsvMappingBuilder<T>();
            var mapping = buildMapping(builder);
            return Read(content, mapping);
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
        /// Reads CSV file and maps each record to the specified type using a fluent mapping builder
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="filePath">Full or relative path to the CSV file</param>
        /// <param name="buildMapping">Function to configure the mapping using fluent API</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadFile<T>(string filePath, Func<CsvMappingBuilder<T>, CsvMapping<T>> buildMapping) where T : class, new()
        {
            var content = File.ReadAllText(filePath);
            return Read<T>(content, buildMapping);
        }


        /// <summary>
        /// Reads CSV content and maps each record to the specified type using auto mapping with manual overrides
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="content">Raw CSV text to parse</param>
        /// <param name="configureMapping">Action to configure manual mapping overrides</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadAutoMapWithOverrides<T>(string content, Action<CsvMapping<T>> configureMapping) where T : class, new()
        {
            return ReadAutoMapWithOverrides(content, CsvOptions.Default, configureMapping);
        }

        /// <summary>
        /// Reads CSV content and maps each record to the specified type using auto mapping with manual overrides and custom options
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="content">Raw CSV text to parse</param>
        /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
        /// <param name="configureMapping">Action to configure manual mapping overrides</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadAutoMapWithOverrides<T>(string content, CsvOptions options, Action<CsvMapping<T>> configureMapping) where T : class, new()
        {
            var mapping = CsvMapping.CreateAutoMapWithOverrides<T>();
            mapping.Options = options;
            configureMapping(mapping);
            return Read<T>(content, mapping);
        }

        /// <summary>
        /// Reads CSV file and maps each record to the specified type using auto mapping with manual overrides
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="filePath">Full or relative path to the CSV file</param>
        /// <param name="configureMapping">Action to configure manual mapping overrides</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadFileAutoMapWithOverrides<T>(string filePath, Action<CsvMapping<T>> configureMapping) where T : class, new()
        {
            var content = File.ReadAllText(filePath);
            return ReadAutoMapWithOverrides<T>(content, configureMapping);
        }

        /// <summary>
        /// Reads CSV data from a stream and returns each row as a string array
        /// </summary>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="leaveOpen">Whether to leave the stream open after reading (default: false)</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadStream(Stream stream, bool leaveOpen = false)
        {
            return ReadStream(stream, CsvOptions.Default, leaveOpen);
        }

        /// <summary>
        /// Reads CSV data from a stream with custom options
        /// </summary>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
        /// <param name="leaveOpen">Whether to leave the stream open after reading (default: false)</param>
        /// <returns>Each CSV row as an array of field values</returns>
        public static IEnumerable<string[]> ReadStream(Stream stream, CsvOptions options, bool leaveOpen = false)
        {
            using var reader = CreateReader(stream, options, leaveOpen: leaveOpen);
            return reader.GetRecords().ToList(); // Materialize to avoid accessing disposed reader
        }

        /// <summary>
        /// Reads CSV data from a stream and maps each record to the specified type using auto mapping
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="leaveOpen">Whether to leave the stream open after reading (default: false)</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadStream<T>(Stream stream, bool leaveOpen = false) where T : class, new()
        {
            return ReadStream<T>(stream, CsvOptions.Default, leaveOpen);
        }

        /// <summary>
        /// Reads CSV data from a stream and maps each record to the specified type using auto mapping with custom options
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="options">Parsing configuration for delimiter, quotes, headers, etc.</param>
        /// <param name="leaveOpen">Whether to leave the stream open after reading (default: false)</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadStream<T>(Stream stream, CsvOptions options, bool leaveOpen = false) where T : class, new()
        {
            using var reader = CreateReader(stream, options, leaveOpen: leaveOpen);
            
            var mapper = new CsvMapper<T>(options);
            
            // If the CSV has headers, read them first and set on mapper
            if (options.HasHeader && reader.TryReadRecord(out var headerRecord))
            {
                mapper.SetHeaders(headerRecord.ToArray());
            }
            
            // Now read the data records
            var records = reader.GetRecords();
            using var enumerator = records.GetEnumerator();

            // Transform each CSV record into the target object type
            while (enumerator.MoveNext())
            {
                var record = enumerator.Current;
                yield return mapper.MapRecord(record);
            }
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
        /// Reads CSV data from a stream and maps each record to the specified type using auto mapping with manual overrides
        /// </summary>
        /// <typeparam name="T">Type to map CSV records to</typeparam>
        /// <param name="stream">Stream containing CSV data</param>
        /// <param name="configureMapping">Action to configure manual mapping overrides</param>
        /// <returns>Enumerable of mapped objects</returns>
        public static IEnumerable<T> ReadStreamAutoMapWithOverrides<T>(Stream stream, Action<CsvMapping<T>> configureMapping) where T : class, new()
        {
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return ReadAutoMapWithOverrides<T>(content, configureMapping);
        }

        /// <summary>
        /// Internal method for reading with a configured mapper
        /// </summary>
        private static IEnumerable<T> ReadWithMapper<T>(string content, CsvOptions options, CsvMapper<T> mapper) where T : class, new()
        {
            using var reader = CreateReader(content, options);
            
            // If the CSV has headers, read them first and set on mapper
            if (options.HasHeader && reader.TryReadRecord(out var headerRecord))
            {
                mapper.SetHeaders(headerRecord.ToArray());
            }
            
            // Now read the data records
            var records = reader.GetRecords();
            using var enumerator = records.GetEnumerator();

            // Transform each CSV record into the target object type
            while (enumerator.MoveNext())
            {
                var record = enumerator.Current;
                yield return mapper.MapRecord(record);
            }
        }
    }