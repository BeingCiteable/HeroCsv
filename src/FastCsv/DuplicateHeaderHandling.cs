namespace FastCsv;

/// <summary>
/// Specifies how duplicate column headers should be handled when reading CSV with headers
/// </summary>
public enum DuplicateHeaderHandling
{
    /// <summary>
    /// Throw an exception when duplicate headers are encountered (default behavior)
    /// </summary>
    ThrowException = 0,
    
    /// <summary>
    /// Keep only the first occurrence of duplicate headers, ignore subsequent ones
    /// </summary>
    KeepFirst = 1,
    
    /// <summary>
    /// Keep only the last occurrence of duplicate headers, overwrite previous values
    /// </summary>
    KeepLast = 2,
    
    /// <summary>
    /// Automatically rename duplicate headers by appending a number (e.g., "Name", "Name_2", "Name_3")
    /// </summary>
    MakeUnique = 3,
    
    /// <summary>
    /// Skip records that have duplicate headers entirely
    /// </summary>
    SkipRecord = 4
}