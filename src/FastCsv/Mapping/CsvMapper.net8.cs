#if NET8_0_OR_GREATER
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// NET8+ optimizations for CsvMapper with frozen collections
/// </summary>
internal sealed partial class CsvMapper<T> where T : class, new()
{
    private FrozenDictionary<string, PropertyInfo>? _frozenPropertyMap;
    private FrozenDictionary<int, PropertyInfo>? _frozenIndexMap;
    private FrozenDictionary<int, Func<string, object?>>? _frozenConverters;

    /// <summary>
    /// Optimizes mapping performance by creating frozen collections
    /// </summary>
    public void OptimizeForRepeatedUse()
    {
        _frozenPropertyMap = _propertyMap.ToFrozenDictionary();
        _frozenIndexMap = _indexMap.ToFrozenDictionary();
        _frozenConverters = _converters.ToFrozenDictionary();
    }

    /// <summary>
    /// Maps a CSV record using frozen collections for maximum performance
    /// </summary>
    /// <param name="record">CSV record as string array</param>
    /// <returns>Mapped object instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T MapRecordFast(string[] record)
    {
        var instance = new T();
        var indexMap = _frozenIndexMap ?? (IReadOnlyDictionary<int, PropertyInfo>)_indexMap;
        var converters = _frozenConverters ?? (IReadOnlyDictionary<int, Func<string, object?>>)_converters;

        // Use frozen collections for optimal lookup performance
        foreach (var kvp in indexMap)
        {
            var index = kvp.Key;
            var property = kvp.Value;

            if (index < record.Length)
            {
                var value = record[index];
                if (!string.IsNullOrEmpty(value) || !_options.SkipEmptyFields)
                {
                    var convertedValue = converters.TryGetValue(index, out var converter)
                        ? converter(value)
                        : ConvertValue(index, value, property.PropertyType);
                    property.SetValue(instance, convertedValue);
                }
            }
        }

        return instance;
    }

    /// <summary>
    /// Creates a mapping builder with frozen collections support
    /// </summary>
    /// <returns>Mapping builder optimized for NET8+</returns>
    public static CsvMappingBuilder<T> CreateBuilder()
    {
        return new CsvMappingBuilder<T>();
    }
}

/// <summary>
/// Builder for creating optimized CSV mappings with frozen collections
/// </summary>
public sealed class CsvMappingBuilder<T> where T : class, new()
{
    private readonly CsvMapping<T> _mapping = new();

    /// <summary>
    /// Maps a property to a column by name
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <returns>This builder instance for fluent configuration</returns>
    public CsvMappingBuilder<T> MapProperty(string propertyName, string columnName)
    {
        _mapping.MapProperty(propertyName, columnName);
        return this;
    }

    /// <summary>
    /// Maps a property to a column by index
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <returns>This builder instance for fluent configuration</returns>
    public CsvMappingBuilder<T> MapProperty(string propertyName, int columnIndex)
    {
        _mapping.MapProperty(propertyName, columnIndex);
        return this;
    }

    /// <summary>
    /// Maps a property to a column with custom converter
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <param name="converter">Custom converter function</param>
    /// <returns>This builder instance for fluent configuration</returns>
    public CsvMappingBuilder<T> MapProperty(string propertyName, string columnName, Func<string, object?> converter)
    {
        _mapping.MapProperty(propertyName, columnName, converter);
        return this;
    }

    /// <summary>
    /// Maps a property to a column with custom converter by index
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <param name="converter">Custom converter function</param>
    /// <returns>This builder instance for fluent configuration</returns>
    public CsvMappingBuilder<T> MapProperty(string propertyName, int columnIndex, Func<string, object?> converter)
    {
        _mapping.MapProperty(propertyName, columnIndex, converter);
        return this;
    }

    /// <summary>
    /// Sets the CSV parsing options
    /// </summary>
    /// <param name="options">CSV parsing options</param>
    /// <returns>This builder instance for fluent configuration</returns>
    public CsvMappingBuilder<T> WithOptions(CsvOptions options)
    {
        _mapping.Options = options;
        return this;
    }

    /// <summary>
    /// Builds the mapping configuration
    /// </summary>
    /// <returns>Configured CSV mapping</returns>
    public CsvMapping<T> Build()
    {
        return _mapping;
    }
}
#endif