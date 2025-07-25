# Sep's Unique Optimizations vs FastCsv

## 1. **Always SIMD, Never Scalar Fallback**
Sep's most significant optimization is that it **NEVER falls back to scalar code**, even for quoted fields:
```csharp
// Sep always uses SIMD, even for complex cases
// FastCsv falls back to scalar for quoted fields
```

## 2. **Pack-and-Compare Algorithm**
Sep uses a clever packing technique to process 32 characters at once:
```csharp
// Load 32 chars as Vector256<ushort> (16-bit chars)
var chars = Vector256.Load(ptr);
// Pack to Vector256<byte> (8-bit) - processes 2x more data
var packed = Avx2.PackUnsignedSaturate(chars.GetLower(), chars.GetUpper());
// Compare against delimiters/quotes in parallel
var matches = Vector256.Equals(packed, delimiterVector);
```

## 3. **SearchValues API (.NET 8+)**
Sep leverages the new SearchValues for automatic SIMD optimization:
```csharp
private static readonly SearchValues<char> SpecialChars = 
    SearchValues.Create([',', '"', '\r', '\n']);
// Hardware-optimized searching
var index = span.IndexOfAny(SpecialChars);
```

## 4. **Unified Sep Structure**
Sep returns a single `Sep` struct that contains both row and column data:
```csharp
public readonly ref struct Sep
{
    public ReadOnlySpan<char> Span { get; }
    public int ColCount { get; }
    public SepColumn this[int index] { get; }
}
```

## 5. **Lazy Column Parsing**
Sep doesn't parse individual fields until accessed:
```csharp
public readonly ref struct SepColumn
{
    // Just stores start/length, no string allocation
    internal int Start { get; }
    internal int Length { get; }
    public ReadOnlySpan<char> Span => // Parse on demand
}
```

## 6. **Bit Manipulation for Position Tracking**
Sep uses advanced bit manipulation to track delimiter positions:
```csharp
// Extract positions from SIMD comparison results
var mask = matches.ExtractMostSignificantBits();
while (mask != 0)
{
    var offset = BitOperations.TrailingZeroCount(mask);
    // Process delimiter at position
    mask &= mask - 1; // Clear lowest bit
}
```

## 7. **Platform-Specific Implementations**
Sep has different implementations for different hardware:
- AVX-512 for newest Intel/AMD CPUs
- AVX2 for modern x64
- ARM NEON for Apple Silicon
- Fallback for older hardware

## 8. **Aggressive Inlining and Branch Prediction**
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining | 
            MethodImplOptions.AggressiveOptimization)]
```

## 9. **Custom Memory Management**
Sep uses ArrayPool and custom buffer management:
```csharp
private T[] _array = ArrayPool<T>.Shared.Rent(initialCapacity);
// Custom growth strategy
if (needsResize) GrowBuffer(newSize);
```

## 10. **Row-Based Processing**
Sep processes entire rows at once rather than field-by-field:
```csharp
// Sep finds all delimiters in a row in one SIMD pass
// FastCsv often processes field-by-field
```

## Key Performance Advantages:

1. **21 GB/s on modern CPUs** (AMD 9950X with AVX-512)
2. **2x faster than Sylvan** (previous fastest)
3. **9x faster than CsvHelper**
4. **Consistent performance** across different CSV structures

## What FastCsv Needs to Compete:

1. **Implement SearchValues** for .NET 8+ targets
2. **Always use SIMD** - never fall back to scalar
3. **Pack characters** to process 2x more data per instruction
4. **Process entire rows** in single SIMD passes
5. **Lazy field parsing** - don't parse until accessed
6. **Platform-specific builds** with optimal SIMD for each

The main takeaway: Sep achieves its speed by **never compromising on SIMD**, even for complex cases, and by processing data in larger chunks (32 chars at once vs 8-16 in most implementations).