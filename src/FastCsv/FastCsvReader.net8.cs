#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Advanced features for FastCsvReader
/// </summary>
internal sealed partial class FastCsvReader
{
    private FrozenSet<string>? _fieldNames;

    /// <summary>
    /// Automatically detects CSV format based on content analysis
    /// </summary>
    /// <returns>Detected CSV options</returns>
    public CsvOptions DetectFormat()
    {
        // Use the existing auto-detection logic from Csv.net8.cs
        return AutoDetectFormat(_content.AsSpan());
    }

    /// <summary>
    /// Attempts to get a field by name using the header row
    /// </summary>
    /// <param name="fieldName">Name of the field to retrieve</param>
    /// <param name="field">Field content if found</param>
    /// <returns>True if field was found</returns>
    public bool TryGetFieldByName(string fieldName, out ReadOnlySpan<char> field)
    {
        field = ReadOnlySpan<char>.Empty;

        if (_currentRecord == null || _fieldNames == null)
        {
            return false;
        }

        var index = 0;
        foreach (var name in _fieldNames)
        {
            if (name == fieldName)
            {
                return _currentRecord.TryGetField(index, out field);
            }
            index++;
        }

        return false;
    }

    /// <summary>
    /// Gets all field names from the header row
    /// </summary>
    /// <returns>Frozen set of field names for optimal performance</returns>
    public FrozenSet<string> GetFieldNames()
    {
        return _fieldNames ?? FrozenSet<string>.Empty;
    }

    /// <summary>
    /// Sets the field names for header-based field access
    /// </summary>
    /// <param name="fieldNames">Array of field names from header row</param>
    internal void SetFieldNames(string[] fieldNames)
    {
        _fieldNames = fieldNames.ToFrozenSet();
    }

    private static CsvOptions AutoDetectFormat(ReadOnlySpan<char> content)
    {
        var sampleSize = Math.Min(content.Length, 2000);
        var sample = content.Slice(0, sampleSize);

        var commaCount = 0;
        var semicolonCount = 0;
        var tabCount = 0;
        var hasQuotes = false;

        for (int i = 0; i < sample.Length; i++)
        {
            switch (sample[i])
            {
                case ',': commaCount++; break;
                case ';': semicolonCount++; break;
                case '\t': tabCount++; break;
                case '"': hasQuotes = true; break;
            }
        }

        var delimiter = ',';
        if (semicolonCount > commaCount && semicolonCount > tabCount)
            delimiter = ';';
        else if (tabCount > commaCount && tabCount > semicolonCount)
            delimiter = '\t';

        return new CsvOptions(delimiter, hasQuotes ? '"' : '"', true);
    }
}

/// <summary>
/// Advanced features for FastCsvRecord
/// </summary>
internal sealed partial class FastCsvRecord
{
    private FrozenSet<string>? _fieldNames;

    /// <summary>
    /// Attempts to get a field by name using the header row
    /// </summary>
    /// <param name="fieldName">Name of the field to retrieve</param>
    /// <param name="field">Field content if found</param>
    /// <returns>True if field was found</returns>
    public bool TryGetFieldByName(string fieldName, out ReadOnlySpan<char> field)
    {
        field = ReadOnlySpan<char>.Empty;

        if (_fieldNames == null)
        {
            return false;
        }

        var index = 0;
        foreach (var name in _fieldNames)
        {
            if (name == fieldName)
            {
                return TryGetField(index, out field);
            }
            index++;
        }

        return false;
    }

    /// <summary>
    /// Gets all field names from the header row
    /// </summary>
    /// <returns>Frozen set of field names for optimal performance</returns>
    public FrozenSet<string> GetFieldNames()
    {
        return _fieldNames ?? FrozenSet<string>.Empty;
    }

    /// <summary>
    /// Attempts to get the index of a field by name using optimized lookup
    /// </summary>
    /// <param name="fieldName">Name of the field to find</param>
    /// <param name="index">Index of the field if found</param>
    /// <returns>True if field was found</returns>
    public bool TryGetFieldIndex(string fieldName, out int index)
    {
        index = -1;

        if (_fieldNames == null)
        {
            return false;
        }

        var currentIndex = 0;
        foreach (var name in _fieldNames)
        {
            if (name == fieldName)
            {
                index = currentIndex;
                return true;
            }
            currentIndex++;
        }

        return false;
    }

    /// <summary>
    /// Sets the field names for header-based field access
    /// </summary>
    /// <param name="fieldNames">Array of field names from header row</param>
    internal void SetFieldNames(string[] fieldNames)
    {
        _fieldNames = fieldNames.ToFrozenSet();
    }
}
#endif