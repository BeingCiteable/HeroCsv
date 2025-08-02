using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastCsv.Core;
using FastCsv.Mapping.Attributes;
using FastCsv.Mapping.Converters;
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
    private readonly Dictionary<string, List<PropertyInfo>> _propertyMap;
    private readonly Dictionary<int, List<PropertyInfo>> _indexMap;
    private readonly Dictionary<int, Func<string, object?>> _converters;
    private readonly Dictionary<PropertyInfo, CsvColumnAttribute?> _attributeCache;
    private readonly Dictionary<PropertyInfo, ICsvConverter?> _converterCache;
    private string[]? _headers;

    /// <summary>
    /// Creates a mapper with auto mapping using property names
    /// </summary>
    /// <param name="options">CSV parsing options</param>
    public CsvMapper(CsvOptions options)
    {
        _options = options;
        _propertyMap = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);
        _indexMap = new Dictionary<int, List<PropertyInfo>>();
        _converters = [];
        _attributeCache = [];
        _converterCache = [];
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
        _propertyMap = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);
        _indexMap = new Dictionary<int, List<PropertyInfo>>();
        _converters = [];
        _attributeCache = [];
        _converterCache = [];

        // Initialize auto mapping with overrides - auto mapping first, then manual overrides
        if (mapping.UseAutoMapWithOverrides)
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
            var properties = kvp.Value;

            if (index < record.Length)
            {
                var value = record[index];
                
                // Map the same value to all properties that map to this index
                foreach (var property in properties)
                {
                    if (!string.IsNullOrEmpty(value) || !_options.SkipEmptyFields)
                    {
                        var convertedValue = ConvertValue(index, value, property);
                        property.SetValue(instance, convertedValue);
                    }
                    else
                    {
                        // Check for default value from mapping or attribute
                        object? defaultValue;
                        if (_mapping?.TryGetDefault(property.Name, out defaultValue) == true && defaultValue != null)
                        {
                            property.SetValue(instance, defaultValue);
                        }
                        else
                        {
                            // Check for attribute default value
                            var attribute = GetCsvColumnAttribute(property);
                            if (attribute?.Default != null)
                            {
                                property.SetValue(instance, attribute.Default);
                            }
                        }
                    }
                }
            }
        }

        return instance;
    }

    /// <summary>
    /// Initialize auto mapping using property names and attributes
    /// </summary>
    private void InitializeAutoMapping()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true && !IsIgnoredByAttribute(p))
            .ToArray();

        foreach (var property in properties)
        {
            var attribute = GetCsvColumnAttribute(property);
            
            // If attribute specifies index, use that
            if (attribute?.HasIndex == true)
            {
                if (!_indexMap.ContainsKey(attribute.Index))
                {
                    _indexMap[attribute.Index] = new List<PropertyInfo>();
                }
                _indexMap[attribute.Index].Add(property);
                
                // Also set up converter if available
                var converter = GetConverter(property);
                if (converter != null)
                {
                    _converters[attribute.Index] = value => converter.ConvertFromString(value, property.PropertyType, attribute.Format);
                }
            }
            // If attribute specifies name, use that name
            else if (attribute?.Name is { } attributeName && !string.IsNullOrEmpty(attributeName))
            {
                if (!_propertyMap.ContainsKey(attributeName))
                {
                    _propertyMap[attributeName] = new List<PropertyInfo>();
                }
                _propertyMap[attributeName].Add(property);
            }
            // Otherwise use property name
            else
            {
                if (!_propertyMap.ContainsKey(property.Name))
                {
                    _propertyMap[property.Name] = new List<PropertyInfo>();
                }
                _propertyMap[property.Name].Add(property);
            }
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
            if (property != null && property.CanWrite && property.SetMethod?.IsPublic == true)
            {
                if (mapping.ColumnIndex.HasValue)
                {
                    if (!_indexMap.ContainsKey(mapping.ColumnIndex.Value))
                    {
                        _indexMap[mapping.ColumnIndex.Value] = new List<PropertyInfo>();
                    }
                    _indexMap[mapping.ColumnIndex.Value].Add(property);
                    
                    if (mapping.Converter != null)
                    {
                        _converters[mapping.ColumnIndex.Value] = mapping.Converter;
                    }
                }
                else if (!string.IsNullOrEmpty(mapping.ColumnName))
                {
                    if (!_propertyMap.ContainsKey(mapping.ColumnName!))
                    {
                        _propertyMap[mapping.ColumnName!] = new List<PropertyInfo>();
                    }
                    _propertyMap[mapping.ColumnName!].Add(property);
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

        // For auto mapping or auto mapping with overrides, map headers to properties
        if (_mapping == null || _mapping.UseAutoMapWithOverrides)
        {
            // Only clear index map if we're not using attribute-based index mapping
            // Preserve any index mappings that were set by attributes
            var attributeIndexMappings = new Dictionary<int, List<PropertyInfo>>(_indexMap);
            _indexMap.Clear();
            
            // First, restore attribute-based index mappings
            foreach (var kvp in attributeIndexMappings)
            {
                _indexMap[kvp.Key] = kvp.Value;
            }
            
            // Then, add header-based mappings (but don't override attribute mappings)
            for (int i = 0; i < _headers.Length; i++)
            {
                var header = _headers[i];
                if (!string.IsNullOrEmpty(header) && _propertyMap.TryGetValue(header, out var properties) && !_indexMap.ContainsKey(i))
                {
                    _indexMap[i] = new List<PropertyInfo>(properties);
                    
                    // Set up converter if available through attribute for the first property
                    // (converters should be the same for properties mapping to the same column)
                    if (properties.Count > 0)
                    {
                        var converter = GetConverter(properties[0]);
                        if (converter != null)
                        {
                            var attribute = GetCsvColumnAttribute(properties[0]);
                            _converters[i] = value => converter.ConvertFromString(value, properties[0].PropertyType, attribute?.Format);
                        }
                    }
                }
            }
        }
        
        // For manual or auto mapping with overrides, apply manual mappings (overrides auto mappings)
        if (_mapping != null)
        {
            // Update column name mappings to use indices
            foreach (var mapping in _mapping.PropertyMappings)
            {
                if (!string.IsNullOrEmpty(mapping.ColumnName) && !mapping.ColumnIndex.HasValue)
                {
                    var index = _headers != null ? Array.IndexOf(_headers, mapping.ColumnName) : -1;
                    if (index >= 0)
                    {
                        var property = typeof(T).GetProperty(mapping.PropertyName);
                        if (property != null && property.CanWrite && property.SetMethod?.IsPublic == true)
                        {
                            if (!_indexMap.ContainsKey(index))
                            {
                                _indexMap[index] = new List<PropertyInfo>();
                            }
                            _indexMap[index].Add(property);
                            
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
    private object? ConvertValue(int index, string value, PropertyInfo property)
    {
        var targetType = property.PropertyType;
        
        // Use custom converter if available
        if (_converters.TryGetValue(index, out var converter))
        {
            return converter(value);
        }

        // Check for attribute converter
        var attributeConverter = GetConverter(property);
        if (attributeConverter != null)
        {
            var attribute = GetCsvColumnAttribute(property);
            return attributeConverter.ConvertFromString(value, targetType, attribute?.Format);
        }

        // Handle nullable types first
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var isNullable = underlyingType != null;
        if (isNullable)
        {
            // For nullable types, treat empty string or "null" as null
            if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            targetType = underlyingType!;
        }

        // Handle null/empty values for non-nullable types
        if (string.IsNullOrEmpty(value))
        {
            // Check for fluent mapping default first
            object? defaultValue;
            if (_mapping?.TryGetDefault(property.Name, out defaultValue) == true && defaultValue != null)
            {
                return defaultValue;
            }
            
            // Check for attribute default
            var attribute = GetCsvColumnAttribute(property);
            if (attribute?.Default != null)
            {
                return attribute.Default;
            }
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // Check for unsupported array types
        if (targetType.IsArray)
        {
            return null; // Arrays are not supported, return null
        }

        // Common type conversions
        return targetType.Name switch
        {
            nameof(String) => value,
            nameof(Byte) => byte.Parse(value),
            nameof(SByte) => sbyte.Parse(value),
            nameof(Int16) => short.Parse(value),
            nameof(UInt16) => ushort.Parse(value),
            nameof(Int32) => int.Parse(value),
            nameof(UInt32) => uint.Parse(value),
            nameof(Int64) => long.Parse(value),
            nameof(UInt64) => ulong.Parse(value),
            nameof(Single) => float.Parse(value),
            nameof(Double) => double.Parse(value),
            nameof(Decimal) => decimal.Parse(value),
            nameof(Boolean) => bool.Parse(value),
            nameof(Char) => value.Length > 0 ? value[0] : '\0',
            nameof(DateTime) => ParseDateTime(value, property),
            nameof(DateTimeOffset) => ParseDateTimeOffset(value, property),
            nameof(Guid) => Guid.Parse(value),
            nameof(TimeSpan) => TimeSpan.Parse(value),
            _ => targetType.IsEnum ? 
                ParseEnum(targetType, value) 
                : TryConvertType(value, targetType)
        };
    }

    /// <summary>
    /// Try to convert type with fallback for unsupported types
    /// </summary>
    private static object? TryConvertType(string value, Type targetType)
    {
        try
        {
            // Check if type is a collection or complex type
            if (targetType.IsGenericType)
            {
                var genericTypeDef = targetType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) || 
                    genericTypeDef == typeof(Dictionary<,>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(IDictionary<,>) ||
                    genericTypeDef == typeof(IEnumerable<>))
                {
                    // Return default instance for collection types
                    return Activator.CreateInstance(targetType);
                }
            }
            
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            // Return default value for unsupported types
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }

    /// <summary>
    /// Parse enum with error handling for invalid values
    /// </summary>
    private static object ParseEnum(Type enumType, string value)
    {
        try
        {
            return Enum.Parse(enumType, value, true); // ignoreCase = true
        }
        catch (ArgumentException)
        {
            // Return default value for invalid enum values
            return Activator.CreateInstance(enumType)!;
        }
    }

    /// <summary>
    /// Parse DateTime with format support
    /// </summary>
    private DateTime ParseDateTime(string value, PropertyInfo property)
    {
        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);
        
        if (format != null)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            throw new FormatException($"Unable to parse '{value}' as DateTime using format '{format}'");
        }
        
        return DateTime.Parse(value);
    }
    
    /// <summary>
    /// Parse DateTimeOffset with format support
    /// </summary>
    private DateTimeOffset ParseDateTimeOffset(string value, PropertyInfo property)
    {
        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);
        
        if (format != null)
        {
            if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
                return dto;
            throw new FormatException($"Unable to parse '{value}' as DateTimeOffset using format '{format}'");
        }
        
        return DateTimeOffset.Parse(value);
    }

    /// <summary>
    /// Checks if a property is ignored by attribute
    /// </summary>
    private bool IsIgnoredByAttribute(PropertyInfo property)
    {
        var attribute = GetCsvColumnAttribute(property);
        return attribute?.Ignore ?? false;
    }

    /// <summary>
    /// Gets the CsvColumnAttribute for a property (cached)
    /// </summary>
    private CsvColumnAttribute? GetCsvColumnAttribute(PropertyInfo property)
    {
        if (!_attributeCache.TryGetValue(property, out var attribute))
        {
            attribute = property.GetCustomAttribute<CsvColumnAttribute>();
            _attributeCache[property] = attribute;
        }
        return attribute;
    }

    /// <summary>
    /// Gets the converter for a property (cached)
    /// </summary>
    private ICsvConverter? GetConverter(PropertyInfo property)
    {
        if (!_converterCache.TryGetValue(property, out var converter))
        {
            var converterAttribute = property.GetCustomAttribute<CsvConverterAttribute>();
            if (converterAttribute != null)
            {
                converter = Activator.CreateInstance(converterAttribute.ConverterType) as ICsvConverter;
                _converterCache[property] = converter;
            }
        }
        return converter;
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
    private readonly HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _defaultValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _formats = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _requiredProperties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Property mapping configurations
    /// </summary>
    public IReadOnlyList<CsvPropertyMapping> PropertyMappings => _propertyMappings;

    /// <summary>
    /// Whether to use auto mapping with manual overrides
    /// </summary>
    public bool UseAutoMapWithOverrides { get; set; } = false;

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
    /// Enables auto mapping with manual overrides
    /// </summary>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> EnableAutoMapWithOverrides()
    {
        UseAutoMapWithOverrides = true;
        return this;
    }

    /// <summary>
    /// Creates a new mapping instance
    /// </summary>
    /// <returns>New mapping instance</returns>
    public static CsvMapping<T> Create() => new();

    /// <summary>
    /// Creates a new mapping instance with auto mapping and manual overrides enabled
    /// </summary>
    /// <returns>New mapping instance with auto mapping enabled</returns>
    public static CsvMapping<T> CreateAutoMapWithOverrides()
    {
        return new CsvMapping<T> { UseAutoMapWithOverrides = true };
    }

    /// <summary>
    /// Ignores a property during mapping
    /// </summary>
    /// <param name="propertyName">Name of the property to ignore</param>
    public void IgnoreProperty(string propertyName)
    {
        _ignoredProperties.Add(propertyName);
    }

    /// <summary>
    /// Checks if a property should be ignored
    /// </summary>
    /// <param name="propertyName">Property name to check</param>
    /// <returns>True if property should be ignored</returns>
    public bool IsPropertyIgnored(string propertyName)
    {
        return _ignoredProperties.Contains(propertyName);
    }

    /// <summary>
    /// Sets a default value for a property
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="defaultValue">Default value</param>
    public void SetDefault(string propertyName, object? defaultValue)
    {
        _defaultValues[propertyName] = defaultValue;
    }

    /// <summary>
    /// Gets the default value for a property
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="defaultValue">Default value if found</param>
    /// <returns>True if default value exists</returns>
    public bool TryGetDefault(string propertyName, out object? defaultValue)
    {
        return _defaultValues.TryGetValue(propertyName, out defaultValue);
    }

    /// <summary>
    /// Sets a format string for a property
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="format">Format string</param>
    public void SetFormat(string propertyName, string format)
    {
        _formats[propertyName] = format;
    }

    /// <summary>
    /// Gets the format string for a property
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="format">Format string if found</param>
    /// <returns>True if format exists</returns>
    public bool TryGetFormat(string propertyName, out string? format)
    {
        if (_formats.TryGetValue(propertyName, out var f))
        {
            format = f;
            return true;
        }
        format = null;
        return false;
    }

    /// <summary>
    /// Sets a property as required
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="required">Whether the property is required</param>
    public void SetRequired(string propertyName, bool required)
    {
        if (required)
            _requiredProperties.Add(propertyName);
        else
            _requiredProperties.Remove(propertyName);
    }

    /// <summary>
    /// Checks if a property is required
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <returns>True if property is required</returns>
    public bool IsPropertyRequired(string propertyName)
    {
        return _requiredProperties.Contains(propertyName);
    }

    /// <summary>
    /// Sets a custom converter for a property
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="converter">Converter function</param>
    public void SetConverter(string propertyName, Func<string, object?> converter)
    {
        var existing = _propertyMappings.FirstOrDefault(m => m.PropertyName == propertyName);
        if (existing != null)
        {
            existing.Converter = converter;
        }
        else
        {
            _propertyMappings.Add(new CsvPropertyMapping
            {
                PropertyName = propertyName,
                Converter = converter
            });
        }
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