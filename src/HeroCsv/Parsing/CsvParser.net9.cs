#if NET9_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using System.Text;
using HeroCsv.Utilities;

namespace HeroCsv.Parsing;

public static partial class CsvParser
{
    // Common field values for quick checks
    private static readonly HashSet<string> CommonFieldValues = ["true", "false", "null", "NULL", "N/A", ""];

    /// <summary>
    /// Ultra-fast SIMD field parsing using Vector512 (AVX-512)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<string> ParseLineVector512(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        if (!Vector512.IsHardwareAccelerated || line.Length < 64)
        {
            // Fall back to Vector256 or scalar
            return ParseLineVector256(line, delimiter, stringPool);
        }

        var fields = new List<string>();
        var delimiterVector = Vector512.Create(delimiter);
        var quoteVector = Vector512.Create('"');

        var position = 0;
        var fieldStart = 0;

        // Process 32 characters at a time (Vector512<ushort>)
        while (position + 32 <= line.Length)
        {
            ReadOnlySpan<ushort> chars = MemoryMarshal.Cast<char, ushort>(line.Slice(position, 32));
            var vector = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(chars));

            // Check for delimiters and quotes in parallel
            var delimiterMask = Vector512.Equals(vector, delimiterVector);
            var quoteMask = Vector512.Equals(vector, quoteVector);

            // Check if any delimiters or quotes found
            // For now, use simple scan until Vector512 comparison APIs are available
            var delimiterFound = false;
            var quoteFound = false;
            for (int j = 0; j < 32 && j + position < line.Length; j++)
            {
                if (line[position + j] == delimiter) { delimiterFound = true; break; }
                if (line[position + j] == '"') { quoteFound = true; break; }
            }

            if (delimiterFound)
            {
                // Find first delimiter position
                var delimiterIndex = 0;
                for (int j = 0; j < 32; j++)
                {
                    if (line[position + j] == delimiter)
                    {
                        delimiterIndex = j;
                        break;
                    }
                }

                var fieldEnd = position + delimiterIndex;

                if (fieldEnd > fieldStart)
                {
                    var field = line[fieldStart..fieldEnd];
                    fields.Add(stringPool.GetString(field));
                }
                else
                {
                    fields.Add(string.Empty);
                }

                fieldStart = fieldEnd + 1;
                position = fieldStart;
            }
            else if (quoteFound)
            {
                // Handle quoted field (fall back to standard parsing)
                return ParseLineWithQuotes(line, delimiter, stringPool);
            }
            else
            {
                position += 32;
            }
        }

        // Process remaining characters
        if (fieldStart < line.Length)
        {
            var remaining = line[fieldStart..];
            var delimiterIndex = remaining.IndexOf(delimiter);

            if (delimiterIndex >= 0)
            {
                fields.Add(stringPool.GetString(remaining[..delimiterIndex]));
                fields.Add(stringPool.GetString(remaining[(delimiterIndex + 1)..]));
            }
            else
            {
                fields.Add(stringPool.GetString(remaining));
            }
        }
        else if (fieldStart == line.Length)
        {
            fields.Add(string.Empty);
        }

        return fields;
    }

    /// <summary>
    /// Vector256 fallback for systems without AVX-512
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<string> ParseLineVector256(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        if (!Avx2.IsSupported || line.Length < 32)
        {
            return ParseLineScalar(line, delimiter, stringPool);
        }

        var fields = new List<string>();
        var delimiterVector = Vector256.Create(delimiter);
        var quoteVector = Vector256.Create('"');

        var position = 0;
        var fieldStart = 0;

        // Process 16 characters at a time (Vector256<ushort>)
        while (position + 16 <= line.Length)
        {
            ReadOnlySpan<ushort> chars = MemoryMarshal.Cast<char, ushort>(line.Slice(position, 16));
            var vector = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(chars));

            var delimiterMask = Vector256.Equals(vector, delimiterVector);
            var quoteMask = Vector256.Equals(vector, quoteVector);

            // Check if any delimiters or quotes found
            // Use simple scan for Vector256 as well
            var delimiterFound = false;
            var quoteFound = false;
            for (int j = 0; j < 16 && j + position < line.Length; j++)
            {
                if (line[position + j] == delimiter) { delimiterFound = true; break; }
                if (line[position + j] == '"') { quoteFound = true; break; }
            }

            if (delimiterFound)
            {
                // Find first delimiter position
                var delimiterIndex = 0;
                for (int j = 0; j < 16; j++)
                {
                    if (line[position + j] == delimiter)
                    {
                        delimiterIndex = j;
                        break;
                    }
                }

                var fieldEnd = position + delimiterIndex;

                if (fieldEnd > fieldStart)
                {
                    var field = line[fieldStart..fieldEnd];
                    fields.Add(stringPool.GetString(field));
                }
                else
                {
                    fields.Add(string.Empty);
                }

                fieldStart = fieldEnd + 1;
                position = fieldStart;
            }
            else if (quoteFound)
            {
                return ParseLineWithQuotes(line, delimiter, stringPool);
            }
            else
            {
                position += 16;
            }
        }

        // Process remaining
        if (fieldStart <= line.Length)
        {
            var remaining = line[fieldStart..];
            fields.Add(stringPool.GetString(remaining));
        }

        return fields;
    }

    /// <summary>
    /// Check if a field value is a common CSV value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCommonValue(ReadOnlySpan<char> field)
    {
        return CommonFieldValues.Contains(field.ToString());
    }

    private static List<string> ParseLineScalar(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        // Standard scalar implementation
        var fields = new List<string>();
        var start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == delimiter)
            {
                fields.Add(stringPool.GetString(line[start..i]));
                start = i + 1;
            }
            else if (line[i] == '"')
            {
                return ParseLineWithQuotes(line, delimiter, stringPool);
            }
        }

        fields.Add(stringPool.GetString(line[start..]));
        return fields;
    }

    private static List<string> ParseLineWithQuotes(ReadOnlySpan<char> line, char delimiter, StringPool stringPool)
    {
        // Existing quote handling logic
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        var start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (line[i] == delimiter && !inQuotes)
            {
                fields.Add(sb.Length > 0 ? sb.ToString() : stringPool.GetString(line[start..i]));
                sb.Clear();
                start = i + 1;
            }
            else
            {
                sb.Append(line[i]);
            }
        }

        fields.Add(sb.Length > 0 ? sb.ToString() : stringPool.GetString(line[start..]));
        return fields;
    }
}
#endif