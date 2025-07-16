#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Frozen;

namespace FastCsv.Validation;

/// <summary>
/// Optimized character detection validation enhancements for ICsvValidator
/// </summary>
public partial interface ICsvValidator
{
    /// <summary>
    /// Validate CSV structure using optimized character detection
    /// </summary>
    bool ValidateStructureAdvanced(ReadOnlySpan<char> data, SearchValues<char> invalidChars);

    /// <summary>
    /// Validate field using optimized character scanning
    /// </summary>
    bool IsValidFieldAdvanced(ReadOnlySpan<char> field, SearchValues<char> specialChars);

    /// <summary>
    /// Get validation rules for fields
    /// </summary>
    FrozenDictionary<string, ValidationRule> GetValidationRules();
}

/// <summary>
/// Validation rule for field validation
/// </summary>
public readonly struct ValidationRule(
    string name,
    ValidationSeverity severity,
    Func<string, bool> validator,
    string errorMessage)
{
    public string Name { get; } = name;
    public ValidationSeverity Severity { get; } = severity;
    public Func<string, bool> Validator { get; } = validator;
    public string ErrorMessage { get; } = errorMessage;
}
#endif