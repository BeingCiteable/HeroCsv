namespace FastCsv.Configuration;

/// <summary>
/// Handler responsible for CSV configuration management
/// Core responsibility: Manage CSV parsing options and format detection
/// </summary>
public partial interface IConfigurationHandler
{
    /// <summary>
    /// Current CSV options
    /// </summary>
    CsvOptions Options { get; }

    /// <summary>
    /// Auto-detect CSV format from sample data
    /// </summary>
    CsvOptions DetectFormat(ReadOnlySpan<char> sampleData);

    /// <summary>
    /// Get preset options by name (e.g., "excel", "tab", "pipe")
    /// </summary>
    CsvOptions GetPresetOptions(string presetName);

    /// <summary>
    /// Validate that the options are consistent
    /// </summary>
    bool ValidateOptions(CsvOptions options);
}