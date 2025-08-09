#if NET8_0_OR_GREATER
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace HeroCsv.Utilities;

public sealed partial class StringPool
{
    // Pre-defined common CSV values using FrozenSet for ultra-fast lookups
    private static readonly FrozenSet<string> CommonCsvValues = new[]
    {
        // Boolean values
        "true", "false", "True", "False", "TRUE", "FALSE",
        "yes", "no", "Yes", "No", "YES", "NO",
        "1", "0",
        
        // Null/empty indicators
        "", "null", "NULL", "Null",
        "N/A", "n/a", "NA", "na",
        "None", "none", "NONE",
        "-", "--", "—",
        
        // Common single characters
        "Y", "N", "y", "n",
        "T", "F", "t", "f",
        
        // Common status values
        "Active", "Inactive", "active", "inactive",
        "Enabled", "Disabled", "enabled", "disabled",
        "Complete", "Incomplete", "complete", "incomplete",
        "Success", "Failure", "success", "failure",
        
        // Common date/time indicators
        "AM", "PM", "am", "pm",
        "UTC", "GMT", "EST", "PST", "CST", "MST"
    }.ToFrozenSet(StringComparer.Ordinal);
    
    // Pre-interned common values for zero-allocation returns
    private static readonly Dictionary<string, string> InternedCommonValues = 
        CommonCsvValues.ToDictionary(s => s, s => string.Intern(s));
    
    /// <summary>
    /// Optimized GetOrAdd using FrozenSet for common values
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetOrAdd(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return string.Empty;
        
        // Fast path for single character
        if (span.Length == 1)
        {
            var ch = span[0];
            return ch switch
            {
                '0' => "0",
                '1' => "1",
                'Y' or 'y' => InternedCommonValues.GetValueOrDefault(ch.ToString()) ?? GetString(span),
                'N' or 'n' => InternedCommonValues.GetValueOrDefault(ch.ToString()) ?? GetString(span),
                'T' or 't' => InternedCommonValues.GetValueOrDefault(ch.ToString()) ?? GetString(span),
                'F' or 'f' => InternedCommonValues.GetValueOrDefault(ch.ToString()) ?? GetString(span),
                '-' => "-",
                _ => GetString(span)
            };
        }
        
        // For short strings, check if it's a common value
        if (span.Length <= 10)
        {
            // Create string only once for lookup
            var str = span.ToString();
            
            // Ultra-fast frozen set lookup
            if (CommonCsvValues.Contains(str))
            {
                return InternedCommonValues[str];
            }
            
            // Not common, use regular pool
            if (str.Length <= _maxStringLength)
            {
                return _pool.GetOrAdd(str, str);
            }
            
            return str;
        }
        
        // Regular pooling for other strings
        return GetString(span);
    }
    
    /// <summary>
    /// Checks if a value is in the common values set (useful for optimization decisions)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCommonValue(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return true;
        if (value.Length > 10) return false;
        
        return CommonCsvValues.Contains(value.ToString());
    }
    
    /// <summary>
    /// Pre-populate the pool with common values for better performance
    /// </summary>
    public void PrePopulateCommonValues()
    {
        foreach (var value in CommonCsvValues)
        {
            _pool.TryAdd(value, InternedCommonValues[value]);
        }
    }
}
#endif