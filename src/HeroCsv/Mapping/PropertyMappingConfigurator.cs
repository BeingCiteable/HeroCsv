using System;
using System.Diagnostics.CodeAnalysis;
using HeroCsv.Mapping.Converters;

namespace HeroCsv.Mapping;

/// <summary>
/// Configurator for property-specific mapping options
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TProperty">Property type</typeparam>
public class PropertyMappingConfigurator<T, TProperty> where T : class, new()
{
    private readonly CsvMapping<T> _mapping;
    private readonly string _propertyName;

    internal PropertyMappingConfigurator(CsvMapping<T> mapping, string propertyName)
    {
        _mapping = mapping;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Sets a custom converter for this property
    /// </summary>
    /// <param name="converter">Converter function</param>
    /// <returns>The parent mapping for fluent configuration</returns>
    public CsvMapping<T> WithConverter(Func<string, TProperty?> converter)
    {
        _mapping.SetConverter(_propertyName, value => converter(value));
        return _mapping;
    }

    /// <summary>
    /// Sets a typed converter for this property
    /// </summary>
    /// <typeparam name="TConverter">Converter type</typeparam>
    /// <returns>The parent mapping for fluent configuration</returns>
    public CsvMapping<T> WithConverter<TConverter>() where TConverter : ICsvConverter, new()
    {
        var converter = new TConverter();
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        // This is an expected use - converters are designed to be AOT-incompatible when using reflection
        _mapping.SetConverter(_propertyName, value => 
            converter.ConvertFromString(value, typeof(TProperty)));
#pragma warning restore IL3050
        return _mapping;
    }

    /// <summary>
    /// Sets a format string for parsing (e.g., date formats)
    /// </summary>
    /// <param name="format">Format string</param>
    /// <returns>The parent mapping for fluent configuration</returns>
    public CsvMapping<T> WithFormat(string format)
    {
        _mapping.SetFormat(_propertyName, format);
        return _mapping;
    }

    /// <summary>
    /// Sets a default value when the CSV field is empty
    /// </summary>
    /// <param name="defaultValue">Default value</param>
    /// <returns>The parent mapping for fluent configuration</returns>
    public CsvMapping<T> WithDefault(TProperty defaultValue)
    {
        _mapping.SetDefault(_propertyName, defaultValue);
        return _mapping;
    }

    /// <summary>
    /// Makes this property required (non-empty)
    /// </summary>
    /// <returns>The parent mapping for fluent configuration</returns>
    public CsvMapping<T> Required()
    {
        _mapping.SetRequired(_propertyName, true);
        return _mapping;
    }
}