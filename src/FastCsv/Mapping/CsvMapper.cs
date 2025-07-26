using System.Reflection;
using System.Runtime.CompilerServices;
using FastCsv.Core;
using FastCsv.Models;

namespace FastCsv.Mapping;

/// <summary>
/// High-performance CSV to object mapper with auto, manual, and mixed mapping support
/// </summary>
/// <typeparam name="T">Type to map CSV records to</typeparam>
internal sealed partial class CsvMapper<T> where T : class, new()
{
    private readonly CsvMapping<T>? _mapping;
    private readonly CsvOptions _options;
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly Dictionary<int, PropertyInfo> _indexMap;
    private readonly Dictionary<int, Func<string, object?>> _converters;
    private string[]? _headers;

    /// <summary>
    /// Creates a mapper with auto mapping using property names
    /// </summary>
    /// <param name="options">CSV parsing options</param>
    public CsvMapper(CsvOptions options)
    {
        _options = options;
        _propertyMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        _indexMap = [];
        _converters = [];
        InitializeAutoMapping();
    }

    /// <summary>
    /// Creates a mapper with manual mapping configuration
    /// </summary>
    /// <param name="mapping">Manual mapping configuration</param>
    public CsvMapper(CsvMapping<T> mapping)
    {
        _mapping = mapping;
        _options = mapping.Options;
        _propertyMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        _indexMap = [];
        _converters = [];

        // Initialize mixed mapping - auto mapping first, then manual overrides
        if (mapping.UseMixedMapping)
        {
            InitializeAutoMapping();
        }
        InitializeManualMapping();
    }

    /// <summary>
    /// Sets the headers for column name mapping
    /// </summary>
    /// <param name="headers">Array of column headers</param>
    public void SetHeaders(string[] headers)
    {
        _headers = headers;
        UpdateMappingWithHeaders();
    }

    /// <summary>
    /// Maps a CSV record to an object instance
    /// </summary>
    /// <param name="record">CSV record as string array</param>
    /// <returns>Mapped object instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T MapRecord(string[] record)
    {
        var instance = new T();

        // Use index-based mapping for performance
        foreach (var kvp in _indexMap)
        {
            var index = kvp.Key;
            var property = kvp.Value;

            if (index < record.Length)
            {
                var value = record[index];
                if (!string.IsNullOrEmpty(value) || !_options.SkipEmptyFields)
                {
                    var convertedValue = ConvertValue(index, value, property.PropertyType);
                    property.SetValue(instance, convertedValue);
                }
            }
        }

        return instance;
    }

