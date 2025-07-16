#if NETSTANDARD2_0
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
using System.Buffers;
#endif


namespace FastCsv;

/// <summary>
/// Configuration for CSV parsing and writing operations
/// </summary>
public readonly struct CsvOptions
{
    public readonly char Delimiter;
    public readonly char Quote;
    public readonly bool HasHeader;
    public readonly bool TrimWhitespace;
    public readonly string NewLine;

#if NET8_0_OR_GREATER
    // Precomputed SearchValues for ultra-fast character searching
    public readonly SearchValues<char> SpecialChars;
    public readonly SearchValues<char> NewLineChars;
#endif

    public CsvOptions(
        char delimiter = ',',
        char quote = '"',
        bool hasHeader = true,
        bool trimWhitespace = false,
        string newLine = "\r\n")
    {
        Delimiter = delimiter;
        Quote = quote;
        HasHeader = hasHeader;
        TrimWhitespace = trimWhitespace;
        NewLine = newLine ?? Environment.NewLine;

#if NET8_0_OR_GREATER
        // Pre-create SearchValues for maximum performance
        SpecialChars = SearchValues.Create([delimiter, quote, '\r', '\n']);
        NewLineChars = SearchValues.Create(['\r', '\n']);
#endif
    }

    public static CsvOptions Default => new(',', '"', true, false, "\r\n");
}
