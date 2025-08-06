namespace HeroCsv.Exceptions;

/// <summary>
/// Exception thrown when attempting to access a CSV field with an invalid index
/// </summary>
/// <remarks>
/// Creates a new instance of CsvFieldIndexOutOfRangeException
/// </remarks>
public class CsvFieldIndexOutOfRangeException(int attemptedIndex, int actualFieldCount, string rowPreview) : ArgumentOutOfRangeException("index", attemptedIndex,
        $"Field index {attemptedIndex} is out of range. Row has {actualFieldCount} fields. Row content: {rowPreview}")
{
    /// <summary>
    /// Gets the actual number of fields in the row
    /// </summary>
    public int ActualFieldCount { get; } = actualFieldCount;

    /// <summary>
    /// Gets the attempted index
    /// </summary>
    public int AttemptedIndex { get; } = attemptedIndex;

    /// <summary>
    /// Gets a preview of the row content
    /// </summary>
    public string RowPreview { get; } = rowPreview;
}