    /// <summary>
    /// Initialize auto mapping using property names
    /// </summary>
    private void InitializeAutoMapping()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        foreach (var property in properties)
        {
            _propertyMap[property.Name] = property;
        }
    }

    /// <summary>
    /// Initialize manual mapping using provided configuration
    /// </summary>
    private void InitializeManualMapping()
    {
        if (_mapping == null) return;

        foreach (var mapping in _mapping.PropertyMappings)
        {
            var property = typeof(T).GetProperty(mapping.PropertyName);
            if (property != null && property.CanWrite)
            {
                if (mapping.ColumnIndex.HasValue)
                {
                    _indexMap[mapping.ColumnIndex.Value] = property;
                    if (mapping.Converter != null)
                    {
                        _converters[mapping.ColumnIndex.Value] = mapping.Converter;
                    }
                }
                else if (!string.IsNullOrEmpty(mapping.ColumnName))
                {
                    _propertyMap[mapping.ColumnName!] = property;
                }
            }
        }
    }

    /// <summary>
    /// Update mapping when headers are provided
    /// </summary>
    private void UpdateMappingWithHeaders()
    {
        if (_headers == null) return;

        // For auto mapping, map headers to properties
        if (_mapping == null)
        {
            _indexMap.Clear();
            for (int i = 0; i < _headers.Length; i++)
            {
                var header = _headers[i];
                if (!string.IsNullOrEmpty(header) && _propertyMap.TryGetValue(header, out var property))
                {
                    _indexMap[i] = property;
                }
            }
        }
        else
        {
            // For manual mapping, update column name mappings to use indices
            foreach (var mapping in _mapping.PropertyMappings)
            {
                if (!string.IsNullOrEmpty(mapping.ColumnName) && !mapping.ColumnIndex.HasValue)
                {
                    var index = _headers != null ? Array.IndexOf(_headers, mapping.ColumnName) : -1;
                    if (index >= 0)
                    {
                        var property = typeof(T).GetProperty(mapping.PropertyName);
                        if (property != null && property.CanWrite)
                        {
                            _indexMap[index] = property;
                            if (mapping.Converter != null)
                            {
                                _converters[index] = mapping.Converter;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert string value to target type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? ConvertValue(int index, string value, Type targetType)
    {
        // Use custom converter if available
        if (_converters.TryGetValue(index, out var converter))
        {
            return converter(value);
        }

        // Handle null/empty values
        if (string.IsNullOrEmpty(value))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            targetType = underlyingType;
        }

        // Common type conversions
        return targetType.Name switch
        {
            nameof(String) => value,
            nameof(Byte) => byte.Parse(value),
            nameof(Int32) => int.Parse(value),
            nameof(Int64) => long.Parse(value),
            nameof(Double) => double.Parse(value),
            nameof(Decimal) => decimal.Parse(value),
            nameof(Boolean) => bool.Parse(value),
            nameof(DateTime) => DateTime.Parse(value),
            nameof(DateTimeOffset) => DateTimeOffset.Parse(value),
            nameof(Guid) => Guid.Parse(value),
            _ => Convert.ChangeType(value, targetType)
        };
    }
}

/// <summary>
/// Configuration for manual CSV mapping
/// </summary>
/// <typeparam name="T">Type to map CSV records to</typeparam>
public sealed class CsvMapping<T> where T : class, new()
{
    /// <summary>
    /// CSV parsing options
    /// </summary>
    public CsvOptions Options { get; set; } = CsvOptions.Default;

    private readonly List<CsvPropertyMapping> _propertyMappings = new();

    /// <summary>
    /// Property mapping configurations
    /// </summary>
    public IReadOnlyList<CsvPropertyMapping> PropertyMappings => _propertyMappings;

    /// <summary>
    /// Whether to use mixed mapping (auto mapping with manual overrides)
    /// </summary>
    public bool UseMixedMapping { get; set; } = false;

    /// <summary>
    /// Maps a property to a column by name
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> MapProperty(string propertyName, string columnName)
    {
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = propertyName,
            ColumnName = columnName
        });
        return this;
    }

    /// <summary>
    /// Maps a property to a column by index
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> MapProperty(string propertyName, int columnIndex)
    {
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = propertyName,
            ColumnIndex = columnIndex
        });
        return this;
    }

    /// <summary>
    /// Maps a property to a column with custom converter
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <param name="converter">Custom converter function</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> MapProperty(string propertyName, string columnName, Func<string, object?> converter)
    {
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = propertyName,
            ColumnName = columnName,
            Converter = converter
        });
        return this;
    }

    /// <summary>
    /// Maps a property to a column with custom converter by index
    /// </summary>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <param name="converter">Custom converter function</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> MapProperty(string propertyName, int columnIndex, Func<string, object?> converter)
    {
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = propertyName,
            ColumnIndex = columnIndex,
            Converter = converter
        });
        return this;
    }

    /// <summary>
    /// Enables mixed mapping (auto mapping with manual overrides)
    /// </summary>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> UseMixed()
    {
        UseMixedMapping = true;
        return this;
    }

    /// <summary>
    /// Creates a new mapping instance
    /// </summary>
    /// <returns>New mapping instance</returns>
    public static CsvMapping<T> Create() => new();

    /// <summary>
    /// Creates a new mixed mapping instance (auto mapping with manual overrides)
    /// </summary>
    /// <returns>New mixed mapping instance</returns>
    public static CsvMapping<T> CreateMixed()
    {
        return new CsvMapping<T> { UseMixedMapping = true };
    }
}

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