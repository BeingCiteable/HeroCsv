using System;
using System.Linq.Expressions;
using System.Reflection;
using HeroCsv.Mapping.Converters;
using HeroCsv.Models;

namespace HeroCsv.Mapping;

/// <summary>
/// Fluent builder for creating CSV mappings with expression support
/// </summary>
/// <typeparam name="T">Type to map CSV records to</typeparam>
public class CsvMappingBuilder<T> where T : class, new()
{
    private readonly CsvMapping<T> _mapping;

    /// <summary>
    /// Creates a new mapping builder
    /// </summary>
    public CsvMappingBuilder()
    {
        _mapping = new CsvMapping<T>();
    }

    /// <summary>
    /// Creates a new mapping builder with options
    /// </summary>
    /// <param name="options">CSV parsing options</param>
    public CsvMappingBuilder(CsvOptions options)
    {
        _mapping = new CsvMapping<T> { Options = options };
    }

    /// <summary>
    /// Maps a property to a column by name using an expression
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnName">CSV column name</param>
    /// <returns>Property mapping configurator</returns>
    public PropertyMappingConfigurator<T, TProperty> Map<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression, 
        string columnName)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _mapping.MapProperty(propertyName, columnName);
        return new PropertyMappingConfigurator<T, TProperty>(_mapping, propertyName);
    }

    /// <summary>
    /// Maps a property to a column by index using an expression
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="columnIndex">CSV column index (0-based)</param>
    /// <returns>Property mapping configurator</returns>
    public PropertyMappingConfigurator<T, TProperty> Map<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression, 
        int columnIndex)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _mapping.MapProperty(propertyName, columnIndex);
        return new PropertyMappingConfigurator<T, TProperty>(_mapping, propertyName);
    }

    /// <summary>
    /// Ignores a property during mapping
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <returns>This builder for fluent configuration</returns>
    public CsvMappingBuilder<T> Ignore<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _mapping.IgnoreProperty(propertyName);
        return this;
    }

    /// <summary>
    /// Enables auto-mapping for properties not explicitly mapped
    /// </summary>
    /// <returns>This builder for fluent configuration</returns>
    public CsvMappingBuilder<T> AutoMap()
    {
        _mapping.UseAutoMapWithOverrides = true;
        return this;
    }

    /// <summary>
    /// Sets default value for a property when CSV field is empty
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="propertyExpression">Property selector expression</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>This builder for fluent configuration</returns>
    public CsvMappingBuilder<T> Default<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression, 
        TProperty defaultValue)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _mapping.SetDefault(propertyName, defaultValue);
        return this;
    }

    /// <summary>
    /// Sets CSV parsing options
    /// </summary>
    /// <param name="options">CSV options</param>
    /// <returns>This builder for fluent configuration</returns>
    public CsvMappingBuilder<T> WithOptions(CsvOptions options)
    {
        _mapping.Options = options;
        return this;
    }

    /// <summary>
    /// Builds the CSV mapping
    /// </summary>
    /// <returns>Configured CSV mapping</returns>
    public CsvMapping<T> Build()
    {
        return _mapping;
    }

    /// <summary>
    /// Implicit conversion to CsvMapping
    /// </summary>
    public static implicit operator CsvMapping<T>(CsvMappingBuilder<T> builder)
    {
        return builder.Build();
    }

    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        if (propertyExpression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandExpression)
        {
            return operandExpression.Member.Name;
        }
        
        throw new ArgumentException("Invalid property expression");
    }
}