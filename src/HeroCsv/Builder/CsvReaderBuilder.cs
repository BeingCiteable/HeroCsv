using System.Runtime.CompilerServices;
using HeroCsv.Configuration;
using HeroCsv.Core;
using HeroCsv.DataSources;
using HeroCsv.Models;

namespace HeroCsv.Builder;

/// <summary>
/// Builds and executes CSV parsing operations with custom configuration
/// </summary>
public sealed class CsvReaderBuilder : ICsvReaderBuilder
{
    private string? _content;
    private string? _filePath;
    private Stream? _stream;
    private readonly CsvReaderConfiguration _configuration;
    
    public CsvReaderBuilder()
    {
        _configuration = new CsvReaderConfiguration
        {
            Options = CsvOptions.Default,
            StringBuilderPool = new StringBuilderPool()
        };
    }
    
#if NET6_0_OR_GREATER
    private bool _hardwareAcceleration;
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
    public ICsvReaderBuilder WithStream(Stream stream)
    {
        _stream = stream;
        _content = null;
        _filePath = null;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithDelimiter(char delimiter)
    {
        _configuration.Options = new CsvOptions(
            delimiter, 
            _configuration.Options.Quote, 
            _configuration.Options.HasHeader, 
            _configuration.Options.TrimWhitespace, 
            _configuration.Options.SkipEmptyFields, 
            _configuration.Options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithQuote(char quote)
    {
        _configuration.Options = new CsvOptions(
            _configuration.Options.Delimiter, 
            quote, 
            _configuration.Options.HasHeader, 
            _configuration.Options.TrimWhitespace, 
            _configuration.Options.SkipEmptyFields, 
            _configuration.Options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithValidation(bool validate = true)
    {
        _configuration.EnableValidation = validate;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithErrorTracking(bool trackErrors = true)
    {
        _configuration.TrackErrors = trackErrors;
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithErrorCallback(Action<CsvValidationError> errorCallback)
    {
        _configuration.ErrorCallback = errorCallback;
        _configuration.TrackErrors = true; // Automatically enable error tracking when callback is set
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithSkipEmptyFields(bool skipEmpty = true)
    {
        _configuration.Options = new CsvOptions(
            _configuration.Options.Delimiter,
            _configuration.Options.Quote,
            _configuration.Options.HasHeader,
            _configuration.Options.TrimWhitespace,
            skipEmpty,
            _configuration.Options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithTrimWhitespace(bool trimWhitespace = true)
    {
        _configuration.Options = new CsvOptions(
            _configuration.Options.Delimiter,
            _configuration.Options.Quote,
            _configuration.Options.HasHeader,
            trimWhitespace,
            _configuration.Options.SkipEmptyFields,
            _configuration.Options.NewLine);
        return this;
    }

    /// <inheritdoc />
    public ICsvReaderBuilder WithOptions(CsvOptions options)
    {
        _configuration.Options = options;
        return this;
    }

#if NET6_0_OR_GREATER
    public ICsvReaderBuilder WithHardwareAcceleration(bool enabled = true)
    {
        _hardwareAcceleration = enabled;
        return this;
    }
#endif

    /// <inheritdoc />
    public ICsvReader Build()
    {
        if (_stream != null)
        {
            var streamSource = new StreamDataSource(_stream);
            return new HeroCsvReader(streamSource, _configuration);
        }

        var content = GetContent();
        var stringSource = new StringDataSource(content);
        return new HeroCsvReader(stringSource, _configuration);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetContent()
    {
        if (_content != null) return _content;
        if (_filePath != null) return File.ReadAllText(_filePath);
        throw new InvalidOperationException(
            "No CSV data source specified. Use WithContent() to provide CSV string data or WithFile() to specify a file path.");
    }


    /// <summary>
    /// Executes parsing and returns results
    /// </summary>
    public CsvReadResult Read()
    {
        using var reader = Build();
        var records = reader.ReadAllRecords();
        return new CsvReadResult(
            records: records,
            recordCount: reader.RecordCount,
            lineCount: reader.LineNumber,
            validationResult: reader.ValidationResult,
            validationPerformed: _configuration.EnableValidation,
            errorTrackingEnabled: _configuration.TrackErrors);
    }

    /// <summary>
    /// Executes parsing and returns enumerable
    /// </summary>
    public IEnumerable<string[]> ReadEnumerable()
    {
        using var reader = Build();
        return reader.GetRecords();
    }
}