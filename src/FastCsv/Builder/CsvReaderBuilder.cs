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
    private Action<CsvValidationError>? _errorCallback = null;
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

    public ICsvReaderBuilder WithStream(Stream stream)
    {
        throw new NotImplementedException();
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
    public ICsvReaderBuilder WithErrorCallback(Action<CsvValidationError> errorCallback)
    {
        _errorCallback = errorCallback;
        _trackErrors = true; // Automatically enable error tracking when callback is set
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

    public ICsvReader Build()
    {
        throw new NotImplementedException();
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
        return new FastCsvReader(
            content,
            _options,
            _validateData,
            _trackErrors,
            _errorCallback);
    }
}