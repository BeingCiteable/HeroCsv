using HeroCsv.Errors;
using HeroCsv.Models;

namespace HeroCsv.Validation;

/// <summary>
/// Default implementation of CSV validation handler
/// </summary>
internal class ValidationHandler(CsvOptions options, IErrorHandler errorHandler, bool isEnabled) : IValidationHandler
{
    private readonly CsvOptions _options = options;
    private readonly IErrorHandler _errorHandler = errorHandler;
    private readonly bool _isEnabled = isEnabled;
    private int? _expectedFieldCount;

    public bool IsEnabled => _isEnabled;

    public int? ExpectedFieldCount => _expectedFieldCount;

    public void ValidateRecord(string[] fields, int lineNumber, int? expectedFieldCount)
    {
        if (!_isEnabled) return;

        // Set expected field count from first record
        if (_expectedFieldCount == null && expectedFieldCount == null)
        {
            _expectedFieldCount = fields.Length;
        }
        else if (_expectedFieldCount == null)
        {
            _expectedFieldCount = expectedFieldCount;
        }

        // Check for consistent field count
        if (_expectedFieldCount != null && fields.Length != _expectedFieldCount.Value)
        {
            var error = new CsvValidationError(
                CsvErrorType.InconsistentFieldCount,
                $"Expected {_expectedFieldCount} fields but found {fields.Length}",
                lineNumber,
                content: string.Join(_options.Delimiter.ToString(), fields)
            );
            _errorHandler.RecordError(error);
        }

        // Check each field for validation issues
        for (int i = 0; i < fields.Length; i++)
        {
            ValidateField(fields[i], lineNumber, i);
        }
    }

    private void ValidateField(string field, int lineNumber, int fieldIndex)
    {
        // Check for unbalanced quotes
        int quoteCount = 0;
        for (int j = 0; j < field.Length; j++)
        {
            if (field[j] == _options.Quote)
            {
                quoteCount++;
            }
        }

        if (quoteCount % 2 != 0)
        {
            var error = new CsvValidationError(
                CsvErrorType.UnbalancedQuotes,
                "Field contains unbalanced quotes",
                lineNumber,
                fieldIndex,
                field
            );
            _errorHandler.RecordError(error);
        }
    }

    public IReadOnlyList<CsvValidationError> GetErrors()
    {
        return _errorHandler.GetValidationResult().Errors;
    }

    public void Reset()
    {
        _expectedFieldCount = null;
        _errorHandler.Reset();
    }
}