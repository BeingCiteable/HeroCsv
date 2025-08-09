namespace HeroCsv.Models;

/// <summary>
/// Represents the result of CSV validation including any errors found
/// </summary>
public class CsvValidationResult
{
    private readonly List<CsvValidationError> _errors = [];

    /// <summary>
    /// Whether the CSV is valid (no errors found)
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// All validation errors found
    /// </summary>
    public IReadOnlyList<CsvValidationError> Errors => _errors;

    /// <summary>
    /// Total number of errors
    /// </summary>
    public int ErrorCount => _errors.Count;

    /// <summary>
    /// Add a validation error
    /// </summary>
    internal void AddError(CsvValidationError error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Clear all errors
    /// </summary>
    internal void Clear()
    {
        _errors.Clear();
    }
}