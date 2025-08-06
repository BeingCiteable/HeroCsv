using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HeroCsv.Core;
using HeroCsv.Mapping.Attributes;
using HeroCsv.Mapping.Converters;
using HeroCsv.Models;

namespace HeroCsv.Mapping;

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
    private readonly Dictionary<string, CultureInfo> _fieldCultures;
    private string[]? _headers;
    private CultureInfo _culture;

    /// <summary>
    /// Creates a mapper with auto mapping using property names
    /// </summary>
    /// <param name="options">CSV parsing options</param>
    /// <param name="culture">Culture for parsing (defaults to current culture)</param>
    public CsvMapper(CsvOptions options, CultureInfo? culture = null)
    {
        _options = options;
        _culture = culture ?? CultureInfo.CurrentCulture;
        _propertyMap = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);
        _indexMap = [];
        _converters = [];
        _attributeCache = [];
        _converterCache = [];
        _fieldCultures = [];
        InitializeAutoMapping();
    }

    /// <summary>
    /// Creates a mapper with manual mapping configuration
    /// </summary>
    /// <param name="mapping">Manual mapping configuration</param>
    /// <param name="culture">Culture for parsing (defaults to current culture)</param>
    public CsvMapper(CsvMapping<T> mapping, CultureInfo? culture = null)
    {
        _mapping = mapping;
        _options = mapping.Options;
        _culture = culture ?? CultureInfo.CurrentCulture;
        _propertyMap = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);
        _indexMap = [];
        _converters = [];
        _attributeCache = [];
        _converterCache = [];
        _fieldCultures = [];

        // Initialize auto mapping with overrides - auto mapping first, then manual overrides
        if (mapping.UseAutoMapWithOverrides)
        {
            InitializeAutoMapping();
        }
        InitializeManualMapping();
    }

    /// <summary>
    /// Sets the culture for a specific field
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="culture">Culture to use for this field</param>
    public void SetFieldCulture(string fieldName, CultureInfo culture)
    {
        _fieldCultures[fieldName] = culture;
    }

    /// <summary>
    /// Sets the default culture for all fields
    /// </summary>
    /// <param name="culture">Culture to use</param>
    public void SetCulture(CultureInfo culture)
    {
        _culture = culture;
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
                        try
                        {
                            var convertedValue = ConvertValue(index, value, property);
                            property.SetValue(instance, convertedValue);
                        }
                        catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is ArgumentException)
                        {
                            // For conversion errors in built-in conversions, use default value
                            var targetType = property.PropertyType;
                            var underlyingType = Nullable.GetUnderlyingType(targetType);
                            var defaultValue = underlyingType != null
                                ? null
                                : (targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
                            property.SetValue(instance, defaultValue);
                        }
                    }
                    else
                    {
                        // Check for default value from mapping or attribute
                        if (_mapping?.TryGetDefault(property.Name, out object? defaultValue) == true && defaultValue != null)
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
                if (!_indexMap.TryGetValue(attribute.Index, out var indexList))
                {
                    indexList = [];
                    _indexMap[attribute.Index] = indexList;
                }
                indexList.Add(property);

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
                if (!_propertyMap.TryGetValue(attributeName, out var propList))
                {
                    propList = [];
                    _propertyMap[attributeName] = propList;
                }
                propList.Add(property);
            }
            // Otherwise use property name
            else
            {
                if (!_propertyMap.TryGetValue(property.Name, out var propList))
                {
                    propList = [];
                    _propertyMap[property.Name] = propList;
                }
                propList.Add(property);
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
                    if (!_indexMap.TryGetValue(mapping.ColumnIndex.Value, out var indexList))
                    {
                        indexList = [];
                        _indexMap[mapping.ColumnIndex.Value] = indexList;
                    }
                    indexList.Add(property);

                    if (mapping.Converter != null)
                    {
                        _converters[mapping.ColumnIndex.Value] = mapping.Converter;
                    }
                }
                else if (!string.IsNullOrEmpty(mapping.ColumnName))
                {
                    if (!_propertyMap.TryGetValue(mapping.ColumnName!, out var propList))
                    {
                        propList = [];
                        _propertyMap[mapping.ColumnName!] = propList;
                    }
                    propList.Add(property);
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
                    _indexMap[i] = [.. properties];

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
                            if (!_indexMap.TryGetValue(index, out var indexList))
                            {
                                indexList = [];
                                _indexMap[index] = indexList;
                            }
                            indexList.Add(property);

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
    /// Convert string value to target type (public for testing and direct use)
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <param name="property">Target property info</param>
    /// <returns>Converted value</returns>
    public object? ConvertValue(string value, PropertyInfo property)
    {
        // Public API lets exceptions bubble up for testing
        return ConvertValue(-1, value, property);
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
            try
            {
                return converter(value);
            }
            catch
            {
                // Return default value if custom converter throws
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        // Check for attribute converter
        var attributeConverter = GetConverter(property);
        if (attributeConverter != null)
        {
            try
            {
                var attribute = GetCsvColumnAttribute(property);
                return attributeConverter.ConvertFromString(value, targetType, attribute?.Format);
            }
            catch
            {
                // Return default value if attribute converter throws
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
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
            if (_mapping?.TryGetDefault(property.Name, out object? defaultValue) == true && defaultValue != null)
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

        // Get culture for this field
        var culture = GetFieldCulture(property);

        // Common type conversions with culture support - let exceptions bubble up
        return targetType.Name switch
        {
            nameof(String) => value,
            nameof(Byte) => byte.Parse(value, NumberStyles.Number, culture),
            nameof(SByte) => sbyte.Parse(value, NumberStyles.Number, culture),
            nameof(Int16) => short.Parse(value, NumberStyles.Number, culture),
            nameof(UInt16) => ushort.Parse(value, NumberStyles.Number, culture),
            nameof(Int32) => int.Parse(value, NumberStyles.Number, culture),
            nameof(UInt32) => uint.Parse(value, NumberStyles.Number, culture),
            nameof(Int64) => long.Parse(value, NumberStyles.Number, culture),
            nameof(UInt64) => ulong.Parse(value, NumberStyles.Number, culture),
            nameof(Single) => float.Parse(value, NumberStyles.Number, culture),
            nameof(Double) => double.Parse(value, NumberStyles.Number, culture),
            nameof(Decimal) => decimal.Parse(value, NumberStyles.Number, culture),
            nameof(Boolean) => bool.Parse(value),
            nameof(Char) => value.Length > 0 ? value[0] : '\0',
            nameof(DateTime) => ParseDateTime(value, property),
            nameof(DateTimeOffset) => ParseDateTimeOffset(value, property),
            nameof(Guid) => Guid.Parse(value),
            nameof(TimeSpan) => TimeSpan.Parse(value, culture),
#if NET6_0_OR_GREATER
            nameof(DateOnly) => ParseDateOnly(value, property),
            nameof(TimeOnly) => ParseTimeOnly(value, property),
#endif
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

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
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
    /// Gets the culture to use for a specific field
    /// </summary>
    private CultureInfo GetFieldCulture(PropertyInfo property)
    {
        // Check for field-specific culture first
        if (_fieldCultures.TryGetValue(property.Name, out var fieldCulture))
        {
            return fieldCulture;
        }

        // Fall back to mapper's default culture
        return _culture;
    }

    /// <summary>
    /// Parse DateTime with format and culture support
    /// </summary>
    private DateTime ParseDateTime(string value, PropertyInfo property)
    {
        var culture = GetFieldCulture(property);

        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);

        if (format != null)
        {
            if (DateTime.TryParseExact(value, format, culture, DateTimeStyles.None, out var dt))
                return dt;
            throw new FormatException($"Unable to parse '{value}' as DateTime using format '{format}' and culture '{culture.Name}'");
        }

        return DateTime.Parse(value, culture);
    }

    /// <summary>
    /// Parse DateTimeOffset with format and culture support
    /// </summary>
    private DateTimeOffset ParseDateTimeOffset(string value, PropertyInfo property)
    {
        var culture = GetFieldCulture(property);

        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);

        if (format != null)
        {
            if (DateTimeOffset.TryParseExact(value, format, culture, DateTimeStyles.None, out var dto))
                return dto;
            throw new FormatException($"Unable to parse '{value}' as DateTimeOffset using format '{format}' and culture '{culture.Name}'");
        }

        return DateTimeOffset.Parse(value, culture);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Parse DateOnly with format and culture support
    /// </summary>
    private DateOnly ParseDateOnly(string value, PropertyInfo property)
    {
        var culture = GetFieldCulture(property);
        
        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);

        if (format != null)
        {
            if (DateOnly.TryParseExact(value, format, culture, DateTimeStyles.None, out var dateOnly))
                return dateOnly;
            throw new FormatException($"Unable to parse '{value}' as DateOnly using format '{format}' and culture '{culture.Name}'");
        }

        return DateOnly.Parse(value, culture);
    }

    /// <summary>
    /// Parse TimeOnly with format and culture support
    /// </summary>
    private TimeOnly ParseTimeOnly(string value, PropertyInfo property)
    {
        var culture = GetFieldCulture(property);
        
        // Check if there's a format specified in the mapping
        string? format = null;
        _mapping?.TryGetFormat(property.Name, out format);

        if (format != null)
        {
            if (TimeOnly.TryParseExact(value, format, culture, DateTimeStyles.None, out var timeOnly))
                return timeOnly;
            throw new FormatException($"Unable to parse '{value}' as TimeOnly using format '{format}' and culture '{culture.Name}'");
        }

        return TimeOnly.Parse(value, culture);
    }
#endif

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
