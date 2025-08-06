#if NET7_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace HeroCsv.Core;

public sealed partial class HeroCsvReader
{
    /// <summary>
    /// Throws if the reader has been disposed (using modern pattern)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType());
    }
}
#endif