namespace FastCsv.Navigation;

/// <summary>
/// Handler responsible for position tracking during CSV reading
/// Core responsibility: Track current position, line numbers, and record counts
/// </summary>
public partial interface IPositionHandler
{
    /// <summary>
    /// Current position in the data
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Current line number (1-based)
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Total number of records processed
    /// </summary>
    long RecordCount { get; }

    /// <summary>
    /// Move to the specified position
    /// </summary>
    void MoveTo(int position);

    /// <summary>
    /// Increment the line number
    /// </summary>
    void IncrementLine();

    /// <summary>
    /// Increment the record count
    /// </summary>
    void IncrementRecord();

    /// <summary>
    /// Reset to the beginning
    /// </summary>
    void Reset();
}