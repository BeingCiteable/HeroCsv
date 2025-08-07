using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace HeroCsv.Core;

/// <summary>
/// Provides pooling for StringBuilder instances to reduce allocations
/// </summary>
public sealed class StringBuilderPool
{
    private readonly ObjectPool<StringBuilder> _pool;
    private readonly int _maxBuilderCapacity;
    
    public StringBuilderPool(int maxBuilderCapacity = 4096)
    {
        _maxBuilderCapacity = maxBuilderCapacity;
        var policy = new StringBuilderPooledObjectPolicy
        {
            MaximumRetainedCapacity = maxBuilderCapacity
        };
        _pool = new DefaultObjectPool<StringBuilder>(policy);
    }
    
    /// <summary>
    /// Rents a StringBuilder from the pool
    /// </summary>
    public StringBuilder Rent()
    {
        return _pool.Get();
    }
    
    /// <summary>
    /// Returns a StringBuilder to the pool
    /// </summary>
    public void Return(StringBuilder stringBuilder)
    {
        if (stringBuilder == null)
            return;
            
        // Clear the builder before returning
        stringBuilder.Clear();
        
        // Only return to pool if capacity is reasonable
        if (stringBuilder.Capacity <= _maxBuilderCapacity)
        {
            _pool.Return(stringBuilder);
        }
    }
    
    private class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
    {
        public int InitialCapacity { get; set; } = 256;
        public int MaximumRetainedCapacity { get; set; } = 4096;
        
        public override StringBuilder Create()
        {
            return new StringBuilder(InitialCapacity);
        }
        
        public override bool Return(StringBuilder obj)
        {
            if (obj.Capacity > MaximumRetainedCapacity)
            {
                // Too big, let it be garbage collected
                return false;
            }
            
            obj.Clear();
            return true;
        }
    }
}