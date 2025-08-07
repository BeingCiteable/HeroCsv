using System;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// Defines a strategy for parsing CSV lines into field arrays
/// </summary>
public interface IParsingStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given line format
    /// </summary>
    bool CanHandle(ReadOnlySpan<char> line, CsvOptions options);
    
    /// <summary>
    /// Parses a CSV line into an array of field values
    /// </summary>
    string[] Parse(ReadOnlySpan<char> line, CsvOptions options);
    
    /// <summary>
    /// Gets the priority of this strategy (higher = checked first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets whether this strategy is available in the current runtime
    /// </summary>
    bool IsAvailable { get; }
}