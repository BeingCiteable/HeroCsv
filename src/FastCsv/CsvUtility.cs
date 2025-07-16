using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

#if NETSTANDARD2_0
using System.ComponentModel;
#endif

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

#if NET7_0_OR_GREATER
using System.Buffers.Text;
#endif

#if NET8_0_OR_GREATER
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Runtime.Intrinsics;
using System.Text.Unicode;
#endif


namespace FastCsv;

/// <summary>
/// Static utility methods for common CSV operations
/// </summary>
public static class CsvUtility
{
#if NET8_0_OR_GREATER
    // Frozen collections for ultra-fast lookups (NET8+)
    private static readonly FrozenSet<char> CommonDelimiters = 
        FrozenSet.ToFrozenSet([',', ';', '\t', '|', ':']);
    
    private static readonly FrozenDictionary<string, CsvOptions> PresetOptions = 
        new Dictionary<string, CsvOptions>
        {
            ["standard"] = new(',', '"', true, false),
            ["excel"] = new(',', '"', true, false),
            ["tab"] = new('\t', '"', true, false),
            ["pipe"] = new('|', '"', true, false),
            ["semicolon"] = new(';', '"', true, false)
        }.ToFrozenDictionary();

    /// <summary>
    /// Auto-detect CSV format using vectorized analysis (NET8+)
    /// </summary>
    public static CsvOptions DetectFormat(ReadOnlySpan<char> sample)
    {
        if (sample.Length < 100)
            return CsvOptions.Default;

        // Take first few lines for analysis
        var lines = sample.Slice(0, Math.Min(1000, sample.Length));
        var delimiterCounts = new Dictionary<char, int>();

        // Count potential delimiters using SearchValues
        foreach (var delimiter in CommonDelimiters)
        {
            var searchValues = SearchValues.Create([delimiter]);
            var remaining = lines;
            var count = 0;

            while (!remaining.IsEmpty)
            {
                var index = remaining.IndexOfAny(searchValues);
                if (index == -1) break;
                count++;
                remaining = remaining.Slice(index + 1);
            }

            if (count > 0)
                delimiterCounts[delimiter] = count;
        }

        // Find most consistent delimiter
        var bestDelimiter = delimiterCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        return new CsvOptions(bestDelimiter);
    }

    /// <summary>
    /// Get preset options by name (NET8+)
    /// </summary>
    public static CsvOptions GetPresetOptions(string presetName)
    {
        return PresetOptions.TryGetValue(presetName.ToLowerInvariant(), out var options) 
            ? options 
            : CsvOptions.Default;
    }

    /// <summary>
    /// Ultra-fast field validation using SIMD (NET8+)
    /// </summary>
    public static bool ValidateField(ReadOnlySpan<char> field, char delimiter, char quote)
    {
        if (field.IsEmpty) return true;

        // Check for unescaped quotes using vectorized search
        var quoteSearch = SearchValues.Create([quote]);
        var remaining = field;
        var inQuotes = false;

        while (!remaining.IsEmpty)
        {
            var quoteIndex = remaining.IndexOfAny(quoteSearch);
            if (quoteIndex == -1) break;

            // Check if quote is escaped
            if (quoteIndex + 1 < remaining.Length && remaining[quoteIndex + 1] == quote)
            {
                remaining = remaining.Slice(quoteIndex + 2);
                continue;
            }

            inQuotes = !inQuotes;
            remaining = remaining.Slice(quoteIndex + 1);
        }

        return !inQuotes; // Field is valid if we're not left in quotes
    }
#endif

