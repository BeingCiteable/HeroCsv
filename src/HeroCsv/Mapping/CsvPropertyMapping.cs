namespace HeroCsv.Mapping;

/// <summary>
/// Individual property mapping configuration
/// </summary>
public sealed class CsvPropertyMapping
{
    /// <summary>
    /// Name of the property to map to
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the CSV column to map from
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Index of the CSV column to map from
    /// </summary>
    public int? ColumnIndex { get; set; }

    /// <summary>
    /// Custom converter function for this property
    /// </summary>
    public Func<string, object?>? Converter { get; set; }
}