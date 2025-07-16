#if NET9_0_OR_GREATER
using System;

namespace FastCsv.Validation;

/// <summary>
/// .NET 9+ advanced validation enhancements for ICsvValidator
/// </summary>
public partial interface ICsvValidator
{
    /// <summary>
    /// Validate structure using advanced hardware acceleration
    /// </summary>
    bool ValidateStructureAdvanced(ReadOnlySpan<char> data, CsvOptions options);
}
#endif