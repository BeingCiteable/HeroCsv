namespace HeroCsv.Utilities;

/// <summary>
/// Provides a scoped byte buffer rental that automatically returns the buffer when disposed
/// </summary>
public readonly struct ByteBufferLease : IDisposable
{
    private readonly BufferPool _pool;
    private readonly byte[] _buffer;

    public ByteBufferLease(BufferPool pool, int minimumLength)
    {
        _pool = pool;
        _buffer = pool.RentByteBuffer(minimumLength);
    }

    public byte[] Buffer => _buffer;

    public Span<byte> Span => _buffer.AsSpan();

    public void Dispose()
    {
        _pool?.ReturnByteBuffer(_buffer);
    }
}