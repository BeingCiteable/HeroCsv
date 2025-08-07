using System;
using HeroCsv.Core;
using HeroCsv.Errors;
using HeroCsv.Models;
using HeroCsv.Validation;

namespace HeroCsv.Configuration;

/// <summary>
/// Configuration for CSV reader instances, replacing multiple constructor parameters
/// </summary>
public sealed class CsvReaderConfiguration
{
    /// <summary>
    /// CSV parsing options
    /// </summary>
    public CsvOptions Options { get; set; } = CsvOptions.Default;
    
    /// <summary>
    /// Error handler for tracking parsing errors
    /// </summary>
    public IErrorHandler? ErrorHandler { get; set; }
    
    /// <summary>
    /// Validation handler for data validation
    /// </summary>
    public IValidationHandler? ValidationHandler { get; set; }
    
    /// <summary>
    /// String pool for deduplication
    /// </summary>
    public Utilities.StringPool? StringPool { get; set; }
    
    /// <summary>
    /// String builder pool for reducing allocations
    /// </summary>
    public StringBuilderPool? StringBuilderPool { get; set; }
    
    /// <summary>
    /// Callback for error notifications
    /// </summary>
    public Action<CsvValidationError>? ErrorCallback { get; set; }
    
    /// <summary>
    /// Whether to enable validation
    /// </summary>
    public bool EnableValidation { get; set; }
    
    /// <summary>
    /// Whether to track errors
    /// </summary>
    public bool TrackErrors { get; set; }
    
    /// <summary>
    /// Creates a default configuration
    /// </summary>
    public static CsvReaderConfiguration Default => new();
    
    /// <summary>
    /// Creates a configuration with validation enabled
    /// </summary>
    public static CsvReaderConfiguration WithValidation => new() 
    { 
        EnableValidation = true,
        TrackErrors = true 
    };
    
    /// <summary>
    /// Creates a high-performance configuration
    /// </summary>
    public static CsvReaderConfiguration HighPerformance => new()
    {
        StringPool = new Utilities.StringPool(),
        StringBuilderPool = new StringBuilderPool(),
        EnableValidation = false,
        TrackErrors = false
    };
}