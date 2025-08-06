using HeroCsv.Models;

namespace HeroCsv.Errors;

/// <summary>
/// Default implementation of error tracking handler
/// </summary>
internal class ErrorHandler(bool isEnabled) : IErrorHandler
{
    private readonly CsvValidationResult _validationResult = new();

    public bool IsEnabled => isEnabled;

    public event Action<CsvValidationError>? ErrorOccurred;

    public void RecordError(CsvValidationError validationError)
    {
        if (!isEnabled)
            return;

        _validationResult.AddError(validationError);
        ErrorOccurred?.Invoke(validationError);
    }

    public CsvValidationResult GetValidationResult()
    {
        return _validationResult;
    }

    public void Reset()
    {
        _validationResult.Clear();
    }
}