using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Builds and executes CSV parsing operations with custom configuration
/// </summary>
internal sealed class CsvReaderBuilder : ICsvReaderBuilder
{
    private string? _content;
    private string? _filePath;
    private CsvOptions _options = CsvOptions.Default;
    private bool _validateData = false;
    private bool _trackErrors = false;
#if NET9_0_OR_GREATER
    private bool _enableProfiling = false;
#endif
#if NET6_0_OR_GREATER
    private bool _hardwareAcceleration = false;
#endif

    /// <inheritdoc />
    public ICsvReaderBuilder WithContent(string content)
    {
        _content = content;
        _filePath = null;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithFile(string filePath)
    {
        _filePath = filePath;
        _content = null;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithDelimiter(char delimiter)
    {
        _options = new CsvOptions(delimiter, _options.Quote, _options.HasHeader, _options.TrimWhitespace, _options.SkipEmptyFields, _options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithQuote(char quote)
    {
        _options = new CsvOptions(_options.Delimiter, quote, _options.HasHeader, _options.TrimWhitespace, _options.SkipEmptyFields, _options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithHeaders(bool hasHeader = true)
    {
        _options = new CsvOptions(_options.Delimiter, _options.Quote, hasHeader, _options.TrimWhitespace, _options.SkipEmptyFields, _options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithValidation(bool validate = true)
    {
        _validateData = validate;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithErrorTracking(bool trackErrors = true)
    {
        _trackErrors = trackErrors;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithSkipEmptyFields(bool skipEmpty = true)
    {
        _options = new CsvOptions(
            _options.Delimiter,
            _options.Quote,
            _options.HasHeader,
            _options.TrimWhitespace,
            skipEmpty,
            _options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithTrimWhitespace(bool trimWhitespace = true)
    {
        _options = new CsvOptions(
            _options.Delimiter,
            _options.Quote,
            _options.HasHeader,
            trimWhitespace,
            _options.SkipEmptyFields,
            _options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithOptions(CsvOptions options)
    {
        _options = options;
        return this;
    }

#if NET6_0_OR_GREATER
    public ICsvReaderBuilder WithHardwareAcceleration(bool enabled = true)
    {
        _hardwareAcceleration = enabled;
        return this;
    }
#endif

#if NET9_0_OR_GREATER
    public ICsvReaderBuilder WithProfiling(bool enabled = true)
    {
        _enableProfiling = enabled;
        return this;
    }
#endif

    /// <inheritdoc />
    public IEnumerable<string[]> Read()
    {
        var content = GetContent();
        var reader = CreateConfiguredReader(content);
        return ExtractRecords(reader);
    }

    /// <inheritdoc />
    public IEnumerable<Dictionary<string, string>> ReadWithHeaders()
    {
        return ReadWithHeaders(DuplicateHeaderHandling.ThrowException);
    }

    /// <inheritdoc />
    public IEnumerable<Dictionary<string, string>> ReadWithHeaders(DuplicateHeaderHandling duplicateHandling)
    {
        var content = GetContent();
        var reader = CreateConfiguredReader(content);
        return ExtractRecordsWithHeaders(reader, duplicateHandling);
    }

    /// <inheritdoc />
    public CsvReadResult ReadWithDetails()
    {
        // Use default options but respect builder's validation and error tracking settings
        var options = CsvReadDetailsOptions.Default;
        options.ValidateData = _validateData;
        options.TrackValidationErrors = _trackErrors;
        return ReadWithDetails(options);
    }

    /// <inheritdoc />
    public CsvReadResult ReadWithDetails(CsvReadDetailsOptions options)
    {
        var content = GetContent();

        var stopwatch = options.IncludeBasicStatistics || options.IncludePerformanceMetrics
            ? System.Diagnostics.Stopwatch.StartNew()
            : null;
        
        var records = new List<string[]>();
        var validationErrors = options.TrackValidationErrors ? new List<string>() : null;
        var statistics = (options.IncludeBasicStatistics || options.IncludePerformanceMetrics || 
                         options.IncludeFieldStatistics || options.IncludeHardwareInfo || 
                         options.IncludeMemoryStatistics) 
                         ? new Dictionary<string, object>() 
                         : null;

        long memoryBefore = 0;
        if (options.IncludeMemoryStatistics)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            memoryBefore = GC.GetTotalMemory(false);
        }

        using var reader = CreateConfiguredReader(content);

        // Extract records with validation
        while (reader.TryReadRecord(out var record))
        {
            var fields = record.ToArray();

            if (options.ValidateData)
            {
                var errors = ValidateRecord(record, reader.RecordCount);
                if (errors.Count > 0 && options.TrackValidationErrors)
                {
                    if (options.MaxValidationErrors == 0 || validationErrors?.Count < options.MaxValidationErrors)
                    {
                        validationErrors?.AddRange(errors);
                    }
                }
            }

            records.Add(fields);
        }

        stopwatch?.Stop();

        // Collect statistics based on options
        if (statistics != null)
        {
            CollectStatisticsBasedOnOptions(statistics, records, stopwatch?.Elapsed ?? TimeSpan.Zero, reader, options, memoryBefore);
        }

        var isValid = !options.ValidateData || (validationErrors?.Count == 0);

        // Use optimized factory methods
        if (!isValid && validationErrors?.Count > 0)
        {
            return CsvReadResult.Failure(validationErrors, stopwatch?.Elapsed ?? TimeSpan.Zero);
        }

#if NET9_0_OR_GREATER
        if (_enableProfiling)
        {
            return CsvReadResult.SuccessWithProfiling(records, stopwatch?.Elapsed ?? TimeSpan.Zero, null);
        }
#endif

        if (statistics?.Count > 0)
        {
            return CsvReadResult.SuccessWithStatistics(records, stopwatch?.Elapsed ?? TimeSpan.Zero, statistics);
        }

        return CsvReadResult.Success(records, stopwatch?.Elapsed ?? TimeSpan.Zero);
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>() where T : class, new()
    {
        var content = GetContent();
        var mapper = new CsvMapper<T>(_options);
        return MapRecords(content, mapper);
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(CsvMapping<T> mapping) where T : class, new()
    {
        var content = GetContent();
        var mapper = new CsvMapper<T>(mapping);
        return MapRecords(content, mapper);
    }

    private IEnumerable<T> MapRecords<T>(string content, CsvMapper<T> mapper) where T : class, new()
    {
        var records = Csv.ReadInternal(content.AsMemory(), _options);
        using var enumerator = records.GetEnumerator();

        // Handle headers if present
        if (_options.HasHeader && enumerator.MoveNext())
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetContent()
    {
        if (_content != null) return _content;
        if (_filePath != null) return File.ReadAllText(_filePath);
        throw new InvalidOperationException("No content or file path specified");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastCsvReader CreateConfiguredReader(string content)
    {
        return new FastCsvReader(content, _options);
    }

    private static List<string[]> ExtractRecords(FastCsvReader reader)
    {
        var records = new List<string[]>();

        while (reader.TryReadRecord(out var record))
        {
            records.Add(record.ToArray());
        }

        return records;
    }

    private static List<Dictionary<string, string>> ExtractRecordsWithHeaders(FastCsvReader reader, DuplicateHeaderHandling duplicateHandling = DuplicateHeaderHandling.ThrowException)
    {
        var records = new List<Dictionary<string, string>>();

        // Read header
        if (!reader.TryReadRecord(out var headerRecord))
        {
            return records;
        }

        var headers = headerRecord.ToArray();

        // Process headers according to duplicate handling strategy
        headers = ProcessHeadersForBuilder(headers, duplicateHandling);
        if (headers == null) return records; // Skip if duplicate handling says to skip

        // Read data records
        while (reader.TryReadRecord(out var dataRecord))
        {
            var dict = CreateRecordDictionaryForBuilder(headers, dataRecord.ToArray(), duplicateHandling);
            if (dict != null) records.Add(dict);
        }

        return records;
    }

    private static string[]? ProcessHeadersForBuilder(string[] headers, DuplicateHeaderHandling duplicateHandling)
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

        return headers;
    }

    private static Dictionary<string, string>? CreateRecordDictionaryForBuilder(string[] headers, string[] record, DuplicateHeaderHandling duplicateHandling)
    {
        var dict = new Dictionary<string, string>(Math.Min(headers.Length, record.Length));

        switch (duplicateHandling)
        {
            case DuplicateHeaderHandling.KeepFirst:
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    if (!dict.ContainsKey(headers[i]))
                    {
                        dict[headers[i]] = record[i];
                    }
                }
                break;

            case DuplicateHeaderHandling.KeepLast:
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i];
                }
                break;

            case DuplicateHeaderHandling.SkipRecord:
                var seen = new HashSet<string>();
                for (int i = 0; i < headers.Length; i++)
                {
                    if (!seen.Add(headers[i]))
                    {
                        return null;
                    }
                }
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i];
                }
                break;

            default:
                for (int i = 0; i < Math.Min(headers.Length, record.Length); i++)
                {
                    dict[headers[i]] = record[i];
                }
                break;
        }

        return dict;
    }

    private static List<string> ValidateRecord(ICsvRecord record, int recordNumber)
    {
        var errors = new List<string>();

        // Basic validation - can be extended
        if (record.FieldCount == 0)
        {
            errors.Add($"Record {recordNumber} is empty");
        }

        // Check for fields that are too long (basic validation)
        for (int i = 0; i < record.FieldCount; i++)
        {
            var field = record.GetField(i);
            if (field.Length > 10000) // Arbitrary limit
            {
                errors.Add($"Record {recordNumber}, field {i} exceeds maximum length");
            }
        }

        return errors;
    }

    private void CollectStatistics(Dictionary<string, object> statistics, List<string[]> records, TimeSpan processingTime, FastCsvReader reader)
    {
        statistics["RecordCount"] = records.Count;
        statistics["ProcessingTimeMs"] = processingTime.TotalMilliseconds;
        statistics["AverageFieldsPerRecord"] = records.Count > 0 ? records.Average(r => r.Length) : 0;
        statistics["RecordsPerSecond"] = records.Count / Math.Max(processingTime.TotalSeconds, 0.001);
        statistics["ValidationEnabled"] = _validateData;
        statistics["ErrorTrackingEnabled"] = _trackErrors;

#if NET6_0_OR_GREATER
        if (_hardwareAcceleration)
        {
            statistics["HardwareAcceleration"] = true;
            statistics["IsHardwareAccelerated"] = System.Numerics.Vector.IsHardwareAccelerated;
        }
#endif

#if NET9_0_OR_GREATER
        if (_enableProfiling)
        {
            statistics["ProfilingEnabled"] = true;
            statistics["Vector512Supported"] = System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated;
        }
#endif
    }

    private void CollectStatisticsBasedOnOptions(Dictionary<string, object> statistics, List<string[]> records, 
        TimeSpan processingTime, FastCsvReader reader, CsvReadDetailsOptions options, long memoryBefore)
    {
        if (options.IncludeBasicStatistics)
        {
            statistics["RecordCount"] = records.Count;
            statistics["ProcessingTimeMs"] = processingTime.TotalMilliseconds;
        }

        if (options.IncludePerformanceMetrics)
        {
            statistics["RecordsPerSecond"] = records.Count / Math.Max(processingTime.TotalSeconds, 0.001);
            statistics["BytesProcessed"] = reader.BytesProcessed;
            statistics["BytesPerSecond"] = reader.BytesProcessed / Math.Max(processingTime.TotalSeconds, 0.001);
        }

        if (options.IncludeFieldStatistics)
        {
            statistics["AverageFieldsPerRecord"] = records.Count > 0 ? records.Average(r => r.Length) : 0;
            statistics["MinFieldsPerRecord"] = records.Count > 0 ? records.Min(r => r.Length) : 0;
            statistics["MaxFieldsPerRecord"] = records.Count > 0 ? records.Max(r => r.Length) : 0;
        }

        if (options.IncludeMemoryStatistics)
        {
            var memoryAfter = GC.GetTotalMemory(false);
            statistics["MemoryUsedBytes"] = memoryAfter - memoryBefore;
            statistics["MemoryUsedMB"] = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
            statistics["GC0Collections"] = GC.CollectionCount(0);
            statistics["GC1Collections"] = GC.CollectionCount(1);
            statistics["GC2Collections"] = GC.CollectionCount(2);
        }

        if (options.ValidateData)
        {
            statistics["ValidationEnabled"] = true;
            statistics["ValidationErrorCount"] = reader.ValidationErrorCount;
        }

        if (options.TrackValidationErrors)
        {
            statistics["ErrorTrackingEnabled"] = true;
        }

#if NET6_0_OR_GREATER
        if (options.IncludeHardwareInfo && _hardwareAcceleration)
        {
            statistics["HardwareAcceleration"] = true;
            statistics["IsHardwareAccelerated"] = System.Numerics.Vector.IsHardwareAccelerated;
            statistics["VectorBitWidth"] = System.Numerics.Vector<byte>.Count * 8;
        }
#endif

#if NET8_0_OR_GREATER
        if (options.IncludeHardwareInfo)
        {
            statistics["Vector128Supported"] = System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated;
            statistics["Vector256Supported"] = System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated;
        }
#endif

#if NET9_0_OR_GREATER
        if (options.IncludeHardwareInfo)
        {
            statistics["Vector512Supported"] = System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated;
        }
        
        if (_enableProfiling)
        {
            statistics["ProfilingEnabled"] = true;
        }
#endif
    }
}

