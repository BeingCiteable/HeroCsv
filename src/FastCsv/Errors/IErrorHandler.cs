using FastCsv.Models;

namespace FastCsv.Errors;

/// <summary>
/// Handles error tracking and reporting for CSV parsing
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Whether error tracking is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Event raised when a validation error occurs
    /// </summary>
    event Action<CsvValidationError>? ErrorOccurred;

    /// <summary>
    /// Records a validation error
    /// </summary>
    /// <param name="error">The validation error to record</param>
    void RecordError(CsvValidationError error);

    /// <summary>
    /// Gets the validation result containing all errors
    /// </summary>
    CsvValidationResult GetValidationResult();

    /// <summary>
    /// Clears all recorded errors
    /// </summary>
    void Reset();
}