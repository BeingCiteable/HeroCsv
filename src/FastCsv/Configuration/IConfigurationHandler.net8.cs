#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Frozen;

namespace FastCsv.Configuration;

/// <summary>
/// Advanced configuration enhancements for IConfigurationHandler
/// </summary>
public partial interface IConfigurationHandler
{
    /// <summary>
    /// Get all available preset configurations
    /// </summary>
    FrozenDictionary<string, CsvOptions> GetAllPresets();

    /// <summary>
    /// Detect format with custom delimiters
    /// </summary>
    CsvOptions DetectFormat(ReadOnlySpan<char> sampleData, SearchValues<char> delimiters);

    /// <summary>
    /// Get delimiters for current configuration
    /// </summary>
    SearchValues<char> GetDelimiters();
}
#endif