namespace HeroCsv.Mapping;

/// <summary>
/// Factory for creating CSV mapping configurations
/// </summary>
public static class CsvMapping
{
    /// <summary>
    /// Creates a new mapping instance for the specified type
    /// </summary>
    /// <typeparam name="T">Type to map</typeparam>
    /// <returns>New mapping instance</returns>
    public static CsvMapping<T> Create<T>() where T : class, new() => new();

    /// <summary>
    /// Creates a new mapping instance with auto mapping and manual overrides enabled
    /// </summary>
    /// <typeparam name="T">Type to map</typeparam>
    /// <returns>New mapping instance with auto mapping enabled</returns>
    public static CsvMapping<T> CreateAutoMapWithOverrides<T>() where T : class, new()
    {
        return new CsvMapping<T> { UseAutoMapWithOverrides = true };
    }
}