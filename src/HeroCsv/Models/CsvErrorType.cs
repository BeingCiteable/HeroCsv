namespace HeroCsv.Models;

/// <summary>
/// Types of CSV validation errors
/// </summary>
public enum CsvErrorType
{
    /// <summary>
    /// Field contains unbalanced quotes
    /// </summary>
    UnbalancedQuotes,

    /// <summary>
    /// Record has incorrect number of fields
    /// </summary>
    InconsistentFieldCount,

    /// <summary>
    /// Empty field where value is required
    /// </summary>
    EmptyRequiredField,

    /// <summary>
    /// Field exceeds maximum length
    /// </summary>
    FieldTooLong,

    /// <summary>
    /// Invalid characters in field
    /// </summary>
    InvalidCharacters,

    /// <summary>
    /// Unexpected end of file
    /// </summary>
    UnexpectedEndOfFile,

    /// <summary>
    /// General parsing error
    /// </summary>
    ParsingError
}