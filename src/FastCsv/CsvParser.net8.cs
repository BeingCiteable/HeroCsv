#if NET8_0_OR_GREATER
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace FastCsv;

/// <summary>
/// .NET 8+ optimized CSV parsing with SIMD vectorization
/// </summary>
internal static partial class CsvParser
{
    /// <summary>
    /// Vectorized line end finding using SIMD instructions
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEndVectorized(ReadOnlySpan<char> content, int start)
    {
        if (start >= content.Length) return content.Length;

        var span = content.Slice(start);
        
        // Use vectorized search for line endings
        if (Vector256.IsHardwareAccelerated && span.Length >= Vector256<ushort>.Count)
        {
            return FindLineEndAvx2(span, start);
        }
        else if (Vector128.IsHardwareAccelerated && span.Length >= Vector128<ushort>.Count)
        {
            return FindLineEndSse2(span, start);
        }
        
        // Fallback to scalar
        return FindLineEnd(content, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindLineEndAvx2(ReadOnlySpan<char> span, int baseStart)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(span);
        var length = span.Length;
        var vectorSize = Vector256<ushort>.Count; // 16 chars
        
        var crVector = Vector256.Create((ushort)'\r');
        var lfVector = Vector256.Create((ushort)'\n');
        
        int i = 0;
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var crMask = Vector256.Equals(vector, crVector);
            var lfMask = Vector256.Equals(vector, lfVector);
            var lineMask = Vector256.BitwiseOr(crMask, lfMask);
            
            if (!Vector256.EqualsAll(lineMask, Vector256<ushort>.Zero))
            {
                var mask = Vector256.ExtractMostSignificantBits(lineMask.AsByte());
                var offset = BitOperations.TrailingZeroCount(mask) / 2; // Convert from byte offset to char offset
                return baseStart + i + offset;
            }
        }
        
        // Handle remaining chars
        for (; i < length; i++)
        {
            var ch = Unsafe.Add(ref searchStart, i);
            if (ch == '\r' || ch == '\n')
            {
                return baseStart + i;
            }
        }
        
        return baseStart + length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindLineEndSse2(ReadOnlySpan<char> span, int baseStart)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(span);
        var length = span.Length;
        var vectorSize = Vector128<ushort>.Count; // 8 chars
        
        var crVector = Vector128.Create((ushort)'\r');
        var lfVector = Vector128.Create((ushort)'\n');
        
        int i = 0;
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var crMask = Vector128.Equals(vector, crVector);
            var lfMask = Vector128.Equals(vector, lfVector);
            var lineMask = Vector128.BitwiseOr(crMask, lfMask);
            
            if (!Vector128.EqualsAll(lineMask, Vector128<ushort>.Zero))
            {
                var mask = Vector128.ExtractMostSignificantBits(lineMask.AsByte());
                var offset = BitOperations.TrailingZeroCount(mask) / 2;
                return baseStart + i + offset;
            }
        }
        
        // Handle remaining chars
        for (; i < length; i++)
        {
            var ch = Unsafe.Add(ref searchStart, i);
            if (ch == '\r' || ch == '\n')
            {
                return baseStart + i;
            }
        }
        
