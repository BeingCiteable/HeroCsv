using System.Buffers;
using System.Runtime.CompilerServices;

namespace HeroCsv.Utilities;

/// <summary>
/// Manages temporary buffer allocations using ArrayPool to reduce GC pressure
/// </summary>
public sealed class BufferPool : IDisposable
{
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
    private readonly List<char[]> _rentedCharBuffers = new();
    private readonly List<byte[]> _rentedByteBuffers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Rents a character buffer of at least the specified size
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char[] RentCharBuffer(int minimumLength)
    {
        var buffer = _charPool.Rent(minimumLength);

        lock (_lock)
        {
            _rentedCharBuffers.Add(buffer);
        }

        return buffer;
    }

    /// <summary>
    /// Rents a byte buffer of at least the specified size
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] RentByteBuffer(int minimumLength)
    {
        var buffer = _bytePool.Rent(minimumLength);

        lock (_lock)
        {
            _rentedByteBuffers.Add(buffer);
        }

        return buffer;
    }

    /// <summary>
    /// Returns a rented character buffer to the pool
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnCharBuffer(char[] buffer, bool clearBuffer = false)
    {
        if (buffer == null) return;

        lock (_lock)
        {
            _rentedCharBuffers.Remove(buffer);
        }

        _charPool.Return(buffer, clearBuffer);
    }

    /// <summary>
    /// Returns a rented byte buffer to the pool
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnByteBuffer(byte[] buffer, bool clearBuffer = false)
    {
        if (buffer == null) return;

        lock (_lock)
        {
            _rentedByteBuffers.Remove(buffer);
        }

        _bytePool.Return(buffer, clearBuffer);
    }

    /// <summary>
    /// Returns all rented buffers to their pools
    /// </summary>
    public void ReturnAll(bool clearBuffers = false)
    {
        lock (_lock)
        {
            foreach (var buffer in _rentedCharBuffers)
            {
                _charPool.Return(buffer, clearBuffers);
            }
            _rentedCharBuffers.Clear();

            foreach (var buffer in _rentedByteBuffers)
            {
                _bytePool.Return(buffer, clearBuffers);
            }
            _rentedByteBuffers.Clear();
        }
    }

    /// <summary>
    /// Disposes of the buffer pool and returns all rented buffers
    /// </summary>
    public void Dispose()
    {
        ReturnAll(true);
    }
}
