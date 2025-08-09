using System;
using System.Runtime.CompilerServices;

namespace HeroCsv.Utilities;

/// <summary>
/// Helper methods for slice operations to maintain compatibility with netstandard2.0
/// </summary>
internal static class SliceHelper
{
    /// <summary>
    /// Slices a span from start index to end of span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SliceFrom<T>(this ReadOnlySpan<T> span, int start)
    {
        return span.Slice(start);
    }
    
    /// <summary>
    /// Slices a span from start to end indices
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SliceRange<T>(this ReadOnlySpan<T> span, int start, int end)
    {
        return span.Slice(start, end - start);
    }
    
    /// <summary>
    /// Slices a span from start index to end of span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceFrom<T>(this Span<T> span, int start)
    {
        return span.Slice(start);
    }
    
    /// <summary>
    /// Slices a span from start to end indices
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceRange<T>(this Span<T> span, int start, int end)
    {
        return span.Slice(start, end - start);
    }
}