        return baseStart + length;
    }

    /// <summary>
    /// Vectorized delimiter counting for field allocation
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDelimitersVectorized(ReadOnlySpan<char> line, char delimiter)
    {
        if (Vector256.IsHardwareAccelerated && line.Length >= Vector256<ushort>.Count)
        {
            return CountDelimitersAvx2(line, delimiter);
        }
        else if (Vector128.IsHardwareAccelerated && line.Length >= Vector128<ushort>.Count)
        {
            return CountDelimitersSse2(line, delimiter);
        }
        
        // Fallback to scalar
        int count = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == delimiter) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountDelimitersAvx2(ReadOnlySpan<char> line, char delimiter)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(line);
        var length = line.Length;
        var vectorSize = Vector256<ushort>.Count;
        
        var delimiterVector = Vector256.Create((ushort)delimiter);
        int count = 0;
        int i = 0;
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var matches = Vector256.Equals(vector, delimiterVector);
            count += BitOperations.PopCount(Vector256.ExtractMostSignificantBits(matches.AsByte())) / 2;
        }
        
        // Handle remaining chars
        for (; i < length; i++)
        {
            if (Unsafe.Add(ref searchStart, i) == delimiter) count++;
        }
        
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountDelimitersSse2(ReadOnlySpan<char> line, char delimiter)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(line);
        var length = line.Length;
        var vectorSize = Vector128<ushort>.Count;
        
        var delimiterVector = Vector128.Create((ushort)delimiter);
        int count = 0;
        int i = 0;
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var matches = Vector128.Equals(vector, delimiterVector);
            count += BitOperations.PopCount(Vector128.ExtractMostSignificantBits(matches.AsByte())) / 2;
        }
        
        // Handle remaining chars
        for (; i < length; i++)
        {
            if (Unsafe.Add(ref searchStart, i) == delimiter) count++;
        }
        
        return count;
    }

    /// <summary>
    /// Ultra-fast unquoted line parsing using vectorized operations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseUnquotedLineVectorized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Fast delimiter count using SIMD
        var fieldCount = CountDelimitersVectorized(line, options.Delimiter) + 1;
        var fields = new string[fieldCount];
        
        // Parse fields in single pass
        var fieldIndex = 0;
        var fieldStart = 0;
        
        // Use span-based iteration for better performance
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == options.Delimiter)
            {
                var field = line.Slice(fieldStart, i - fieldStart);
                fields[fieldIndex++] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
                fieldStart = i + 1;
            }
        }
        
        // Add final field
        if (fieldStart <= line.Length)
        {
            var field = line.Slice(fieldStart);
            fields[fieldIndex] = options.TrimWhitespace ? field.Trim().ToString() : field.ToString();
        }
        
        return fields;
    }

    /// <summary>
    /// Check if line contains quotes using vectorized search
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsQuotesVectorized(ReadOnlySpan<char> line, char quote)
    {
        if (Vector256.IsHardwareAccelerated && line.Length >= Vector256<ushort>.Count)
        {
            return ContainsQuotesAvx2(line, quote);
        }
        else if (Vector128.IsHardwareAccelerated && line.Length >= Vector128<ushort>.Count)
        {
            return ContainsQuotesSse2(line, quote);
        }
        
        // Fallback to scalar
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == quote) return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsQuotesAvx2(ReadOnlySpan<char> line, char quote)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(line);
        var length = line.Length;
        var vectorSize = Vector256<ushort>.Count;
        
        var quoteVector = Vector256.Create((ushort)quote);
        int i = 0;
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var matches = Vector256.Equals(vector, quoteVector);
            if (!Vector256.EqualsAll(matches, Vector256<ushort>.Zero))
            {
                return true;
            }
        }
        
        // Check remaining chars
        for (; i < length; i++)
        {
            if (Unsafe.Add(ref searchStart, i) == quote) return true;
        }
        
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsQuotesSse2(ReadOnlySpan<char> line, char quote)
    {
        ref var searchStart = ref MemoryMarshal.GetReference(line);
        var length = line.Length;
        var vectorSize = Vector128<ushort>.Count;
        
        var quoteVector = Vector128.Create((ushort)quote);
        int i = 0;
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            ref var current = ref Unsafe.Add(ref searchStart, i);
            var vector = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref current));
            
            var matches = Vector128.Equals(vector, quoteVector);
            if (!Vector128.EqualsAll(matches, Vector128<ushort>.Zero))
            {
                return true;
            }
        }
        
        // Check remaining chars
        for (; i < length; i++)
        {
            if (Unsafe.Add(ref searchStart, i) == quote) return true;
        }
        
        return false;
    }

    /// <summary>
    /// Optimized ParseLine using vectorized operations for .NET 8+
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] ParseLineOptimized(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty) return [];

        // Use vectorized quote detection
        bool hasQuotes = ContainsQuotesVectorized(line, options.Quote);

        return hasQuotes ? ParseQuotedLine(line, options) : ParseUnquotedLineVectorized(line, options);
    }
}
#endif