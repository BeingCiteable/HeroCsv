using System;

namespace HeroCsv.Mapping.Attributes;

/// <summary>
/// Indicates that a property should be ignored during CSV mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class CsvIgnoreAttribute : Attribute
{
}