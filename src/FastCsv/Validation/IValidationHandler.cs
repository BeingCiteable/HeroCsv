using System;

namespace FastCsv.Validation;

/// <summary>
/// Handler responsible for CSV validation
/// Core responsibility: Validate CSV data structure and content
/// </summary>
public partial interface IValidationHandler
{
    /// <summary>
    /// Validate that a record is well-formed
    /// </summary>
    bool IsValidRecord(ReadOnlySpan<char> record, CsvOptions options);
    
    /// <summary>
    /// Validate that a field is properly formatted
    /// </summary>
    bool IsValidField(ReadOnlySpan<char> field, CsvOptions options);
    
    /// <summary>
    /// Check if quotes are properly balanced in a field
    /// </summary>
    bool HasBalancedQuotes(ReadOnlySpan<char> field, CsvOptions options);
    
    /// <summary>
    /// Validate the overall CSV structure
    /// </summary>
    bool IsValidCsvStructure(ReadOnlySpan<char> data, CsvOptions options);
}