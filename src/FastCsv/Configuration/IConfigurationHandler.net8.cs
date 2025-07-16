#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Buffers;

namespace FastCsv.Configuration;

/// <summary>
/// .NET 8+ configuration enhancements for IConfigurationHandler
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