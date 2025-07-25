using System.Runtime.CompilerServices;

namespace FastCsv;

/// <summary>
/// Zero-allocation enumerable for CSV rows
/// </summary>
public readonly struct RowEnumerable
{
    private readonly FastCsvReader _reader;

    internal RowEnumerable(FastCsvReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Gets the enumerator
    /// </summary>
    public RowEnumerator GetEnumerator() => new RowEnumerator(_reader);
}

/// <summary>
/// Zero-allocation enumerator for CSV rows
/// </summary>
public ref struct RowEnumerator
{
    private readonly FastCsvReader _reader;
    private readonly CsvOptions _options;
    private readonly ReadOnlySpan<char> _buffer;
    private int _lineStart;
    private int _lineLength;
    private int _lineNumber;

    internal RowEnumerator(FastCsvReader reader)
    {
        _reader = reader;
        _options = reader.Options;
        _buffer = reader.GetBuffer();
        _lineStart = 0;
        _lineLength = 0;
        _lineNumber = 0;
    }

    /// <summary>
    /// Gets the current row
    /// </summary>
    public readonly CsvRow Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_buffer, _lineStart, _lineLength, _options);
    }

    /// <summary>
    /// Moves to the next row
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        return _reader.TryGetNextLine(out _lineStart, out _lineLength, out _lineNumber);
    }
}