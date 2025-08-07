using System;

namespace HeroCsv.Attributes;

/// <summary>
/// Marks a class for CSV mapping source generation, enabling AOT-safe and reflection-free CSV parsing
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateCsvMappingAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether the CSV has headers (default: true)
    /// </summary>
    public bool HasHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the delimiter character (default: ',')
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets the quote character (default: '"')
    /// </summary>
    public char Quote { get; set; } = '"';

    /// <summary>
    /// Gets or sets whether to skip empty lines (default: true)
    /// </summary>
    public bool SkipEmptyLines { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow comments (default: false)
    /// </summary>
    public bool AllowComments { get; set; }

    /// <summary>
    /// Gets or sets the comment character (default: '#')
    /// </summary>
    public char CommentCharacter { get; set; } = '#';
}