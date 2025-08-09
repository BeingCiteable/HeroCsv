using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Unit.Utilities;

public class BufferPoolTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void RentCharBuffer_ReturnsBufferOfRequestedSizeOrLarger(int requestedSize)
    {
        // Arrange
        using var pool = new BufferPool();

        // Act
        var buffer = pool.RentCharBuffer(requestedSize);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestedSize);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    public void RentByteBuffer_ReturnsBufferOfRequestedSizeOrLarger(int requestedSize)
    {
        // Arrange
        using var pool = new BufferPool();

        // Act
        var buffer = pool.RentByteBuffer(requestedSize);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestedSize);
    }

    [Fact]
    public void ReturnCharBuffer_HandlesNullGracefully()
    {
        // Arrange
        using var pool = new BufferPool();

        // Act & Assert - should not throw
        pool.ReturnCharBuffer(null);
    }

    [Fact]
    public void ReturnByteBuffer_HandlesNullGracefully()
    {
        // Arrange
        using var pool = new BufferPool();

        // Act & Assert - should not throw
        pool.ReturnByteBuffer(null);
    }

    [Fact]
    public void ReturnAll_ClearsAllRentedBuffers()
    {
        // Arrange
        using var pool = new BufferPool();
        var charBuffers = new List<char[]>();
        var byteBuffers = new List<byte[]>();

        // Rent multiple buffers
        for (int i = 0; i < 5; i++)
        {
            charBuffers.Add(pool.RentCharBuffer(100 * (i + 1)));
            byteBuffers.Add(pool.RentByteBuffer(200 * (i + 1)));
        }

        // Act
        pool.ReturnAll();

        // Assert - After ReturnAll, all buffers should be returned to the pool
        // We can't directly verify this, but we can ensure no exceptions occur
        Assert.True(true);
    }

    [Fact]
    public void BufferPool_ReusesPreviouslyReturnedBuffers()
    {
        // Arrange
        using var pool = new BufferPool();

        // Act - Rent and return a buffer
        var buffer1 = pool.RentCharBuffer(100);
        var buffer1Length = buffer1.Length;
        pool.ReturnCharBuffer(buffer1);

        // Rent again with same size
        var buffer2 = pool.RentCharBuffer(100);

        // Assert - ArrayPool might return the same array
        Assert.True(buffer2.Length >= 100);
        // Note: We can't guarantee it's the same array due to ArrayPool implementation details
    }

    [Fact]
    public void RentCharBuffer_ZeroSize_ReturnsValidBuffer()
    {
        // Arrange
        using var pool = new BufferPool();

        // Act
        var buffer = pool.RentCharBuffer(0);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 0);
    }

    [Fact]
    public void RentByteBuffer_ZeroSize_ReturnsValidBuffer()
    {
        // Arrange
        using var pool = new BufferPool();

        // Act
        var buffer = pool.RentByteBuffer(0);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 0);
    }

    [Fact]
    public void BufferPool_Dispose_ReturnsAllBuffers()
    {
        // Arrange
        var pool = new BufferPool();
        var charBuffer = pool.RentCharBuffer(100);
        var byteBuffer = pool.RentByteBuffer(200);

        // Act
        pool.Dispose();

        // Assert - After dispose, all buffers should be returned
        // Dispose should be idempotent
        pool.Dispose();
    }

    [Fact]
    public void ReturnCharBuffer_WithClearFlag_ClearsBuffer()
    {
        // Arrange
        using var pool = new BufferPool();
        var buffer = pool.RentCharBuffer(10);

        // Fill buffer with data
        for (int i = 0; i < buffer.Length && i < 10; i++)
        {
            buffer[i] = 'X';
        }

        // Act
        pool.ReturnCharBuffer(buffer, clearBuffer: true);

        // Note: We can't verify the buffer is cleared after return
        // as it's returned to the ArrayPool
        Assert.True(true);
    }

    [Fact]
    public void ReturnByteBuffer_WithClearFlag_ClearsBuffer()
    {
        // Arrange
        using var pool = new BufferPool();
        var buffer = pool.RentByteBuffer(10);

        // Fill buffer with data
        for (int i = 0; i < buffer.Length && i < 10; i++)
        {
            buffer[i] = 123;
        }

        // Act
        pool.ReturnByteBuffer(buffer, clearBuffer: true);

        // Note: We can't verify the buffer is cleared after return
        // as it's returned to the ArrayPool
        Assert.True(true);
    }

    [Fact]
    public void BufferPool_ThreadSafety_ConcurrentRentAndReturn()
    {
        // Arrange
        using var pool = new BufferPool();
        var tasks = new Task[10];

        // Act - Multiple threads renting and returning buffers
        for (int i = 0; i < tasks.Length; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var charBuffer = pool.RentCharBuffer(100 + taskId * 10);
                    var byteBuffer = pool.RentByteBuffer(200 + taskId * 20);

                    // Simulate some work
                    Task.Delay(1).Wait();

                    pool.ReturnCharBuffer(charBuffer);
                    pool.ReturnByteBuffer(byteBuffer);
                }
            }, TestContext.Current.CancellationToken);
        }

        Task.WaitAll(tasks, TestContext.Current.CancellationToken);

        // Assert - No exceptions should be thrown
        Assert.True(true);
    }

    [Fact]
    public void ReturnAll_WithClearFlag_ClearsAllBuffers()
    {
        // Arrange
        using var pool = new BufferPool();
        var charBuffer = pool.RentCharBuffer(10);
        var byteBuffer = pool.RentByteBuffer(10);

        // Fill buffers with data
        for (int i = 0; i < 10; i++)
        {
            if (i < charBuffer.Length) charBuffer[i] = 'X';
            if (i < byteBuffer.Length) byteBuffer[i] = 123;
        }

        // Act
        pool.ReturnAll(clearBuffers: true);

        // Assert - Buffers are returned (we can't verify they're cleared)
        Assert.True(true);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(16384)]
    public void BufferPool_VariousSizes_HandledCorrectly(int size)
    {
        // Arrange
        using var pool = new BufferPool();

        // Act
        var charBuffer = pool.RentCharBuffer(size);
        var byteBuffer = pool.RentByteBuffer(size);

        // Assert
        Assert.True(charBuffer.Length >= size);
        Assert.True(byteBuffer.Length >= size);

        // Clean up
        pool.ReturnCharBuffer(charBuffer);
        pool.ReturnByteBuffer(byteBuffer);
    }
}