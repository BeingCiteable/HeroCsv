using System.Linq.Expressions;
using HeroCsv.Models;

namespace HeroCsv.Mapping;

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

    private readonly List<CsvPropertyMapping> _propertyMappings = [];
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
    public bool UseAutoMapWithOverrides { get; set; }

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
    /// Type-safe property mapping using expression
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> Map<TProperty>(Expression<Func<T, TProperty>> propertyExpression, string columnName)
    {
        var mapping = CsvPropertyMapping<T>.FromExpression(propertyExpression, columnName: columnName);
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = mapping.PropertyName,
            ColumnName = mapping.ColumnName,
            Converter = mapping.Converter
        });
        return this;
    }

    /// <summary>
    /// Type-safe property mapping using expression with column index
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> Map<TProperty>(Expression<Func<T, TProperty>> propertyExpression, int columnIndex)
    {
        var mapping = CsvPropertyMapping<T>.FromExpression(propertyExpression, columnIndex: columnIndex);
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = mapping.PropertyName,
            ColumnIndex = mapping.ColumnIndex,
            Converter = mapping.Converter
        });
        return this;
    }

    /// <summary>
    /// Type-safe property mapping with custom converter
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnName">Name of the CSV column</param>
    /// <param name="converter">Type-safe converter function</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> Map<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        string columnName,
        Func<string, TProperty> converter)
    {
        var mapping = CsvPropertyMapping<T>.FromExpression(propertyExpression, columnName: columnName, converter: converter);
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = mapping.PropertyName,
            ColumnName = mapping.ColumnName,
            Converter = mapping.Converter
        });
        return this;
    }

    /// <summary>
    /// Type-safe property mapping with custom converter by index
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnIndex">Zero-based index of the CSV column</param>
    /// <param name="converter">Type-safe converter function</param>
    /// <returns>This mapping instance for fluent configuration</returns>
    public CsvMapping<T> Map<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        int columnIndex,
        Func<string, TProperty> converter)
    {
        var mapping = CsvPropertyMapping<T>.FromExpression(propertyExpression, columnIndex: columnIndex, converter: converter);
        _propertyMappings.Add(new CsvPropertyMapping
        {
            PropertyName = mapping.PropertyName,
            ColumnIndex = mapping.ColumnIndex,
            Converter = mapping.Converter
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
