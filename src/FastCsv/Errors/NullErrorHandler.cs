using FastCsv.Models;

namespace FastCsv.Errors;

/// <summary>
/// Null implementation of error handler when error tracking is disabled
/// </summary>
internal class NullErrorHandler : IErrorHandler
{
    private static readonly CsvValidationResult EmptyResult = new();

    public bool IsEnabled => false;

    public event Action<CsvValidationError>? ErrorOccurred
    {
        add { } // No-op
        remove { } // No-op
    }

    public void RecordError(CsvValidationError error)
    {
        // Do nothing - error tracking is disabled
    }

    public CsvValidationResult GetValidationResult()
    {
        return EmptyResult;
    }

    public void Reset()
    {
        // Do nothing
    }
}