using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeroCsv.AOT;

/// <summary>
/// AOT-compatible type preservers for common CSV types
/// </summary>
#if NET5_0_OR_GREATER
[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", 
    Justification = "Types are known at compile time")]
#endif
internal static class CsvTypePreserver
{
    static CsvTypePreserver()
    {
        // Preserve common CSV field types for AOT
        PreserveType<string>();
        PreserveType<int>();
        PreserveType<long>();
        PreserveType<double>();
        PreserveType<decimal>();
        PreserveType<bool>();
        PreserveType<DateTime>();
        PreserveType<DateTimeOffset>();
        PreserveType<Guid>();
        PreserveType<string[]>();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PreserveType<T>()
    {
        _ = typeof(T);
    }
}