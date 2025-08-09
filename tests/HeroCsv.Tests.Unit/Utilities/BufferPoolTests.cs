using HeroCsv.Utilities;
using Xunit;

namespace HeroCsv.Tests.Unit.Utilities;

public class BufferPoolTests
{
    [Fact]
    public void BufferPool_RentCharBuffer_ReturnsBufferOfRequestedSizeOrLarger()
    {
        // Arrange
        using var pool = new BufferPool();
        var requestedSize = 100;
        
        // Act
        var buffer = pool.RentCharBuffer(requestedSize);
        
        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestedSize);
    }
    
    [Fact]
    public void BufferPool_RentByteBuffer_ReturnsBufferOfRequestedSizeOrLarger()
    {
        // Arrange
        using var pool = new BufferPool();
        var requestedSize = 256;
        
        // Act
        var buffer = pool.RentByteBuffer(requestedSize);
        
        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestedSize);
    }
    
    [Fact]
    public void BufferPool_ReturnCharBuffer_DoesNotThrow()
    {
        // Arrange
        using var pool = new BufferPool();
        var buffer = pool.RentCharBuffer(100);
        
        // Act & Assert (should not throw)
        pool.ReturnCharBuffer(buffer);
    }
    
    [Fact]
    public void BufferPool_ReturnByteBuffer_DoesNotThrow()
    {
        // Arrange
        using var pool = new BufferPool();
        var buffer = pool.RentByteBuffer(100);
        
        // Act & Assert (should not throw)
        pool.ReturnByteBuffer(buffer);
    }
    
    [Fact]
    public void BufferPool_ReturnAll_ClearsAllRentedBuffers()
    {
        // Arrange
        using var pool = new BufferPool();
        var charBuffer1 = pool.RentCharBuffer(100);
        var charBuffer2 = pool.RentCharBuffer(200);
        var byteBuffer1 = pool.RentByteBuffer(150);
        var byteBuffer2 = pool.RentByteBuffer(250);
        
        // Act
        pool.ReturnAll();
        
        // Assert - after ReturnAll, we should be able to return same buffers without issues
        // (this would fail if they were still tracked as rented)
        pool.ReturnCharBuffer(charBuffer1);
        pool.ReturnCharBuffer(charBuffer2);
        pool.ReturnByteBuffer(byteBuffer1);
        pool.ReturnByteBuffer(byteBuffer2);
    }
    
    [Fact]
    public void CharBufferLease_AutomaticallyReturnsBufferOnDispose()
    {
        // Arrange
        var pool = new BufferPool();
        char[] buffer;
        
        // Act
        using (var lease = new CharBufferLease(pool, 100))
        {
            buffer = lease.Buffer;
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 100);
        }
        
        // Assert - buffer should be returned, we can return it again without issue
        pool.ReturnCharBuffer(buffer);
    }
    
    [Fact]
    public void ByteBufferLease_AutomaticallyReturnsBufferOnDispose()
    {
        // Arrange
        var pool = new BufferPool();
        byte[] buffer;
        
        // Act
        using (var lease = new ByteBufferLease(pool, 256))
        {
            buffer = lease.Buffer;
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 256);
        }
        
        // Assert - buffer should be returned, we can return it again without issue
        pool.ReturnByteBuffer(buffer);
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
        
        // Assert - pool should have returned all buffers
        // We verify this by being able to return them again
        pool.ReturnCharBuffer(charBuffer);
        pool.ReturnByteBuffer(byteBuffer);
    }
}