#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using HeroCsv.Core;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// SIMD-accelerated parsing strategy for high-performance CSV processing on .NET 8+
/// </summary>
public sealed class SIMDOptimizedParsingStrategy : IParsingStrategy
{
    private readonly SearchValues<char> _delimiterSearch;
    private readonly SearchValues<char> _quoteSearch;
    
    public SIMDOptimizedParsingStrategy()
    {
        _delimiterSearch = SearchValues.Create(",");
        _quoteSearch = SearchValues.Create("\"");
    }
    
    public int Priority => 200; // Highest priority when available
    
    public bool IsAvailable => Vector256.IsHardwareAccelerated || Vector128.IsHardwareAccelerated;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanHandle(ReadOnlySpan<char> line, CsvOptions options)
    {
        // SIMD path for comma delimiter without quotes
        return options.Delimiter == ',' && 
               !options.TrimWhitespace && 
               !line.ContainsAny(_quoteSearch) &&
               IsAvailable;
    }
    
    public string[] Parse(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty)
            return Array.Empty<string>();

        // Use SearchValues for ultra-fast delimiter searching
        var fieldCount = CountFields(line);
        var fields = new string[fieldCount];
        var fieldIndex = 0;
        var startIndex = 0;
        
        while (startIndex < line.Length && fieldIndex < fields.Length)
        {
            var delimiterIndex = line.Slice(startIndex).IndexOfAny(_delimiterSearch);
            
            if (delimiterIndex < 0)
            {
                // Last field
                var lastFieldSpan = line.Slice(startIndex);
                fields[fieldIndex++] = options.StringPool?.GetString(lastFieldSpan) ?? lastFieldSpan.ToString();
                break;
            }
            
            var fieldSpan = line.Slice(startIndex, delimiterIndex);
            fields[fieldIndex++] = options.StringPool?.GetString(fieldSpan) ?? fieldSpan.ToString();
            startIndex += delimiterIndex + 1;
        }
        
        // If we've consumed all input but haven't filled all fields, 
        // it means the line ended with a delimiter - add empty field
        while (fieldIndex < fields.Length)
        {
            fields[fieldIndex++] = string.Empty;
        }
        
        return fields;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountFields(ReadOnlySpan<char> line)
    {
        if (line.IsEmpty)
            return 0;
            
        int count = 1; // At least one field
        int index = 0;
        
        // Use SearchValues for fast counting
        while (index < line.Length)
        {
            var nextDelimiter = line.Slice(index).IndexOfAny(_delimiterSearch);
            if (nextDelimiter < 0)
                break;
                
            count++;
            index += nextDelimiter + 1;
        }
        
        return count;
    }
}
#endif