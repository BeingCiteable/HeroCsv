using System.Linq.Expressions;

namespace HeroCsv.Mapping;

/// <summary>
/// Type-safe property mapping configuration
/// </summary>
/// <typeparam name="T">Type being mapped to</typeparam>
public sealed class CsvPropertyMapping<T> where T : class, new()
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
    /// Custom converter function for this property (non-generic for backward compatibility)
    /// </summary>
    public Func<string, object?>? Converter { get; set; }

    /// <summary>
    /// Type-safe property expression
    /// </summary>
    public Expression<Func<T, object?>>? PropertyExpression { get; set; }

    /// <summary>
    /// Creates a property mapping from an expression
    /// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types - This is a factory method pattern
    public static CsvPropertyMapping<T> FromExpression<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        string? columnName = null,
        int? columnIndex = null,
        Func<string, TProperty>? converter = null)
    {
        var memberExpression = propertyExpression.Body as MemberExpression;
        if (memberExpression == null)
        {
            var unaryExpression = propertyExpression.Body as UnaryExpression;
            memberExpression = unaryExpression?.Operand as MemberExpression;
        }

        if (memberExpression == null)
            throw new ArgumentException("Invalid property expression", nameof(propertyExpression));

        var mapping = new CsvPropertyMapping<T>
        {
            PropertyName = memberExpression.Member.Name,
            ColumnName = columnName,
            ColumnIndex = columnIndex
        };

        if (converter != null)
        {
            mapping.Converter = value => converter(value);
        }

        return mapping;
    }
#pragma warning restore CA1000
}