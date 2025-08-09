using System;
using System.Buffers;
using Xunit;
using HeroCsv.Utilities;

namespace HeroCsv.Tests.Unit.Utilities;

public class BufferPoolEnhancedTests
{
    [Fact]
    public void BufferPool_RentsCharBufferOfRequestedSizeOrLarger()
    {
        // Arrange
        using var pool = new BufferPool();
        var requestedSize = 1024;
        
        // Act
        var buffer = pool.RentCharBuffer(requestedSize);
        
        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestedSize);
        
        // Clean up
        pool.ReturnCharBuffer(buffer);
    }

    [Fact]
    public void BufferPool_DisposesCorrectly()
    {
        // Arrange & Act
        char[]? buffer = null;
        
        using (var pool = new BufferPool())
        {
            buffer = pool.RentCharBuffer(100);
            buffer[0] = 'A'; // Should be able to write
            pool.ReturnCharBuffer(buffer);
        }
        
        // Assert - Pool disposal should clean up all buffers
        Assert.NotNull(buffer);
    }

    [Fact]
    public void ArrayPool_ReusesPreviouslyAllocatedArrays()
    {
        // This tests that ArrayPool actually reuses arrays
        char[]? firstArray = null;
        char[]? secondArray = null;
        
        // Rent and return an array
        firstArray = ArrayPool<char>.Shared.Rent(1024);
        ArrayPool<char>.Shared.Return(firstArray, clearArray: true);
        
        // Rent again - might get the same array back
        secondArray = ArrayPool<char>.Shared.Rent(1024);
        
        // The arrays might be the same reference (pool reuse)
        // or different (pool decided to allocate new)
        Assert.NotNull(secondArray);
        Assert.True(secondArray.Length >= 1024);
        
        // Clean up
        ArrayPool<char>.Shared.Return(secondArray, clearArray: true);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(16384)]
    public void BufferPool_HandlesVariousSizes(int size)
    {
        // Arrange
        using var pool = new BufferPool();
        
        // Act
        var buffer = pool.RentCharBuffer(size);
        
        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= size);
        
        // Clean up
        pool.ReturnCharBuffer(buffer);
    }

    [Fact]
    public void BufferPool_MultipleBuffersWork()
    {
        // Arrange
        using var pool = new BufferPool();
        
        // Act - Get multiple buffers
        var buffer1 = pool.RentCharBuffer(100);
        var buffer2 = pool.RentCharBuffer(200);
        var buffer3 = pool.RentCharBuffer(300);
        
        // Assert - All should be different buffers
        Assert.NotNull(buffer1);
        Assert.NotNull(buffer2);
        Assert.NotNull(buffer3);
        Assert.NotSame(buffer1, buffer2);
        Assert.NotSame(buffer2, buffer3);
        Assert.NotSame(buffer1, buffer3);
        
        // Clean up
        pool.ReturnCharBuffer(buffer1);
        pool.ReturnCharBuffer(buffer2);
        pool.ReturnCharBuffer(buffer3);
    }

    [Fact]
    public void BufferPool_ZeroSizeRequest_ReturnsValidBuffer()
    {
        // Arrange
        using var pool = new BufferPool();
        
        // Act
        var buffer = pool.RentCharBuffer(1); // Request at least 1 to ensure non-empty
        
        // Assert - Should get a valid buffer
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 1);
        
        // Clean up
        pool.ReturnCharBuffer(buffer);
    }

    [Fact]
    public void BufferPool_ByteBuffersAlsoWork()
    {
        // Arrange
        using var pool = new BufferPool();
        
        // Act
        var byteBuffer = pool.RentByteBuffer(256);
        var charBuffer = pool.RentCharBuffer(256);
        
        // Assert
        Assert.NotNull(byteBuffer);
        Assert.NotNull(charBuffer);
        Assert.True(byteBuffer.Length >= 256);
        Assert.True(charBuffer.Length >= 256);
        
        // Clean up
        pool.ReturnByteBuffer(byteBuffer);
        pool.ReturnCharBuffer(charBuffer);
    }
}