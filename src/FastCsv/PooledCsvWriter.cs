using System.Buffers;

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
/// Memory-pooled buffer writer for CSV operations
/// </summary>
public sealed class PooledCsvWriter : IBufferWriter<char>, IDisposable
{
    private char[] _buffer;
    private int _position;
    private readonly ArrayPool<char> _pool;

    public PooledCsvWriter(int initialCapacity = 4096)
    {
        _pool = ArrayPool<char>.Shared;
        _buffer = _pool.Rent(initialCapacity);
        _position = 0;
    }

    public ReadOnlySpan<char> WrittenSpan => _buffer.AsSpan(0, _position);
    public ReadOnlyMemory<char> WrittenMemory => _buffer.AsMemory(0, _position);

    public void Advance(int count)
    {
        if (count < 0 || _position + count > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        _position += count;
    }

    public Memory<char> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_position);
    }

    public Span<char> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_position);
    }

    private void EnsureCapacity(int sizeHint)
    {
        var needed = _position + sizeHint;
        if (needed <= _buffer.Length)
            return;

        var newSize = Math.Max(needed, _buffer.Length * 2);
        var newBuffer = _pool.Rent(newSize);

        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        _pool.Return(_buffer);
        _buffer = newBuffer;
    }

    public override string ToString() => new(_buffer, 0, _position);

    public void Dispose()
    {
        if (_buffer != null)
        {
            _pool.Return(_buffer);
            _buffer = null!;
        }
    }
}
