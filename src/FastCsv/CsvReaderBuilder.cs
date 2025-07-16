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

    public ICsvReaderBuilder WithContent(string content)
    {
        _content = content;
        _filePath = null;
        return this;
    }

    public ICsvReaderBuilder WithFile(string filePath)
    {
        _filePath = filePath;
        _content = null;
        return this;
    }

    public ICsvReaderBuilder WithDelimiter(char delimiter)
    {
        _options = new CsvOptions(delimiter, _options.Quote, _options.HasHeader, _options.TrimWhitespace, _options.NewLine);
        return this;
    }

    public ICsvReaderBuilder WithQuote(char quote)
    {
        _options = new CsvOptions(_options.Delimiter, quote, _options.HasHeader, _options.TrimWhitespace, _options.NewLine);
        return this;
    }

    public ICsvReaderBuilder WithHeaders(bool hasHeader = true)
    {
        _options = new CsvOptions(_options.Delimiter, _options.Quote, hasHeader, _options.TrimWhitespace, _options.NewLine);
        return this;
    }

    public ICsvReaderBuilder WithValidation(bool validate = true)
    {
        _validateData = validate;
        return this;
    }

    public ICsvReaderBuilder WithErrorTracking(bool trackErrors = true)
    {
        _trackErrors = trackErrors;
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

    public IEnumerable<string[]> Read()
    {
        var content = GetContent();
        var reader = CreateConfiguredReader(content);
        return ExtractRecords(reader);
    }

    public IEnumerable<Dictionary<string, string>> ReadWithHeaders()
    {
        var content = GetContent();
        var reader = CreateConfiguredReader(content);
        return ExtractRecordsWithHeaders(reader);
    }

    public CsvReadResult ReadAdvanced()
    {
        var content = GetContent();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var records = new List<string[]>();
        var validationErrors = _trackErrors ? new List<string>() : null;
        var statistics = new Dictionary<string, object>();

        using var reader = CreateConfiguredReader(content);

        // Extract records with validation
        while (reader.TryReadRecord(out var record))
        {
            var fields = record.ToArray();

            if (_validateData)
            {
                var errors = CsvReaderBuilder.ValidateRecord(record, reader.RecordCount);
                if (errors.Count > 0 && _trackErrors)
                {
                    validationErrors?.AddRange(errors);
                }
            }

            records.Add(fields);
        }

        stopwatch.Stop();

        // Collect statistics
        CollectStatistics(statistics, records, stopwatch.Elapsed, reader);

        var isValid = !_validateData || (validationErrors?.Count == 0);

        // Use optimized factory methods
        if (!isValid && validationErrors?.Count > 0)
        {
            return CsvReadResult.Failure(validationErrors, stopwatch.Elapsed);
        }

#if NET9_0_OR_GREATER
        if (_enableProfiling)
        {
            return CsvReadResult.SuccessWithProfiling(records, stopwatch.Elapsed);
        }
#endif

        if (statistics.Count > 0)
        {
            return CsvReadResult.SuccessWithStatistics(records, stopwatch.Elapsed, statistics);
        }

        return CsvReadResult.Success(records, stopwatch.Elapsed);
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

    private static List<Dictionary<string, string>> ExtractRecordsWithHeaders(FastCsvReader reader)
    {
        var records = new List<Dictionary<string, string>>();

        // Read header
        if (!reader.TryReadRecord(out var headerRecord))
        {
            return records;
        }

        var headers = headerRecord.ToArray();

        // Read data records
        while (reader.TryReadRecord(out var dataRecord))
        {
            var dict = new Dictionary<string, string>(Math.Min(headers.Length, dataRecord.FieldCount));

            for (int i = 0; i < Math.Min(headers.Length, dataRecord.FieldCount); i++)
            {
                dict[headers[i]] = dataRecord.GetField(i).ToString();
            }

            records.Add(dict);
        }

        return records;
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
}