    /// <summary>
    /// Read CSV from a file with streaming
    /// </summary>
    public static List<CsvRecordWrapper> ReadFile(string filePath, CsvOptions options = default)
    {
        var text = File.ReadAllText(filePath);
        var reader = new CsvReader(text.AsSpan(), options);
        var results = new List<CsvRecordWrapper>();

        if (options.HasHeader)
            reader.SkipHeader();

        while (reader.HasMoreData)
        {
            var record = reader.ReadRecord();
            results.Add(new CsvRecordWrapper(record));
        }
        
        return results;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Read CSV from file with auto-detection (NET8+)
    /// </summary>
    public static List<CsvRecordWrapper> ReadFileAutoDetect(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var options = DetectFormat(text.AsSpan(0, Math.Min(1000, text.Length)));
        
        var reader = new CsvReader(text.AsSpan(), options);
        var results = new List<CsvRecordWrapper>();
        
        if (options.HasHeader)
            reader.SkipHeader();

        while (reader.HasMoreData)
        {
            var record = reader.ReadRecord();
            results.Add(new CsvRecordWrapper(record));
        }
        
        return results;
    }

    /// <summary>
    /// Asynchronous CSV reading with UTF-8 optimization (NET8+)
    /// </summary>
    public static async Task<List<CsvRecordWrapper>> ReadFileAsync(string filePath, 
        CsvOptions options = default)
    {
        var utf8Bytes = await File.ReadAllBytesAsync(filePath);
        
        // Ultra-fast UTF-8 to UTF-16 conversion
        var charCount = Encoding.UTF8.GetCharCount(utf8Bytes);
        var chars = ArrayPool<char>.Shared.Rent(charCount);
        var results = new List<CsvRecordWrapper>();
        
        try
        {
            var actualCharCount = Encoding.UTF8.GetChars(utf8Bytes, chars);
            var text = chars.AsSpan(0, actualCharCount);
            
            var reader = new CsvReader(text, options);
            if (options.HasHeader)
                reader.SkipHeader();

            while (reader.HasMoreData)
            {
                var record = reader.ReadRecord();
                results.Add(new CsvRecordWrapper(record));
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
        
        return results;
    }
#endif

    /// <summary>
    /// Write CSV to a file
    /// </summary>
    public static void WriteFile(string filePath, IEnumerable<string[]> records,
        string[]? headers = null, CsvOptions options = default)
    {
        using var pooledWriter = new PooledCsvWriter();
        var writer = new CsvWriter(pooledWriter, options);

        if (headers != null)
        {
            writer.WriteHeader(headers);
        }

        foreach (var record in records)
        {
            writer.WriteRecord(record);
        }

        File.WriteAllText(filePath, pooledWriter.ToString());
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Vectorized field counting for very large records (NET6+)
    /// </summary>
    public static int CountFieldsVectorized(ReadOnlySpan<char> record, char delimiter)
    {
        if (Vector.IsHardwareAccelerated && record.Length >= Vector<ushort>.Count)
        {
            return CountFieldsVectorizedImpl(record, delimiter);
        }
        
        return CountFieldsScalar(record, delimiter);
    }

    private static int CountFieldsVectorizedImpl(ReadOnlySpan<char> record, char delimiter)
    {
        var delimiterVector = new Vector<ushort>(delimiter);
        var count = 1; // Start with 1 field
        var pos = 0;
        var vectorSize = Vector<ushort>.Count;

        // Process in vector-sized chunks
        while (pos <= record.Length - vectorSize)
        {
            var span = record.Slice(pos, vectorSize);
            var vector = new Vector<ushort>(MemoryMarshal.Cast<char, ushort>(span));
            var matches = Vector.Equals(vector, delimiterVector);
            
            count += Vector.Dot(matches, Vector<ushort>.One);
            pos += vectorSize;
        }

        // Process remaining characters scalar
        for (; pos < record.Length; pos++)
        {
            if (record[pos] == delimiter)
                count++;
        }

        return count;
    }

    private static int CountFieldsScalar(ReadOnlySpan<char> record, char delimiter)
    {
        var count = 1;
        for (int i = 0; i < record.Length; i++)
        {
            if (record[i] == delimiter)
                count++;
        }
        return count;
    }
#endif

#if NET8_0_OR_GREATER && NET9_0_OR_GREATER
    /// <summary>
    /// Next-generation vectorized field counting using NET9 features
    /// </summary>
    public static int CountFieldsNet9(ReadOnlySpan<char> record, char delimiter)
    {
        // Use latest SIMD intrinsics and vectorization improvements in NET9
        if (Vector512.IsHardwareAccelerated && record.Length >= Vector512<ushort>.Count)
        {
            return CountFieldsVector512(record, delimiter);
        }
        
        if (Vector256.IsHardwareAccelerated && record.Length >= Vector256<ushort>.Count)
        {
            return CountFieldsVector256(record, delimiter);
        }
        
        return CountFieldsVectorized(record, delimiter);
    }

    private static int CountFieldsVector512(ReadOnlySpan<char> record, char delimiter)
    {
        var delimiterVector = Vector512.Create((ushort)delimiter);
        var count = 1;
        var pos = 0;
        var vectorSize = Vector512<ushort>.Count;

        while (pos <= record.Length - vectorSize)
        {
            var span = record.Slice(pos, vectorSize);
            var chars = MemoryMarshal.Cast<char, ushort>(span);
            var vector = Vector512.Create(chars);
            var matches = Vector512.Equals(vector, delimiterVector);
            
            count += BitOperations.PopCount(matches.ExtractMostSignificantBits());
            pos += vectorSize;
        }

        // Process remaining with smaller vectors or scalar
        for (; pos < record.Length; pos++)
        {
            if (record[pos] == delimiter)
                count++;
        }

        return count;
    }

    private static int CountFieldsVector256(ReadOnlySpan<char> record, char delimiter)
    {
        var delimiterVector = Vector256.Create((ushort)delimiter);
        var count = 1;
        var pos = 0;
        var vectorSize = Vector256<ushort>.Count;

        while (pos <= record.Length - vectorSize)
        {
            var span = record.Slice(pos, vectorSize);
            var chars = MemoryMarshal.Cast<char, ushort>(span);
            var vector = Vector256.Create(chars);
            var matches = Vector256.Equals(vector, delimiterVector);
            
            count += BitOperations.PopCount(matches.ExtractMostSignificantBits());
            pos += vectorSize;
        }

        return count;
    }
#endif
}