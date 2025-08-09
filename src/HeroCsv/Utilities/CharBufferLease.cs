namespace HeroCsv.Utilities;

/// <summary>
/// Provides a scoped buffer rental that automatically returns the buffer when disposed
/// </summary>
public readonly struct CharBufferLease : IDisposable
{
    private readonly BufferPool _pool;
    private readonly char[] _buffer;

    public CharBufferLease(BufferPool pool, int minimumLength)
    {
        _pool = pool;
        _buffer = pool.RentCharBuffer(minimumLength);
    }

    public char[] Buffer => _buffer;

    public Span<char> Span => _buffer.AsSpan();

    public void Dispose()
    {
        _pool?.ReturnCharBuffer(_buffer);
    }
}
