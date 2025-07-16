using System.Buffers;
using System.Runtime.CompilerServices;

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
#endif


namespace FastCsv;

/// <summary>
/// High-performance CSV writer using ref struct and spans
/// </summary>
public ref struct CsvWriter
{
    private readonly CsvOptions _options;
    private readonly IBufferWriter<char> _writer;
    private bool _isFirstField;
    private bool _isFirstRecord;

    public CsvWriter(IBufferWriter<char> writer, CsvOptions options = default)
    {
        _options = options.Equals(default) ? CsvOptions.Default : options;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _isFirstField = true;
        _isFirstRecord = true;
    }

    /// <summary>
    /// Write a field to the current record
    /// </summary>
    public void WriteField(ReadOnlySpan<char> field)
    {
        if (!_isFirstField)
        {
            WriteChar(_options.Delimiter);
        }

        if (NeedsQuoting(field))
        {
            WriteQuotedField(field);
        }
        else
        {
            WriteSpan(field);
        }

        _isFirstField = false;
    }

    /// <summary>
    /// Write a field from a string
    /// </summary>
    public void WriteField(string field)
    {
        WriteField(field.AsSpan());
    }

    /// <summary>
    /// Write multiple fields as a complete record
    /// </summary>
    public void WriteRecord(params string[] fields)
    {
        foreach (var field in fields)
        {
            WriteField(field);
        }
        EndRecord();
    }


    /// <summary>
    /// End the current record and start a new line
    /// </summary>
    public void EndRecord()
    {
        if (!_isFirstRecord)
        {
            WriteSpan(_options.NewLine.AsSpan());
        }
        _isFirstField = true;
        _isFirstRecord = false;
    }

    /// <summary>
    /// Write header fields
    /// </summary>
    public void WriteHeader(params string[] headers)
    {
        WriteRecord(headers);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool NeedsQuoting(ReadOnlySpan<char> field)
    {
        var delimiter = _options.Delimiter;
        var quote = _options.Quote;

        for (int i = 0; i < field.Length; i++)
        {
            var ch = field[i];
            if (ch == delimiter || ch == quote || ch == '\r' || ch == '\n')
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteQuotedField(ReadOnlySpan<char> field)
    {
        var quote = _options.Quote;
        WriteChar(quote);

        for (int i = 0; i < field.Length; i++)
        {
            var ch = field[i];
            if (ch == quote)
            {
                WriteChar(quote); // Escape quote
            }
            WriteChar(ch);
        }

        WriteChar(quote);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteChar(char ch)
    {
        var span = _writer.GetSpan(1);
        span[0] = ch;
        _writer.Advance(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteSpan(ReadOnlySpan<char> text)
    {
        var span = _writer.GetSpan(text.Length);
        text.CopyTo(span);
        _writer.Advance(text.Length);
    }
}
