using HeroCsv.Models;

namespace HeroCsv.Validation;

/// <summary>
/// Null implementation of validation handler when validation is disabled
/// </summary>
internal class NullValidationHandler : IValidationHandler
{
    public bool IsEnabled => false;

    public int? ExpectedFieldCount => null;

    public void ValidateRecord(string[] fields, int lineNumber, int? expectedFieldCount)
    {
        // Do nothing - validation is disabled
    }

    public static void ValidateField(string field, int lineNumber, int fieldIndex)
    {
        // Do nothing - validation is disabled
    }

    public IReadOnlyList<CsvValidationError> GetErrors()
    {
        return Array.Empty<CsvValidationError>();
    }

    public void Reset()
    {
        // Do nothing
    }
}