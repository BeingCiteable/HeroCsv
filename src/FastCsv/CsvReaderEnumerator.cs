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
/// Enumerator for CsvReader to support foreach loops
/// </summary>
public ref struct CsvReaderEnumerator
{
    private CsvReader _reader;

    internal CsvReaderEnumerator(CsvReader reader)
    {
        _reader = reader;
    }

    public readonly CsvReaderEnumerator GetEnumerator() => this;

    public bool MoveNext() => _reader.HasMoreData;

    public CsvRecord Current => _reader.ReadRecord();
}
